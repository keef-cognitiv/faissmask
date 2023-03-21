using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FaissMask.Internal
{
	internal class IndexSafeHandle : SafeHandleZeroIsInvalid
	{
		private IndexSafeHandle? _parent = null;
		private readonly HashSet<IndexSafeHandle> _derivatives = new();

		public static THandle Read<THandle>(string filename, Func<IntPtr, THandle> createHandle, IoFlags flags)
			where THandle : IndexSafeHandle
		{
			if (string.IsNullOrEmpty(filename))
			{
				throw new ArgumentNullException(nameof(filename));
			}

			filename = Path.GetFullPath(filename);
			if (!File.Exists(filename))
			{
				throw new FileNotFoundException($"The file {filename} does not exist", filename);
			}

			var pointer = IntPtr.Zero;
			var returnCode = NativeMethods.faiss_read_index_fname(filename, (int)flags, ref pointer);
			if (returnCode != 0 || pointer == IntPtr.Zero)
			{
				var lastError = NativeMethods.faiss_get_last_error();

				if (string.IsNullOrEmpty(lastError))
				{
					throw new IOException(
						$"An unknown error occurred trying to read the index '{filename}' (return code {returnCode})");
				}
				else
				{
					throw new IOException(
						$"An error occurred trying to read the index '{filename}': {lastError} (return code {returnCode})");
				}
			}

			var index = createHandle(pointer);
			return index;
		}

		public void Write(string filename)
		{
			filename = Path.GetFullPath(filename);
			var returnCode = NativeMethods.faiss_write_index_fname(this, filename);
			if (returnCode != 0)
			{
				var lastError = NativeMethods.faiss_get_last_error();

				if (string.IsNullOrEmpty(lastError))
				{
					throw new IOException(
						$"An unknown error occurred trying to write the index '{filename}' (return code {returnCode})");
				}
				else
				{
					throw new IOException(
						$"An error occurred trying to write the index '{filename}': {lastError} (return code {returnCode})");
				}
			}
		}

		public static T FactoryCreate<T>(int dimensions, string description, MetricType metricType)
			where T : IndexSafeHandle, new()
		{
			T result = new T();
			var returnCode = NativeMethods.faiss_index_factory(ref result.handle, dimensions, description, metricType);
			if (returnCode == 0 && result.handle != IntPtr.Zero)
			{
				return result;
			}

			var lastError = NativeMethods.faiss_get_last_error();
			throw new ArgumentException($"Invalid arguments for FAISS factory: {lastError} (return code {returnCode})");
		}

		public bool IsFree { get; internal set; } = false;

		public IndexSafeHandle()
		{
		}

		internal IndexSafeHandle(IntPtr pointer) : base(pointer)
		{
		}

		public int Dimensions => NativeMethods.faiss_Index_d(this);

		public unsafe void Add(long count, ReadOnlySpan<float> vectors)
		{
			Checks.RequireCountMatches(count, vectors, Dimensions);

			fixed (float* vects = vectors)
			{
				NativeMethods.faiss_Index_add(this, count, vects);
			}
		}

		public bool IsTrained()
		{
			return NativeMethods.faiss_Index_is_trained(this);
		}

		public long NTotal()
		{
			long total = NativeMethods.faiss_Index_ntotal(this);
			return total;
		}

		public virtual void Free()
		{
			if (!IsInvalid)
			{
				NativeMethods.faiss_Index_free(handle);
				IsFree = true;
			}
		}

		public unsafe void Train(long count, ReadOnlySpan<float> trainingData)
		{
			Checks.RequireCountMatches(count, trainingData, Dimensions);

			int returnCode;
			fixed (float* data = trainingData)
			{
				returnCode = NativeMethods.faiss_Index_train(this, count, data);
			}

			if (returnCode != 0)
			{
				var lastError = NativeMethods.faiss_get_last_error();

				if (string.IsNullOrEmpty(lastError))
				{
					throw new ArgumentException(
						$"An unknown error occurred while training the index (return code {returnCode})");
				}

				throw new ArgumentException(
					$"Invalid arguments for a train or train on an Index that doesn't support it: {lastError} (return code {returnCode})");
			}
		}

		public unsafe void Search(long count, ReadOnlySpan<float> vectors, long k, Span<float> distances,
			Span<long> labels)
		{
			Checks.RequireCountMatches(count, vectors, Dimensions);
			if (distances.Length != k * count)
			{
				throw new ArgumentException(
					$"Output distances size ({distances.Length}) should match k * vector count ({k * count})");
			}

			if (labels.Length != k * count)
			{
				throw new ArgumentException(
					$"Output labels size ({labels.Length}) should match k * vector count ({k * count})");
			}

			fixed (float* vects = vectors)
			{
				fixed (float* dists = distances)
				{
					fixed (long* labs = labels)
					{
						NativeMethods.faiss_Index_search(this, count, vects, k, dists, labs);
					}
				}
			}
		}

		public RangeSearchResultSafeHandle RangeSearch(long count, ReadOnlySpan<float> vectors, float radius)
		{
			Checks.RequireCountMatches(count, vectors, Dimensions);

			var results = RangeSearchResultSafeHandle.New(count);
			int returnCode;

			unsafe
			{
				fixed (float* vects = vectors)
				{
					returnCode = NativeMethods.faiss_Index_range_search(this, count, vects, radius, results);
				}
			}

			if (returnCode != 0)
			{
				results.Dispose();
				var lastError = NativeMethods.faiss_get_last_error();

				if (string.IsNullOrEmpty(lastError))
				{
					throw new ArgumentException(
						$"An unknown error occurred while querying the index (return code {returnCode})");
				}

				throw new ArgumentException(
					$"Invalid arguments for a range_search or range_search on an Index that doesn't support it: {lastError} (return code {returnCode})");
			}

			return results;
		}

		protected override bool ReleaseHandle()
		{
			if (!IsFree)
				Free();
			
			_parent?._derivatives?.Remove(this);
			foreach (var derivative in _derivatives.ToList())
			{
				try
				{
					derivative._parent = null;
					derivative.Dispose();
				}
				catch (Exception)
				{
				}
			}

			return true;
		}

		public float[] ReconstructVector(long key)
		{
			var vector = new float[Dimensions];
			NativeMethods.faiss_Index_reconstruct(this, key, vector);
			return vector;
		}

		public float[][] ReconstructVectors(long startKey, long amount)
		{
			// TODO: There's probably a better way to marshall this 2D-array
			// Create one big float[] of the necessary size
			var dimensions = Dimensions;
			var reconstructedVectors = new float[dimensions * amount];
			NativeMethods.faiss_Index_reconstruct_n(this, startKey, amount, reconstructedVectors);
			// Then chop into smaller arrays of size equal to the number of dimensions
			var choppedVectors = new float[amount][];
			for (var i = 0; i < amount; i++)
			{
				var chop = new float[dimensions];
				Array.Copy(reconstructedVectors, i * dimensions, chop, 0, dimensions);
				choppedVectors[i] = chop;
			}

			return choppedVectors;
		}

		public unsafe byte[] EncodeVector(ReadOnlySpan<float> vector, int numberOfCOdes)
		{
			var bytes = new byte[numberOfCOdes];
			fixed (float* decoded = vector)
			{
				NativeMethods.faiss_Index_sa_encode(this, 1, decoded, bytes);
			}

			return bytes;
		}

		public unsafe float[] DecodeVector(ReadOnlySpan<byte> bytes)
		{
			var vector = new float[Dimensions];
			fixed (byte* encoded = bytes)
			{
				NativeMethods.faiss_Index_sa_decode(this, 1, encoded, vector);
			}

			return vector;
		}

		public void Assign(float[] vector, long[] labels, int k)
		{
			NativeMethods.faiss_Index_assign(this, 1, vector, labels, k);
		}

		public ulong SaCodeSize
		{
			get
			{
				var ptr = new UIntPtr(sizeof(ulong));
				NativeMethods.faiss_Index_sa_code_size(this, ptr);
				return ptr.ToUInt64();
			}
		}

		public void TrackDerivative(IndexSafeHandle nhandle)
		{
			nhandle._parent = this;
			nhandle.IsFree = true;
			_derivatives.Add(nhandle);
		}
	}
}