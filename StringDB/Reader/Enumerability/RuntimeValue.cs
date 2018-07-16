﻿using System;

namespace StringDB.Reader {

	/// <summary>A value that reads from the Stream for retrieving struff like the type, length, or data value.</summary>
	public interface IRuntimeValue {

		/// <summary>Gets the value if type T matches the type the data is stored as.</summary>
		T Get<T>();

		/// <summary>Gets the value if the typeHandler's byte Id matches the stored byte Id</summary>
		T Get<T>(TypeHandler<T> typeHandler);

		/// <summary>Ignores whatever type it's supposed to be and tries to get it as the type specified.</summary>
		T GetAs<T>();

		/// <summary>Ignores whatever type it's supposed to be and tries to get it as the type specified.</summary>
		T GetAs<T>(TypeHandler<T> typeHandler);

		/// <summary>Uses the TypeManager to try find the TypeHandler associated with the data and return it's type.</summary>
		Type GetTypeOf();

		/// <summary>Returns how long the data is.</summary>
		long GetLength();

		/// <summary>Get the identifier byte</summary>
		byte GetIdentifierByte();
	}

	internal struct RuntimeValue : IRuntimeValue {

		internal RuntimeValue(IRawReader rawReader, long readPos, byte? specifyType = null, long specifyLen = Consts.NOSPECIFYLEN) {
			this._specifyType = specifyType;
			this._specifyLen = specifyLen;
			this._rawReader = rawReader;
			this._readPos = readPos;
		}

		private readonly IRawReader _rawReader;
		private readonly long _specifyLen;
		private readonly byte? _specifyType;
		internal long _readPos;

		/// <inheritdoc/>
		public T Get<T>()
			=> this.Get<T>(TypeManager.GetHandlerFor<T>());

		/// <inheritdoc/>
		public T GetAs<T>()
			=> this.GetAs<T>(TypeManager.GetHandlerFor<T>());

		/// <inheritdoc/>
		public T Get<T>(TypeHandler<T> typeHandler)
			=> this._rawReader.ReadData<T>(this._readPos, typeHandler, this._specifyLen);

		/// <inheritdoc/>
		public T GetAs<T>(TypeHandler<T> typeHandler)
			=> this._rawReader.ReadDataAs<T>(this._readPos, typeHandler, this._specifyLen);

		/// <inheritdoc/>
		public Type GetTypeOf()
			=> this._specifyType == null ?
					this._rawReader.ReadType(this._readPos, null).Type
					: TypeManager.GetHandlerFor((byte)this._specifyType).Type;

		/// <inheritdoc/>
		public long GetLength()
			=> this._specifyLen != Consts.NOSPECIFYLEN ?
					this._specifyLen
					: this._rawReader.ReadLength(this._readPos);

		/// <inheritdoc/>
		public byte GetIdentifierByte()
			=> this._specifyType == null ?
					this._rawReader.ReadByte(this._readPos)
					: (byte)this._specifyType;

		/// <inheritdoc/>
		public override string ToString()
			=> $"(0x{this.GetIdentifierByte().ToString("x2")}, {this.GetLength()} bytes, {this.GetTypeOf()})";
	}
}