﻿using JetBrains.Annotations;

using System.Collections.Generic;

namespace StringDB.IO
{
	/// <inheritdoc />
	/// <summary>
	/// An <see cref="T:StringDB.IO.IDatabaseIODevice" /> that interfaces with <see cref="T:StringDB.IO.ILowlevelDatabaseIODevice" />
	/// </summary>
	[PublicAPI]
	public sealed class DatabaseIODevice : IDatabaseIODevice
	{
		private bool _disposed = false;

		public ILowlevelDatabaseIODevice LowLevelDatabaseIODevice { get; }

		/// <inheritdoc />
		public IOptimalTokenSource OptimalTokenSource { get; }

		public DatabaseIODevice
		(
			[NotNull] ILowlevelDatabaseIODevice lowlevelDBIOD
		)
			: this(lowlevelDBIOD, new OptimalTokenSource())
		{
		}

		public DatabaseIODevice
		(
			[NotNull] ILowlevelDatabaseIODevice lowlevelDBIOD,
			[NotNull] IOptimalTokenSource optimalTokenSource
		)
		{
			LowLevelDatabaseIODevice = lowlevelDBIOD;
			OptimalTokenSource = optimalTokenSource;
		}

		public void Reset() => LowLevelDatabaseIODevice.Reset();

		/// <inheritdoc />
		public byte[] ReadValue(long position)
		{
			// temporarily go to the position to read the value,
			// then seek back to the cursor position for reading
			var curPos = LowLevelDatabaseIODevice.GetPosition();

			var value = LowLevelDatabaseIODevice.ReadValue(position);

			LowLevelDatabaseIODevice.Seek(curPos);

			return value;
		}

		/// <inheritdoc />
		public DatabaseItem ReadNext()
		{
			if (OptimalTokenSource.OptimalToken.OptimalReadingTime)
			{
				OptimalTokenSource.SetOptimalReadingTime(false);
			}

			// handle EOFs/Jumps
			var peek = LowLevelDatabaseIODevice.Peek(out var peekResult);

			ExecuteJumps(ref peek, out var jmpPeekResult);

			if (jmpPeekResult != 0x00)
			{
				peekResult = jmpPeekResult;
			}

			if (peek == NextItemPeek.EOF)
			{
				return new DatabaseItem
				{
					EndOfItems = true
				};
			}

			// peek HAS to be an Index at this point

			var item = LowLevelDatabaseIODevice.ReadIndex(peekResult);

			return new DatabaseItem
			{
				Key = item.Index,
				DataPosition = item.DataPosition,
				EndOfItems = false
			};
		}

		private void ExecuteJumps(ref NextItemPeek peek, out byte peekResult)
		{
			peekResult = 0x00;

			if (peek != NextItemPeek.Jump)
			{
				return;
			}

			do
			{
				var jump = LowLevelDatabaseIODevice.ReadJump();
				LowLevelDatabaseIODevice.Seek(jump);
				peek = LowLevelDatabaseIODevice.Peek(out peekResult);
			}
			while (peek == NextItemPeek.Jump);

			OptimalTokenSource.SetOptimalReadingTime(true);
		}

		/// <inheritdoc />
		public void Insert(KeyValuePair<byte[], byte[]>[] items)
		{
			LowLevelDatabaseIODevice.SeekEnd();

			var offset = LowLevelDatabaseIODevice.GetPosition();

			UpdatePreviousJump(offset);

			// we need to calculate the total offset of all the indexes
			// then we write every index & increment the offset by the offset of each value
			// and then we write the values

			// phase 1: calculating total offset

			foreach (var kvp in items)
			{
				offset += LowLevelDatabaseIODevice.CalculateIndexOffset(kvp.Key);
			}

			// the jump offset is important, we will be jumping after
			offset += LowLevelDatabaseIODevice.JumpOffsetSize;

			// phase 2: writing each key
			//			and incrementing the offset by the value

			foreach (var kvp in items)
			{
				LowLevelDatabaseIODevice.WriteIndex(kvp.Key, offset);

				offset += LowLevelDatabaseIODevice.CalculateValueOffset(kvp.Value);
			}

			WriteJump();

			// phase 3: writing each value sequentially

			foreach (var kvp in items)
			{
				LowLevelDatabaseIODevice.WriteValue(kvp.Value);
			}
		}

		private void UpdatePreviousJump(long jumpTo)
		{
			var currentPosition = LowLevelDatabaseIODevice.GetPosition();

			if (LowLevelDatabaseIODevice.JumpPos != 0)
			{
				// goto old jump pos and overwrite it with the current jump pos
				LowLevelDatabaseIODevice.Seek(LowLevelDatabaseIODevice.JumpPos);
				LowLevelDatabaseIODevice.WriteJump(jumpTo);
			}

			LowLevelDatabaseIODevice.Seek(currentPosition);
		}

		private void WriteJump()
		{
			var position = LowLevelDatabaseIODevice.GetPosition();

			LowLevelDatabaseIODevice.JumpPos = position;
			LowLevelDatabaseIODevice.WriteJump(0);
		}

		public void Dispose()
		{
			// we call "flush" when we shouldn't flush something disposed
			// so we gotta make sure it's not dead
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			LowLevelDatabaseIODevice.Flush();
			LowLevelDatabaseIODevice.Dispose();
		}
	}
}