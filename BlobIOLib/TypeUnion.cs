using System;
using System.Runtime.InteropServices;

namespace BlobIO
{
    [StructLayout(LayoutKind.Explicit)]
    public struct TypeUnion<A, B>
    {
        [FieldOffset(0)]
        private A _aValue;

        [FieldOffset(0)]
        private B _bValue;

        public A FirstType
        {
            get { return _aValue; }
            set { _aValue = value; }
        }

        public B SecondType
        {
            get { return _bValue; }
            set { _bValue = value; }
        }

        public TypeUnion(A value) { _bValue = default(B); _aValue = value; }
        public TypeUnion(B value) { _aValue = default(A); _bValue = value; }
    }
}

