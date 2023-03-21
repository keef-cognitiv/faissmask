using System;
using System.Collections.Generic;
using System.Linq;
using FaissMask.Internal;

namespace FaissMask;

public class RangeSearchResult : IDisposable
{
	internal RangeSearchResult(RangeSearchResultSafeHandle handle) 
	{
		if (handle.IsInvalid)
		{
			throw new ArgumentException("Handle is invalid");
		}
		
		Handle = handle;
	}

	public RangeSearchResultSafeHandle Handle { get; init; }

	public IEnumerable<IEnumerable<SearchResult>> SearchResults
	{
		get
		{
			var labelResults = Handle.Labels;
			var labels = labelResults.Labels.ToArray();
			var distances = labelResults.Distances.ToArray();
			var lims = Handle.Limits.ToArray();
			int nq = (int)Handle.Nq;

			for (int i = 0; i < nq; i++)
			{
				int start = (int)lims[i];
				int count = (int)lims[i + 1] - start;

				yield return Enumerable.Range(start, count).Select(x => new SearchResult()
				{
					Label = labels[x],
					Distance = distances[x]
				});
			}
		}
	}

	public void Dispose()
	{
		Handle.Dispose();
	}
}