using System;

namespace FaissMask.Internal
{
	internal class IndexIDMapSafeHandle : IndexSafeHandle
	{
		public static IndexIDMapSafeHandle New(IndexFlatSafeHandle index)
		{
			var idIndex = new IndexIDMapSafeHandle();
			NativeMethods.faiss_IndexIDMap_new(ref idIndex, index);
			return idIndex;
		}

		public IndexIDMapSafeHandle()
		{
		}

		public IndexIDMapSafeHandle(IntPtr pointer) : base(pointer)
		{
		}

		public unsafe void Add(long count, Span<float> vectors, Span<long> ids)
		{
			fixed (float* vptr = vectors)
			{
				fixed (long* idptr = ids)
				{
					NativeMethods.faiss_Index_add_with_ids(this, count, vptr, idptr);
				}
			}
		}
	}
}