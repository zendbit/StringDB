﻿using JetBrains.Annotations;

using StringDB.Querying.Queries;

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace StringDB.Querying
{
	public class ReactiveQueryManager<TKey, TValue> : IQueryManager<TKey, TValue>
	{
		private readonly TrainEnumerable<KeyValuePair<TKey, IRequest<TValue>>> _trainEnumerable;

		public ReactiveQueryManager(TrainEnumerable<KeyValuePair<TKey, IRequest<TValue>>> trainEnumerable)
		{
			_trainEnumerable = trainEnumerable;
		}

		public void Dispose() => throw new NotImplementedException();

		public async Task<bool> ExecuteQuery([NotNull] IQuery<TKey, TValue> query)
		{
			foreach(var item in _trainEnumerable)
			{
				var result = await query.Accept(item.Key, item.Value)
					.ConfigureAwait(false);

				if (result == QueryAcceptance.Completed
					|| result == QueryAcceptance.Accepted)
				{
					await query.Process(item.Key, item.Value)
						.ConfigureAwait(false);

					if (result == QueryAcceptance.Completed)
					{
						return true;
					}
				}
			}

			return false;
		}

		public Task ExecuteQuery([NotNull] IWriteQuery<TKey, TValue> writeQuery) => throw new NotImplementedException();
	}
}