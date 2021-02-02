namespace ScTools
{
    using System;
    using System.Diagnostics;

    internal static class Util
    {
        public static readonly IntPtr BaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
        public static bool IsInGame => Process.GetCurrentProcess().MainModule!.ModuleName == "GTA5.exe";

        public static IntPtr RVA(long offset) => (IntPtr)(BaseAddress.ToInt64() + offset);
    }
}
