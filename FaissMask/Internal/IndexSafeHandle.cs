using System;
using System.IO;

namespace FaissMask.Internal
{
	internal class IndexSafeHandle : SafeHandleZeroIsInvalid
	{
		public static THandle Read<THandle>(string filename, Func<IntPtr, THandle> createHandle)
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
			var returnCode = NativeMethods.faiss_read_index_fname(filename, 0, ref pointer);
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

		public static IndexSafeHandle FactoryCreate(int dimensions, string description, MetricType metricType)
		{
			IndexSafeHandle result = new IndexSafeHandle();
			var returnCode = NativeMethods.faiss_index_factory(ref result, dimensions, description, metricType);
			if (returnCode == 0 && result.handle != IntPtr.Zero)
			{
				return result;
			}

			var lastError = NativeMethods.faiss_get_last_error();
			throw new ArgumentException($"Invalid arguments for FAISS factory: {lastError} (return code {returnCode})");
		}

		public bool IsFree { get; internal set; } = false;

		protected IndexSafeHandle()
		{
		}

		protected IndexSafeHandle(IntPtr pointer) : base(pointer)
		{
		}

		public int Dimensions => NativeMethods.faiss_Index_d(this);

		public void Add(long count, float[] vectors)
		{
			NativeMethods.faiss_Index_add(this, count, vectors);
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

		public void Search(long count, float[] vectors, long k, float[] distances, long[] labels)
		{
			NativeMethods.faiss_Index_search(this, count, vectors, k, distances, labels);
		}

		public RangeSearchResultSafeHandle RangeSearch(long count, float[] vectors, float radius)
		{
			var results = RangeSearchResultSafeHandle.New(count);
			var returnCode = NativeMethods.faiss_Index_range_search(this, count, vectors, radius, results);

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

		public byte[] EncodeVector(float[] vector, int numberOfCOdes)
		{
			var bytes = new byte[numberOfCOdes];
			NativeMethods.faiss_Index_sa_encode(this, 1, vector, bytes);
			return bytes;
		}

		public float[] DecodeVector(byte[] bytes)
		{
			var vector = new float[Dimensions];
			NativeMethods.faiss_Index_sa_decode(this, 1, bytes, vector);
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
	}
}