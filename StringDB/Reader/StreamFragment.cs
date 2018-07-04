﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StringDB.Reader
{
	public class StreamFragment : Stream {
		public StreamFragment(Stream main, long pos, long lenAfterPos) {
			this.Length = lenAfterPos;
			this._originalPos = pos;
			this._pos = pos;
			this._s = main;
		}

		private Stream _s;

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;

		public override long Length { get; }

		private long _originalPos { get; }
		private long _pos;
		public override long Position {
			get {
				return this._pos - this._originalPos;
			}
			set {
				var lP = this._pos;
				this._pos = this._originalPos + value;
				//TODO:                                                                >=
				if (this._pos - this._originalPos < 0 || this._pos - this._originalPos	> this.Length)
					this._pos = lP;
			}
		}

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count) {
			if (this._pos - this._originalPos < 0)
				return -1;

			var c = count;
			if (this._pos - this._originalPos + c > this.Length)
				c += (int)( this.Length - ((this._pos - this._originalPos) + c) );

			this._s.Seek(this._pos, SeekOrigin.Begin);
			this._pos += c;
			return this._s.Read(buffer, offset, c);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			if(origin == SeekOrigin.Begin) {
				this.Position = offset;
			} else if (origin == SeekOrigin.Current) {
				this.Position += offset;
			} else if (origin == SeekOrigin.End) {
				this.Position = this.Length + offset;
			}

			return this.Position;
		}

		public override void SetLength(long value) => throw new NotImplementedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
	}
}
