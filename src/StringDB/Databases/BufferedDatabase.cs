﻿using JetBrains.Annotations;

using StringDB.LazyLoaders;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StringDB.Databases
{
	/// <summary>
	/// Buffers writes to a database,
	/// coagulating multiple inserts until the buffer is full.
	/// </summary>
	[PublicAPI]
	public sealed class BufferedDatabase<TKey, TValue>
		: BaseDatabase<TKey, TValue>, IDatabaseLayer<TKey, TValue>
	{
		public const int MinimumBufferSize = 16;

		/// <summary>
		/// Creates a new <see cref="BufferedDatabase{TKey, TValue}"/>
		/// with the specified buffer.
		/// </summary>
		/// <param name="database">The database to buffer.</param>
		/// <param name="bufferSize">The size of the buffer.</param>
		/// <param name="disposeDatabase">If the underlying database should be disposed on dispose.</param>
		public BufferedDatabase
		(
			[NotNull] IDatabase<TKey, TValue> database,
			int bufferSize = 0x1000,
			bool disposeDatabase = true
		)
		{
			if (bufferSize < MinimumBufferSize)
			{
				throw new ArgumentException(nameof(bufferSize), $"A buffer smaller than {MinimumBufferSize} is not allowed.");
			}

			InnerDatabase = database;
			_disposeDatabase = disposeDatabase;
			_buffer = new KeyValuePair<TKey, TValue>[bufferSize];
			_bufferPos = 0;
		}

		[NotNull] private readonly KeyValuePair<TKey, TValue>[] _buffer;
		private readonly bool _disposeDatabase;
		private int _bufferPos = 0;

		public IDatabase<TKey, TValue> InnerDatabase { get; }

		/// <summary>
		/// Flushes the internal buffer.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Flush() => WriteBuffer();

		/// <summary>
		/// Fills the internal buffer.
		/// </summary>
		/// <param name="fillAmt"></param>
		/// <param name="used">As more of the items in the array are used,</param>
		/// <returns>True if the buffer is filled, false if it is not.</returns>
		private bool FillBuffer([NotNull] KeyValuePair<TKey, TValue>[] fillAmt, ref int used)
		{
			var needFill = fillAmt.Length - used;

			if (needFill == 0)
			{
				return false;
			}

			// the amount we can fill,
			// it's either the amount of space remaning in the buffer,
			// or the array length
			var amountCanFill = Math.Min(_buffer.Length - _bufferPos, needFill);

			// if we can't fill anything, say that the buffer's full
			if (amountCanFill <= 0)
			{
				return true;
			}

			// an overflow will occur
			var willFill = _bufferPos + needFill >= _buffer.Length;

			// copy from the src to the buffer
			Array.Copy(fillAmt, used, _buffer, _bufferPos, amountCanFill);

			// increment respective variables
			used += amountCanFill;
			_bufferPos += amountCanFill;

			return willFill;
		}

		private bool FillBufferSingle(KeyValuePair<TKey, TValue> entry)
		{
			if (_bufferPos == _buffer.Length)
			{
				// no room to insert anything
				return true;
			}

			_buffer[_bufferPos++] = entry;

			return false;
		}

		/// <summary>
		/// Writes the entire buffer to the db
		/// </summary>
		private void WriteBuffer()
		{
			// if our buffer is full
			if (_bufferPos == _buffer.Length)
			{
				// write the entire buffer
				InnerDatabase.InsertRange(_buffer);
			}
			else if (_bufferPos == 0)
			{
				return;
			}
			else
			{
				// otherwise, make a temporary array to copy the buffer to
				var array = new KeyValuePair<TKey, TValue>[_bufferPos];

				Array.Copy(_buffer, array, _bufferPos);

				InnerDatabase.InsertRange(array);
			}

			// since we've written the buffer, clean it out
			// this will remove all references to used objects to let GC do it's thing
			for (int i = 0; i < _buffer.Length; i++)
			{
				_buffer[i] = default;
			}

			_bufferPos = 0;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			WriteBuffer();

			if (_disposeDatabase)
			{
				InnerDatabase.Dispose();
			}
		}

		/// <inheritdoc/>
		public override void Insert(TKey key, TValue value)
		{
			var pair = new KeyValuePair<TKey, TValue>(key, value);

			// this will be true if it fails to fill
			if (FillBufferSingle(pair))
			{
				// that means we need to flush the buffer,
				WriteBuffer();

				// and re-add it to the buffer
				FillBufferSingle(pair);
			}
		}

		/// <inheritdoc/>
		public override void InsertRange(params KeyValuePair<TKey, TValue>[] items)
		{
			int used = 0;

			while (FillBuffer(items, ref used))
			{
				WriteBuffer();
			}
		}

		/// <inheritdoc/>
		protected override IEnumerable<KeyValuePair<TKey, ILazyLoader<TValue>>> Evaluate()
		{
			// return the original database
			foreach (var kvp in InnerDatabase)
			{
				yield return kvp;
			}

			// and virtually append the buffer
			for (var i = 0; i < _bufferPos; i++)
			{
				var kvp = _buffer[i];
				yield return new ValueLoader<TValue>(kvp.Value)
					.ToKeyValuePair(kvp.Key);
			}
		}
	}
}