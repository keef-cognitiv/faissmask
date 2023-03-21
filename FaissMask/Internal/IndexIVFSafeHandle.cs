using System;

namespace FaissMask.Internal
{
    internal class IndexIVFSafeHandle : IndexSafeHandle
    {
        public IndexIVFSafeHandle() {}
        public IndexIVFSafeHandle(IntPtr pointer) : base(pointer) {}
        public void MakeDirectMap()
        {
            NativeMethods.faiss_IndexIVF_make_direct_map(this, 1);
        }

        public void Merge(IndexIVFSafeHandle other, long addId)
        {
            int returnCode = NativeMethods.faiss_IndexIVF_merge_from(this, other, addId);
            if (returnCode != 0)
            {
                var lastError = NativeMethods.faiss_get_last_error();

                if (string.IsNullOrEmpty(lastError))
                {
                    throw new ArgumentException(
                        $"An unknown error occurred while merging the index (return code {returnCode})");
                }

                throw new ArgumentException(
                    $"Invalid arguments for merging or merging an Index that doesn't support it: {lastError} (return code {returnCode})");
            }
        }
        
        public long NumProbes
        {
            get => NativeMethods.faiss_IndexIVF_nprobe(this);
            set => NativeMethods.faiss_IndexIVF_set_nprobe(this, value);
        }
    }
}