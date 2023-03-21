using System;

namespace FaissMask.Internal
{
    internal class IndexPreTransformSafeHandle : IndexSafeHandle
    {
        public IndexPreTransformSafeHandle() {}
        public IndexPreTransformSafeHandle(IntPtr pointer) : base(pointer) {}

        public IndexSafeHandle Index
        {
            get
            {
                var index = NativeMethods.faiss_IndexPreTransform_index(this);
                if (index == IntPtr.Zero)
                {
                    throw new ObjectDisposedException("IndexPreTransform.Index");
                }

                var safeHandle = new IndexSafeHandle(index);
                safeHandle.IsFree = true;
                return safeHandle;
            }
        }

        public static IndexPreTransformSafeHandle New(IndexSafeHandle innerIndex)
        {
            var index = new IndexPreTransformSafeHandle();
            NativeMethods.faiss_IndexPreTransform_new_with(ref index, innerIndex);
            return index;
        }

        public void PrependTransform(VectorTransformSafeHandle transform)
        {
            int returnCode = NativeMethods.faiss_IndexPreTransform_prepend_transform(this, transform);
            if (returnCode != 0)
            {
                var lastError = NativeMethods.faiss_get_last_error();

                if (string.IsNullOrEmpty(lastError))
                {
                    throw new ArgumentException(
                        $"An unknown error occurred while prepending the transform (return code {returnCode})");
                }

                throw new ArgumentException(
                    $"Failure to prepend transform: {lastError} (return code {returnCode})");
            }
        }
    }
}