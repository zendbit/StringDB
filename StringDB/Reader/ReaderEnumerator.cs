﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace StringDB.Reader {
	/// <summary>Pairs an index with a value. It only retrieves the value when it's called for, so it makes no wasted calls when iterating over in a foreach loop.</summary>
	public class ReaderPair {
		internal ReaderPair(IReader parent, IReaderInteraction dataPos) {
			this._parent = parent;
			this._dataPos = dataPos;
		}

		private IReader _parent { get; }
		private IReaderInteraction _dataPos { get; }
		private string ValueCached { get; set; }

		/// <summary>The Index of this ReaderPair.</summary>
		public string Index => this._dataPos.Index;
		
		/// <summary>The Value of this ReaderPair.<para>When called for the first time, it retrieves the value of it and stores it for later usage incase of multiple calls.</para></summary>
		public string Value => (this.ValueCached ?? (this.ValueCached = this._parent.GetValueOf(this._dataPos)));
	}

	/// <summary>Allows you to enumerate over an IReader efficiently.</summary>
	public class ReaderEnumerator : IEnumerator<ReaderPair> {
		internal ReaderEnumerator(IReader parent, IReaderInteraction start) {
			this._parent = parent;

			this._indexOn = start.Index;

			this._seekTo = 0;
			this._toSeek = start.QuickSeek;
			this._first = false;
		}

		private bool _first { get; set; }

		private string _indexOn { get; set; }
		private ulong _seekTo { get; set; }
		private ulong _toSeek { get; set; }
		private IReader _parent { get; set; }

		//TODO: Reading the value of the index is resource heavy, especially if one is only iterating over it for the indexes. Should use some kind of class to fetch the value of the index for quicker reading.
		/// <inheritdoc/>
		public ReaderPair Current => new ReaderPair(this._parent, new ReaderInteraction(this._indexOn, 0, this._toSeek));

		object IEnumerator.Current => this.Current; /// <inheritdoc/>

		public bool MoveNext() { //TODO: not use IndexAfter. in the documentation we literally say not to call IndexAfter and that a foreach loop is better because the foreach loop doesnt - clearly we do.
			if (!this._first) {
				this._first = true;
				return true;
			}

			var rr = this._parent.IndexAfter(this._indexOn, true, this._seekTo);

			if (rr == null)
				return false;

			this._indexOn = rr.Index;

			this._seekTo = this._toSeek;
			this._toSeek = rr.QuickSeek;

			return true;
		} /// <inheritdoc/>

		public void Reset() {
			var rr = this._parent.FirstIndex();

			this._indexOn = rr.Index;
			this._seekTo = rr.QuickSeek;
		}

		#region IDisposable Support
		private bool disposedValue = false; /// <inheritdoc/>

		protected virtual void Dispose(bool disposing) {
			if (!this.disposedValue) {
				if (disposing) {
					//TODO: dispose managed state (managed objects).
				}

				//TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				//TODO: set large fields to null.

				this.disposedValue = true;
			}
		} /// <inheritdoc/>

		//TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~ReaderEnumerator() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			//TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}