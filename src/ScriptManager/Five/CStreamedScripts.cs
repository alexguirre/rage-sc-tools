namespace ScTools.Five
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct CStreamedScripts
    {
        [FieldOffset(0)] public VTableDef* VTable;

        public ref scrProgram GetPtr(uint index) => ref Unsafe.AsRef<scrProgram>(VTable->GetPtr(ref this, index));
        public uint GetSize() => VTable->GetSize(ref this);
        public uint GetNumUsedSlots() => VTable->GetNumUsedSlots(ref this);

        private static readonly void* GetAssetNameAddress = Util.IsInGame ? (void*)Util.RVA(0xA300B4) : null;
        public string GetAssetName(uint index)
        {
            var namePtr = ((delegate* unmanaged[Thiscall]<ref CStreamedScripts, uint, IntPtr>)GetAssetNameAddress)(ref this, index);
            return Marshal.PtrToStringAnsi(namePtr) ?? "<null>";
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct VTableDef
        {
            [FieldOffset(0x040)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, uint, scrProgram*> GetPtr;

            [FieldOffset(0x100)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, uint> GetSize;
            [FieldOffset(0x108)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, uint> GetNumUsedSlots;
        }

        // TODO: use patterns
        public static readonly CStreamedScripts* InstancePtr = Util.IsInGame ? (CStreamedScripts*)Util.RVA(0x2610240) : null;
        public static ref CStreamedScripts Instance => ref Unsafe.AsRef<CStreamedScripts>(InstancePtr);
    }
}
