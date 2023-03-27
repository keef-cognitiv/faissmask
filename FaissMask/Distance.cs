using System;
using System.Collections.Generic;
using System.Linq;
using FaissMask.Extensions;
using FaissMask.Internal;

namespace FaissMask;

public static class Distance
{
	public static List<float[]> RenormL2(IReadOnlyCollection<IReadOnlyList<float>> x)
	{
		if (x.Count == 0)
		{
			return new List<float[]>();
		}

		var dims = x.First().Count;
		foreach (var vec in x)
		{
			if (vec.Count != dims)
			{
				throw new ArgumentException("Inconsistent dimensions");
			}
		}

		var results = x.SelectMany(d => d).ToArray();
		NativeMethods.faiss_fvec_renorm_L2((ulong)dims, (ulong)x.Count, results);

		return Enumerable.Range(0, x.Count)
			.Select(index => new ArraySegment<float>(results, index * dims, dims).Array)
			.ToList();
	}

	public static IList<float> InnerProducts(IReadOnlyCollection<float[]> vectors, float[] x)
	{
		if (vectors.Count == 0)
		{
			return Array.Empty<float>();
		}

		var dims = vectors.First().Length;
		foreach (var vec in vectors)
		{
			if (vec.Length != dims)
			{
				throw new ArgumentException("Inconsistent dimensions");
			}
		}

		var result = new float[vectors.Count];
		var vectorsFlat = vectors.Flatten();
		NativeMethods.faiss_fvec_inner_products_ny(result, x, vectorsFlat, (ulong)dims, (ulong)vectors.Count);

		return result;
	}
	
	public static IList<float> L2Square(IReadOnlyCollection<IReadOnlyList<float>> vectors, float[] x)
	{
		if (vectors.Count == 0)
		{
			return Array.Empty<float>();
		}

		var dims = vectors.First().Count;
		foreach (var vec in vectors)
		{
			if (vec.Count != dims)
			{
				throw new ArgumentException("Inconsistent dimensions");
			}
		}

		var result = new float[vectors.Count];
		var vectorsFlat = vectors.SelectMany(v => v).ToArray();
		NativeMethods.faiss_fvec_L2sqr_ny(result, x, vectorsFlat, (ulong)dims, (ulong)vectors.Count);

		return result;
	}
}