namespace ScTools.Five
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct CStreamedScripts
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct VTableDef
        {
            [FieldOffset(0x010)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, ref int, IntPtr, int*> FindSlot;

            [FieldOffset(0x020)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, int, void> RemoveSlot;
            
            [FieldOffset(0x040)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, int, scrProgram*> GetPtr;

            [FieldOffset(0x098)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, int, uint> GetNumRefs;

            [FieldOffset(0x100)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, int> GetSize;
            [FieldOffset(0x108)] public delegate* unmanaged[Thiscall]<ref CStreamedScripts, int> GetNumUsedSlots;
        }

        [FieldOffset(0x00)] public VTableDef* VTable;
        [FieldOffset(0x08)] public int ObjectsBaseIndex;
        [FieldOffset(0x0C)] public int ModuleIndex;
        [FieldOffset(0x10)] public int ObjectsCapacity;

        public strLocalIndex FindSlot(string name)
        {
            var namePtr = Marshal.StringToHGlobalAnsi(name);
            int index = -1;
            VTable->FindSlot(ref this, ref index, namePtr);
            Marshal.FreeHGlobal(namePtr);
            return index;
        }
        public void RemoveSlot(strLocalIndex index) => VTable->RemoveSlot(ref this, index.Value);
        public ref scrProgram GetPtr(strLocalIndex index) => ref Unsafe.AsRef<scrProgram>(VTable->GetPtr(ref this, index.Value));
        public uint GetNumRefs(strLocalIndex index) => VTable->GetNumRefs(ref this, index.Value);
        public int GetSize() => VTable->GetSize(ref this);
        public int GetNumUsedSlots() => VTable->GetNumUsedSlots(ref this);

        private static readonly void* GetAssetNameAddress = Util.IsInGame ? (void*)Util.RVA(0xA31540/*b2215*/) : null;
        public string GetAssetName(strLocalIndex index)
        {
            var namePtr = ((delegate* unmanaged[Thiscall]<ref CStreamedScripts, int, IntPtr>)GetAssetNameAddress)(ref this, index.Value);
            return Marshal.PtrToStringAnsi(namePtr) ?? "<null>";
        }

        private static readonly void* StreamingBlockingLoadAddress = Util.IsInGame ? (void*)Util.RVA(0x15D7180/*b2215*/) : null;
        public bool StreamingBlockingLoad(strLocalIndex index, uint flags)
            => ((delegate* unmanaged[Thiscall]<ref CStreamedScripts, int, uint, bool>)StreamingBlockingLoadAddress)(ref this, index.Value, flags);

        // TODO: use patterns
        public static readonly CStreamedScripts* InstancePtr = Util.IsInGame ? (CStreamedScripts*)Util.RVA(0x2618290/*b2215*/) : null;
        public static ref CStreamedScripts Instance => ref Unsafe.AsRef<CStreamedScripts>(InstancePtr);
    }
}
