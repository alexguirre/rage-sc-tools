namespace ScTools
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal static class Util
    {
        public static readonly IntPtr BaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
        public static bool IsInGame => Process.GetCurrentProcess().MainModule!.ModuleName == "GTA5.exe";

        public static IntPtr RVA(long offset) => (IntPtr)(BaseAddress.ToInt64() + offset);

        public static event Action? AfterGameUpdate;

        public static void RaiseAfterGameUpdate() => AfterGameUpdate?.Invoke();
    }

    internal static class Kernel32
    {
        private const string Lib = "kernel32.dll";

        [DllImport(Lib)]
        public static unsafe extern IntPtr CreateThread(IntPtr lpThreadAttributes, ulong dwStackSize, delegate* unmanaged[Stdcall]<IntPtr, int> lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();
        
        [DllImport(Lib)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeConsole();
    }
}
