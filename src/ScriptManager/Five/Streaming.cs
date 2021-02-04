using System;
using System.Runtime.InteropServices;

namespace ScTools.Five
{
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct strIndex
    {
        public int Value;

        public strLocalIndex ToLocal(int objectsBaseIndex) => new strLocalIndex { Value = this.Value - objectsBaseIndex };

        public static implicit operator strIndex(int index) => new strIndex { Value = index };
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct strLocalIndex
    {
        public int Value;

        public static implicit operator strLocalIndex(int index) => new strLocalIndex { Value = index };
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
    }
}
