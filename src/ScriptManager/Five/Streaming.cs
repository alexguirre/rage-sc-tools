namespace ScTools.Five
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

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
        private static readonly void* ClearRequiredFlagAddress = Util.IsInGame ? (void*)Util.FindPattern("89 54 24 10 48 83 EC 28 4C 8B C9 8B CA 4D 8B 11 45 0F B7 5C CA") : null;
        public void ClearRequiredFlag(strIndex index, uint flags)
            => ((delegate* unmanaged[Thiscall]<ref strStreaming, int, uint, void>)ClearRequiredFlagAddress)(ref this, index.Value, flags);

        private static readonly void* RemoveObjectAddress = Util.IsInGame ? (void*)Util.FindPattern("48 89 5C 24 ? 89 54 24 10 55 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ? ? ? ? 48 81 EC ? ? ? ? 4C 8B 31") : null;
        public bool RemoveObject(strIndex index)
            => ((delegate* unmanaged[Thiscall]<ref strStreaming, int, bool>)RemoveObjectAddress)(ref this, index.Value);

        private static readonly void* UnregisterObjectAddress = Util.IsInGame ? (void*)Util.FindPattern("8B C2 48 8B 11 F6 44 C2 ? ? 75 15 33 C9 66 89 4C C2") : null;
        public bool UnregisterObject(strIndex index)
            => ((delegate* unmanaged[Thiscall]<ref strStreaming, int, bool>)UnregisterObjectAddress)(ref this, index.Value);


        public static readonly strStreaming* InstancePtr = Util.IsInGame ? Util.FindPattern("48 8B 05 ? ? ? ? 03 CB 8B 54 C8 04 80 E2 03 80 FA 01 0F 85 ? ? ? ? 48 8B 05").GetAddress<strStreaming>(3) : null;
        public static ref strStreaming Instance => ref Unsafe.AsRef<strStreaming>(InstancePtr);
    }

    internal static unsafe class strPackfileManager
    {
        private static readonly void* RegisterIndividualFileAddress = Util.IsInGame ? (void*)(Util.FindPattern("4D 0F 45 F9 E8 ? ? ? ? 48 8B F8 48 85 C0 75 13 38 84 24") - 0x2D) : null;
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

        private static readonly void* InvalidateIndividualFileAddress = Util.IsInGame ? (void*)Util.FindPattern("40 53 48 83 EC 20 48 8B D9 E8 ? ? ? ? 48 8B D3 4C 8B 00 48 8B C8 41 FF 90 ? ? ? ? 8B D8 E8") : null;
        public static void InvalidateIndividualFile(string registeredAs)
        {
            var fn = (delegate* unmanaged[Thiscall]<IntPtr, void>)InvalidateIndividualFileAddress;
            var registeredAsCopy = Marshal.StringToHGlobalAnsi(registeredAs);
            fn(registeredAsCopy);
            Marshal.FreeHGlobal(registeredAsCopy);
        }
    }
}
