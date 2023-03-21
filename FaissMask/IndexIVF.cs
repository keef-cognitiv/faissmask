using System;
using FaissMask.Internal;

namespace FaissMask
{
    public class IndexIVF : Index
    {
        internal IndexIVF(IndexIVFSafeHandle handle) : base(handle)
        {
        }

        public static IndexIVF Read(string filename, IoFlags flags = IoFlags.None)
        {
            var handle = IndexSafeHandle.Read(filename, ptr => new IndexIVFSafeHandle(ptr), flags);
            return new IndexIVF(handle);
        }
        
        IndexIVFSafeHandle IndexIvfSafeHandle => Handle as IndexIVFSafeHandle;

        public new static IndexIVF Create(int dimensions, string indexDescription, MetricType metric)
        {
            return new IndexIVF(IndexSafeHandle.FactoryCreate<IndexIVFSafeHandle>(dimensions, indexDescription, metric));
        }
        
        public void MakeDirectMap()
        {
            IndexIvfSafeHandle.MakeDirectMap();
        }

        public void Merge(IndexIVF other, long addId)
        {
            IndexIvfSafeHandle.Merge((IndexIVFSafeHandle)other.Handle, addId);
        }
        
        public long NumProbes
        {
            get => IndexIvfSafeHandle.NumProbes;
            set => IndexIvfSafeHandle.NumProbes = value;
        }

        public static IndexIVF ConvertFrom(Index index)
        {
            var handle = index.Handle;
            var nhandle = new IndexIVFSafeHandle(handle.DangerousGetHandle());
            
            // leave ownership on the other handle
            handle.TrackDerivative(nhandle);
            
            // return the IVF
            return new IndexIVF(nhandle);
        }
    }
}