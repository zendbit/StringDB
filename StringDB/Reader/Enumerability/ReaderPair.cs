﻿using System;
using System.Collections.Generic;
using System.Text;

namespace StringDB.Reader {
	public interface IReaderPair {
		byte[] IndexAsByteArray { get; }
		string Index { get; }
		string Value { get; }
	}

	public class ReaderPair : IReaderPair {
		public ReaderPair(IPartDataPair dp, IRawReader rawReader) {
			this._dp = dp;
			this._rawReader = rawReader;
		}
		
		private IPartDataPair _dp { get; }
		private IRawReader _rawReader { get; }

		private string _indexCache { get; set; } = null;
		private string _valueCache { get; set; } = null;

		public byte[] IndexAsByteArray => this._dp.Index;

		public string Index => this._indexCache ?? (this._indexCache = Encoding.UTF8.GetString(this._dp.Index));
		public string Value => this._valueCache ?? (this._valueCache = Encoding.UTF8.GetString(this._dp.ReadData(this._rawReader) ?? new byte[0] { }));
	}
}