using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ScTools.Five
{
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct strIndex
    {
        public int Value;

        public strLocalIndex ToLocal(int objectsBaseIndex) => new strLocalIndex { Value = this.Value - objectsBaseIndex };

        public override int GetHashCode() => Value;
        public override bool Equals(object? other) => other is strIndex i && Equals(i);
        public bool Equals(strIndex other) => Value == other.Value;

        public static implicit operator strIndex(int index) => new strIndex { Value = index };
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct strLocalIndex
    {
        public int Value;

        public strIndex ToGlobal(int objectsBaseIndex) => new strIndex { Value = this.Value + objectsBaseIndex };

        public override int GetHashCode() => Value;
        public override bool Equals(object? other) => other is strLocalIndex i && Equals(i);
        public bool Equals(strLocalIndex other) => Value == other.Value;

        public static implicit operator strLocalIndex(int index) => new strLocalIndex { Value = index };
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct strStreaming
    {
        private static readonly void* ClearRequiredFlagAddress = Util.IsInGame ? (void*)Util.RVA(0x15C2700/*b2189*/) : null;
        public void ClearRequiredFlag(strIndex index, uint flags)
            => ((delegate* unmanaged[Thiscall]<ref strStreaming, int, uint, void>)ClearRequiredFlagAddress)(ref this, index.Value, flags);

        private static readonly void* RemoveObjectAddress = Util.IsInGame ? (void*)Util.RVA(0x15D30E4/*b2189*/) : null;
        public bool RemoveObject(strIndex index)
            => ((delegate* unmanaged[Thiscall]<ref strStreaming, int, bool>)RemoveObjectAddress)(ref this, index.Value);

        private static readonly void* UnregisterObjectAddress = Util.IsInGame ? (void*)Util.RVA(0x15D6A10/*b2189*/) : null;
        public bool UnregisterObject(strIndex index)
            => ((delegate* unmanaged[Thiscall]<ref strStreaming, int, bool>)UnregisterObjectAddress)(ref this, index.Value);


        public static readonly strStreaming* InstancePtr = Util.IsInGame ? (strStreaming*)Util.RVA(0x2DA0AF0/*b2189*/) : null;
        public static ref strStreaming Instance => ref Unsafe.AsRef<strStreaming>(InstancePtr);
    }

    internal static unsafe class strPackfileManager
    {
        private static readonly void* RegisterIndividualFileAddress = Util.IsInGame ? (void*)Util.RVA(0x15D23B0/*b2189*/) : null;
        public static strIndex RegisterIndividualFile(string fullPath, bool a3, string registerAs, bool errorIfFailed)
        {
            var fn = (delegate* unmanaged[Thiscall]<strIndex*, IntPtr, bool, IntPtr, bool, strIndex*>)RegisterIndividualFileAddress;
            strIndex result;
            var fullPathCopy = Marshal.StringToHGlobalAnsi(fullPath);
            var registerAsCopy = Marshal.StringToHGlobalAnsi(registerAs);
            fn(&result, fullPathCopy, a3, registerAsCopy, errorIfFailed);
            Marshal.FreeHGlobal(fullPathCopy);
            Marshal.FreeHGlobal(registerAsCopy);
            return result;
        }

        private static readonly void* InvalidateIndividualFileAddress = Util.IsInGame ? (void*)Util.RVA(0x15CDDF0/*b2189*/) : null;
        public static void InvalidateIndividualFile(string registeredAs)
        {
            var fn = (delegate* unmanaged[Thiscall]<IntPtr, void>)InvalidateIndividualFileAddress;
            var registeredAsCopy = Marshal.StringToHGlobalAnsi(registeredAs);
            fn(registeredAsCopy);
            Marshal.FreeHGlobal(registeredAsCopy);
        }
    }
}
