namespace ScTools.Five
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct CStreamedScripts
    {
        [FieldOffset(0x00)] public VTableDef* VTable;
        [FieldOffset(0x08)] public int ObjectsBaseIndex;
        [FieldOffset(0x0C)] public int ModuleIndex;
        [FieldOffset(0x10)] public int ObjectsCapacity;

        public strLocalIndex FindSlot(string name)
        {
            var namePtr = Marshal.StringToHGlobalAnsi(name);
            VTable->FindSlot(ref this, out strLocalIndex index, namePtr);
            Marshal.FreeHGlobal(namePtr);
            return index;
        }
        public ref scrProgram GetPtr(strLocalIndex index) => ref Unsafe.AsRef<scrProgram>(VTable->GetPtr(ref this, index.Value));
        public int GetSize() => VTable->GetSize(ref this);
        public int GetNumUsedSlots() => VTable->GetNumUsedSlots(ref this);

        private static readonly void* GetAssetNameAddress = Util.IsInGame ? (void*)Util.RVA(0xA300B4/*b2189*/) : null;
        public string GetAssetName(strLocalIndex index)
        {
            var namePtr = ((delegate* unmanaged[Thiscall]<ref CStreamedScripts, int, IntPtr>)GetAssetNameAddress)(ref this, index.Value);
            return Marshal.PtrToStringAnsi(namePtr) ?? "<null>";
        }

        private static readonly void* StreamingBlockingLoadAddress = Util.IsInGame ? (void*)Util.RVA(0x15D5D74/*b2189*/) : null;
        public bool StreamingBlockingLoad(strLocalIndex index, uint flags)
            => ((delegate* unmanaged[Thiscall]<ref CStreamedScripts, int, uint, bool>)StreamingBlockingLoadAddress)(ref this, index.Value, flags);

        [StructLayout(LayoutKind.Explicit)]
        public struct VTableDef
        {
            [FieldOffset(0x040)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, out strLocalIndex, IntPtr, ref strLocalIndex> FindSlot;

            [FieldOffset(0x040)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, int, scrProgram*> GetPtr;

            [FieldOffset(0x100)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, int> GetSize;
            [FieldOffset(0x108)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, int> GetNumUsedSlots;
        }

        // TODO: use patterns
        public static readonly CStreamedScripts* InstancePtr = Util.IsInGame ? (CStreamedScripts*)Util.RVA(0x2610240/*b2189*/) : null;
        public static ref CStreamedScripts Instance => ref Unsafe.AsRef<CStreamedScripts>(InstancePtr);
    }
}
