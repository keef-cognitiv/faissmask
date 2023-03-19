using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FaissMask.Internal
{
	internal static class NativeMethods
	{
		static NativeMethods()
		{
			string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			//  on Unix, without setting LD_LIBRARY_PATH, .NET will appropriately resolve
			// faiss_c below, but then it will defer to the operating system to dlopen it.
			// It depends on libfaiss mkl and libomp. So to get the load to work correctly,
			// we need to preload the libraries from the .NET location
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
			    RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
			{
				var assembly = Assembly.GetExecutingAssembly();
				IntPtr lib = IntPtr.Zero;
				if (RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86)
				{
					// load omp and mkl
					NativeLibrary.TryLoad("mkl_core", assembly, default, out lib);
					NativeLibrary.TryLoad("mkl_sequential", assembly, default, out lib);
					NativeLibrary.TryLoad("mkl_intel_lp64", assembly, default, out lib);
				}

				// load gomp and base faiss
				NativeLibrary.TryLoad("gomp", assembly, default, out lib);
				NativeLibrary.TryLoad("faiss", assembly, default, out lib);
			}
		}

		[DllImport("faiss_c")]
		public static extern int faiss_Index_add(IndexSafeHandle index, long n, float[] x);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_add_with_ids(IndexSafeHandle index, long n, float[] x, long[] xids);

		[DllImport("faiss_c")]
		public static extern bool faiss_Index_is_trained(IndexSafeHandle index);

		[DllImport("faiss_c")]
		public static extern long faiss_Index_ntotal(IndexSafeHandle index);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_free(IntPtr index);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_search(IndexSafeHandle index, long n, float[] x, long k, float[] distances,
			long[] labels);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_sa_code_size(IndexSafeHandle index, UIntPtr size);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_sa_encode(IndexSafeHandle index, long n, float[] x, byte[] bytes);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_sa_decode(IndexSafeHandle index, long n, byte[] bytes, float[] x);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_assign(IndexSafeHandle index, long n, float[] x, long[] labels, long k);

		[DllImport("faiss_c")]
		public static extern int faiss_IndexFlat_new(ref IndexFlatSafeHandle index);

		[DllImport("faiss_c")]
		public static extern int faiss_IndexFlat_new_with(ref IndexFlatSafeHandle index, long d, MetricType metric);

		[DllImport("faiss_c")]
		public static extern int faiss_IndexFlat_free(IntPtr index);

		[DllImport("faiss_c")]
		public static extern int faiss_IndexFlatL2_new(ref IndexFlatL2SafeHandle index);

		[DllImport("faiss_c")]
		public static extern int faiss_IndexFlatL2_new_with(ref IndexFlatL2SafeHandle index, long d);

		[DllImport("faiss_c")]
		public static extern int faiss_IndexIDMap_new(ref IndexIDMapSafeHandle mapIndex, IndexFlatSafeHandle index);

		[DllImport("faiss_c")]
		public static extern int faiss_read_index_fname(string fname, int io_flags, ref IntPtr p_out);

		[DllImport("faiss_c")]
		public static extern string faiss_get_last_error();

		[DllImport("faiss_c")]
		public static extern int faiss_IndexIVF_make_direct_map(IndexSafeHandle index, int new_maintain_direct_map);

		[DllImport("faiss_c")]
		public static extern void faiss_Index_reconstruct(IndexSafeHandle index, long key, float[] recons);

		[DllImport("faiss_c")]
		public static extern void faiss_Index_reconstruct_n(IndexSafeHandle index, long i0, long ni, float[] recons);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_d(IndexSafeHandle index);

		[DllImport("faiss_c")]
		public static extern long faiss_IndexIVF_nprobe(IndexSafeHandle index);

		[DllImport("faiss_c")]
		public static extern void faiss_IndexIVF_set_nprobe(IndexSafeHandle index, long nProbe);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_range_search(IndexSafeHandle index, long n, float[] x, float radius,
			RangeSearchResultSafeHandle result);

		[DllImport("faiss_c")]
		public static extern int faiss_index_factory(ref IndexSafeHandle index, int d, string description,
			MetricType metric);

		[DllImport("faiss_c")]
		public static extern void faiss_IndexPreTransform_new(ref IndexPreTransformSafeHandle index);

		[DllImport("faiss_c")]
		public static extern int faiss_RangeSearchResult_new(ref RangeSearchResultSafeHandle result, long nq);

		[DllImport("faiss_c")]
		public static extern ulong faiss_RangeSearchResult_nq(RangeSearchResultSafeHandle result);

		[DllImport("faiss_c")]
		public static extern ulong faiss_RangeSearchResult_buffer_size(RangeSearchResultSafeHandle result);

		[DllImport("faiss_c")]
		public static extern void faiss_RangeSearchResult_lims(RangeSearchResultSafeHandle result, ref IntPtr limsPtr);

		[DllImport("faiss_c")]
		public static extern void faiss_RangeSearchResult_labels(RangeSearchResultSafeHandle result, ref IntPtr labels,
			ref IntPtr distances);

		[DllImport("faiss_c")]
		public static extern void faiss_RangeSearchResult_free(IntPtr result);
	}
}