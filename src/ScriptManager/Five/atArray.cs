namespace ScTools.Five
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    internal unsafe struct atArray<T> where T : unmanaged
    {
        public T* Items;
        public ushort Count;
        public ushort Size;

        public ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return ref Items[index];
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    internal unsafe struct atArrayOfPtrs<T> where T : unmanaged
    {
        public T** Items;
        public ushort Count;
        public ushort Size;

        public ref T* this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return ref Items[index];
            }
        }

        public bool IsItemNull(int index) => this[index] == null;

        public ref T ItemDeref(int index)
        {
            var item = this[index];
            if (item == null)
            {
                throw new NullReferenceException("Attempt to dereference null pointer");
            }

            return ref Unsafe.AsRef(*item);
        }
    }
}
