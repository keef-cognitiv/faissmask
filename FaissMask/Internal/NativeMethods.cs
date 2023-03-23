using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FaissMask.Internal
{
	internal static class NativeMethods
	{
		private static class FaissPreloader
		{
			[Flags]
			private enum UnixDlOpenFlags
			{
				RTLD_LAZY = 0x00001,
				RTLD_NOW = 0x00002,
				RTLD_GLOBAL = 0x00100,
				RTLD_LOCAL = 0
			}

			[DllImport("libdl.so.2")]
			private static extern unsafe IntPtr dlopen(string library_name, UnixDlOpenFlags flags);

			[DllImport("libdl.so.2")]
			private static extern unsafe IntPtr dlerror();

			static FaissPreloader()
			{
				// Faiss links optionally against MKL, OpenMP, and/or BLAS implementations. Some of these ship
				// as module libraries that just export symbols. Faiss links directly to a subset of these which
				// it needs. But those libs internally depend on each other as modules. And they leave it up to
				// derivative programs to explicitly link as required through the entire dependency chain. As such,
				// faiss can be loaded immediately but then RTLD_LAZY calls end up requiring symbols that are not
				// explicitly loaded in as a byproduct of loading FAISS. So we have to do extra work to preaload
				// them as needed
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
				    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
				    RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
				{
					if (RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.X86)
					{
						LoadLibrary("mkl_core");

						// load deferred mkl dependencies
						if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
						{
							LoadLibrary("mkl_gnu_thread");
						}

						LoadLibrary("mkl_def");
						LoadLibrary("mkl_avx2");
					}

					// load gomp and base faiss
					LoadLibrary("gomp");
					LoadLibrary("faiss");
				}
			}

			private static string GetRuntimeOS()
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					return "linux";
				}

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					return "windows";
				}

				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					return "osx";
				}

				if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
				{
					return "freebsd";
				}

				throw new Exception($"Unsupported OS {RuntimeInformation.OSDescription}");
			}

			private static string GetRuntimeArch()
			{
				switch (RuntimeInformation.ProcessArchitecture)
				{
					case Architecture.Arm:
						return "arm";
					case Architecture.Arm64:
						return "arm64";
					case Architecture.X64:
						return "x64";
					case Architecture.X86:
						return "x86";
					default:
						throw new Exception($"Unsupported OS {RuntimeInformation.ProcessArchitecture.ToString()}");
				}
			}

			private static string GetRuntimeDir()
			{
				return $"{GetRuntimeOS()}-{GetRuntimeArch()}";
			}

			private static (string, string) GetPrefixAndSuffix()
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
				    RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
				{
					return ("lib", ".so");
				}

				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					return ("", ".dll");
				}

				if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					return ("lib", ".dylib");
				}

				throw new Exception($"Unsupported OS {RuntimeInformation.OSDescription}");
			}

			private static void LoadLibrary(string library)
			{
				IntPtr result;

				var (prefix, suffix) = GetPrefixAndSuffix();
				var assembly = Assembly.GetExecutingAssembly();

				var assemblyDir = Path.GetDirectoryName(assembly.Location);
				var osDir = Path.Join(assemblyDir, "runtimes", GetRuntimeDir(), "native");

				var dirs = new[] { assemblyDir, osDir };
				foreach (var dir in dirs)
				{
					var filename = Path.Join(dir, $"{prefix}{library}{suffix}");
					var files = Directory.GetFiles(dir);
					foreach (var file in files)
					{
						if (!file.StartsWith(filename))
						{
							continue;
						}

						result = dlopen(file, UnixDlOpenFlags.RTLD_GLOBAL | UnixDlOpenFlags.RTLD_LAZY);
						if (result != IntPtr.Zero)
						{
							return;
						}
					}
				}

				result = dlopen(library, UnixDlOpenFlags.RTLD_GLOBAL | UnixDlOpenFlags.RTLD_LAZY);
				if (result == IntPtr.Zero)
				{
					var err = Marshal.PtrToStringUTF8(dlerror());
					throw new DllNotFoundException($"Couldn't load library {library}: {err}");
				}
			}


			public static IntPtr PreloaderResolver(string libraryName, Assembly asm,
				DllImportSearchPath? dllImportSearchPath)
			{
				return IntPtr.Zero;
			}
		}

		static NativeMethods()
		{
			var assembly = Assembly.GetExecutingAssembly();
			NativeLibrary.SetDllImportResolver(assembly, FaissPreloader.PreloaderResolver);
		}

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_Index_add(IndexSafeHandle index, long n, float* x);

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_Index_add_with_ids(IndexSafeHandle index, long n, float* x, long* xids);

		[DllImport("faiss_c")]
		public static extern bool faiss_Index_is_trained(IndexSafeHandle index);

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_Index_train(IndexSafeHandle index, long count, float* data);

		[DllImport("faiss_c")]
		public static extern long faiss_Index_ntotal(IndexSafeHandle index);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_free(IntPtr index);

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_Index_search(IndexSafeHandle index, long n, float* x, long k,
			float* distances,
			long* labels);

		[DllImport("faiss_c")]
		public static extern int faiss_Index_sa_code_size(IndexSafeHandle index, UIntPtr size);

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_Index_sa_encode(IndexSafeHandle index, long n, float* x, byte[] bytes);

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_Index_sa_decode(IndexSafeHandle index, long n, byte* bytes, float[] x);

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
		public static extern int faiss_write_index_fname(IndexSafeHandle index, string fname);

		[DllImport("faiss_c")]
		public static extern string faiss_get_last_error();

		[DllImport("faiss_c")]
		public static extern int faiss_IndexIVF_make_direct_map(IndexSafeHandle index, int new_maintain_direct_map);

		[DllImport("faiss_c")]
		public static extern int faiss_IndexIVF_merge_from(IndexIVFSafeHandle index, IndexIVFSafeHandle other,
			long addIds);

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
		public static extern unsafe int faiss_Index_range_search(IndexSafeHandle index, long n, float* x, float radius,
			RangeSearchResultSafeHandle result);

		[DllImport("faiss_c")]
		public static extern int faiss_index_factory(ref IntPtr index, int d, string description,
			MetricType metric);

		[DllImport("faiss_c")]
		public static extern void faiss_IndexPreTransform_new_with(ref IndexPreTransformSafeHandle index,
			IndexSafeHandle innerIndex);

		[DllImport("faiss_c")]
		public static extern IntPtr faiss_IndexPreTransform_index(IndexPreTransformSafeHandle index);
		
		[DllImport("faiss_c")]
		public static extern int faiss_IndexPreTransform_prepend_transform(
			IndexPreTransformSafeHandle index,
			VectorTransformSafeHandle transform);

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

		[DllImport("faiss_c")]
		public static extern int faiss_VectorTransform_d_in(VectorTransformSafeHandle transform);

		[DllImport("faiss_c")]
		public static extern int faiss_VectorTransform_d_out(VectorTransformSafeHandle transform);

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_VectorTransform_train(VectorTransformSafeHandle transform, long count,
			float* data);

		[DllImport("faiss_c")]
		public static extern int faiss_VectorTransform_free(IntPtr transform);

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_VectorTransform_apply_noalloc(
			VectorTransformSafeHandle transform,
			long count,
			float* vectors,
			float* output);

		[DllImport("faiss_c")]
		public static extern unsafe int faiss_VectorTransform_reverse_transform(
			VectorTransformSafeHandle transform,
			long count,
			float* vectors,
			float* output);

		[DllImport("faiss_c")]
		public static extern int faiss_RandomRotationMatrix_new_with(ref VectorTransformSafeHandle handle,
			int dimensionsIn, int dimensionsOut);

		[DllImport("faiss_c")]
		public static extern int faiss_PCAMatrix_new_with(
			ref VectorTransformSafeHandle handle,
			int dimensionsIn,
			int dimensionsOut,
			float eigenPower,
			int randomRotation);
		
		[DllImport("faiss_c")]
		public static extern int faiss_ITQMatrix_new_with(ref VectorTransformSafeHandle handle, int d);
		
		[DllImport("faiss_c")]
		public static extern int faiss_ITQTransform_new_with(
			ref VectorTransformSafeHandle handle,
			int dimensionsIn,
			int dimensionsOut,
			int doPca);
		
		[DllImport("faiss_c")]
		public static extern int faiss_OPQMatrix_new_with(
			ref VectorTransformSafeHandle handle,
			int d,
			int m,
			int d2);
		
		[DllImport("faiss_c")]
		public static extern int faiss_RemapDimensionsTransform_new_with(
			ref VectorTransformSafeHandle handle,
			int dimensionsIn,
			int dimensionsOut,
			int uniform);
		
		[DllImport("faiss_c")]
		public static extern int faiss_NormalizationTransform_new_with(
			ref VectorTransformSafeHandle handle,
			int d,
			float norm);
		
		[DllImport("faiss_c")]
		public static extern int faiss_CenteringTransform_new_with(
			ref VectorTransformSafeHandle handle,
			int d);
		
		[DllImport("faiss_c")]
		public static extern void faiss_fvec_renorm_L2(ulong d, ulong nx, float[] x);
		
		[DllImport("faiss_c")]
		public static extern void faiss_fvec_inner_products_ny(float[] ip, float[] x, float[] y, ulong d, ulong ny);
		
		[DllImport("faiss_c")]
		public static extern void faiss_fvec_L2sqr_ny(float[] dist, float[] x, float[] y, ulong d, ulong ny);
	}
}