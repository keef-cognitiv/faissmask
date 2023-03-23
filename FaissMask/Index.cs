using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using FaissMask.Extensions;
using FaissMask.Internal;

namespace FaissMask
{
	public class Index : IDisposable
	{
		internal readonly IndexSafeHandle Handle;

		public long Count
		{
			get => Handle.NTotal();
		}

		public bool IsTrained
		{
			get => Handle.IsTrained();
		}

		protected Index(object handle)
		{
			Handle = handle as IndexSafeHandle;
		}

		internal Index(IndexSafeHandle handle)
		{
			Handle = handle;
		}

		public void Add(float[] vector)
		{
			Add(1, vector);
		}

		public void Add(IReadOnlyCollection<float[]> vectors)
		{
			var count = vectors.Count;
			Add(count, vectors.SelectMany(v => v).ToArray());
		}

		public void Add(long count, ReadOnlySpan<float> vectors)
		{
			Handle.Add(count, vectors);
		}

		public long[] Assign(float[] vectors, int k)
		{
			var labels = new long[k];
			Handle.Assign(vectors, labels, k);
			return labels;
		}

		public IEnumerable<SearchResult> Search(float[] vector, long kneigbors)
		{
			return Search(1, vector, kneigbors).First();
		}

		public IReadOnlyCollection<IEnumerable<SearchResult>> Search(IReadOnlyCollection<float[]> vectors,
			long kneighbors)
		{
			int count = vectors.Count;
			var vectorsFlattened = vectors.Flatten();

			return Search(count, vectorsFlattened, kneighbors);
		}

		public void Train(long count, ReadOnlySpan<float> trainingData)
		{
			Handle.Train(count, trainingData);
		}

		public void Train(IReadOnlyCollection<float[]> trainingData)
		{
			Train(trainingData.Count, trainingData.Flatten());
		}

		private IReadOnlyList<IEnumerable<SearchResult>> Search(long count, Span<float> vectorsFlattened,
			long kneighbors)
		{
			float[] distances = new float[kneighbors * count];
			long[] labels = new long[kneighbors * count];

			Handle.Search(count, vectorsFlattened, kneighbors, distances, labels);
			return ToSearchResults(count, distances, labels, kneighbors);
		}

		private IReadOnlyList<IEnumerable<SearchResult>> ToSearchResults(long count, float[] distances, long[] labels,
			long kneighbors)
		{
			return Enumerable.Range(0, (int)count)
				.Select(index => 
					ToSearchResults(distances, labels, index * (int)kneighbors, (int)kneighbors))
				.ToList();
		}

		public IEnumerable<SearchResult> ToSearchResults(float[] distances, long[] labels, int offset, int count)
		{
			for (int i = 0; i < count; i++)
			{
				yield return new SearchResult()
				{
					Distance = distances[offset + i],
					Label = labels[offset + i]
				};
			}
		}

		public RangeSearchResult RangeSearch(IReadOnlyCollection<float[]> vectors, float radius)
		{
			int count = vectors.Count;
			var vectorsFlattened = vectors.Flatten();

			return RangeSearch(count, vectorsFlattened, radius);
		}

		public RangeSearchResult RangeSearch(long count, float[] vectors, float radius)
		{
			return new RangeSearchResult(Handle.RangeSearch(count, vectors, radius));
		}

		public int Dimensions => Handle.Dimensions;

		public virtual bool CanRangeSearch => false;

		public float[] ReconstructVector(long key)
		{
			var ret = Handle.ReconstructVector(key);
			return ret;
		}

		public float[][] ReconstructVectors(long startKey, long amount)
		{
			var ret = Handle.ReconstructVectors(startKey, amount);
			return ret;
		}

		public static Index Create(int dimensions, string indexDescription, MetricType metric)
		{
			return new Index(IndexSafeHandle.FactoryCreate<IndexSafeHandle>(dimensions, indexDescription, metric));
		}

		public void Write(string filename)
		{
			Handle.Write(filename);
		}

		public ulong SaCodeSize => Handle.SaCodeSize;

		public byte[] EncodeVector(float[] vector, int numberOfCodes)
		{
			return Handle.EncodeVector(vector, numberOfCodes);
		}

		public float[] DecodeVector(byte[] bytes)
		{
			return Handle.DecodeVector(bytes);
		}

		public virtual void Dispose()
		{
			Handle?.Dispose();
		}
	}
}