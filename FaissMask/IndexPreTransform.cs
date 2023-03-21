using System;
using System.Collections.Generic;
using System.Linq;
using FaissMask.Internal;

namespace FaissMask
{
    public class IndexPreTransform : Index
    {
        private readonly Index _index;
        private readonly LinkedList<VectorTransform> _transforms = new();

        private IndexPreTransform(IndexPreTransformSafeHandle handle, Index index) : base(handle)
        {
            _index = index;
        }
        
        public IndexPreTransform(Index index) : base(IndexPreTransformSafeHandle.New(index.Handle))
        {
            _index = index;
        }

        public void PrependTransform(VectorTransform transform)
        {
            IndexPreTransformSafeHandle.PrependTransform(transform.Handle);
            _transforms.AddFirst(transform);
        }
        
        public static IndexPreTransform Read(string filename, IoFlags flags = IoFlags.None)
        {
            var handle = IndexSafeHandle.Read(filename, ptr => new IndexPreTransformSafeHandle(ptr), flags);
            Index inner;

            try
            {
                inner = new Index(handle.Index);
            }
            catch (Exception)
            {
                handle.Dispose();
                throw;
            }

            return new IndexPreTransform(handle, inner);
        }
        
        IndexPreTransformSafeHandle IndexPreTransformSafeHandle => Handle as IndexPreTransformSafeHandle;

        public Index Index => _index;

        public override void Dispose()
        {
            foreach (var tx in _transforms)
            {
                tx.Dispose();
            }
            
            _index.Dispose();
            base.Dispose();
        }
    }
}