namespace ScTools
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal static class Util
    {
        public static readonly nint BaseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;
        public static bool IsInGame => Process.GetCurrentProcess().MainModule!.ModuleName == "GTA5.exe";

        public static event Action? AfterGameUpdate;

        public static void RaiseAfterGameUpdate() => AfterGameUpdate?.Invoke();

        public static unsafe void* GetAddress(this nint address, nint offset)
        {
            if (address == 0)
            {
                return null;
            }

            address = address + *(int*)(address + offset) + offset + 4;
            return (void*)address;
        }

        public static unsafe T* GetAddress<T>(this nint address, nint offset) where T : unmanaged => (T*)GetAddress(address, offset);

        public static nint FindPattern(string pattern) => FindPattern(PatternFromString(pattern));
        public static nint FindPattern(ReadOnlySpan<byte> pattern)
        {
            var module = Process.GetCurrentProcess().MainModule!;
            return FindPattern(module.BaseAddress, module.ModuleMemorySize, pattern);
        }

        // https://github.com/learn-more/findpattern-bench/blob/master/patterns/fdsasdf.h
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static unsafe nint FindPattern(nint baseAddress, nint imageSize, ReadOnlySpan<byte> pattern)
        {
            byte* patternPtr = (byte*)Unsafe.AsPointer(ref Unsafe.AsRef(pattern[0]));
            byte* address = (byte*)baseAddress;
            byte first = patternPtr[0];
            byte* max = (byte*)(baseAddress + imageSize - pattern.Length);

            for (; address < max; ++address)
            {
                if (*address != first)
                {
                    continue;
                }
                if (CompareByteArray(address, patternPtr, pattern.Length))
                {
                    Console.WriteLine($"Found pattern '{PatternToString(pattern)}' = {((nint)address).ToString("X16")}  (+{((nint)address - BaseAddress).ToString("X8")})");
                    return (nint)address;
                }
            }

            Console.WriteLine($"Failed to find pattern '{PatternToString(pattern)}'");
            return 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            static bool CompareByteArray(byte* data, byte* pattern, int length)
            {
                for (; length != 0; ++pattern, ++data, --length)
                {
                    if (*pattern == 0)
                    {
                        continue;
                    }
                    if (*data != *pattern)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private static byte[] PatternFromString(string pattern)
            => pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .Select(s => s == "?" ? (byte)0x00 : byte.Parse(s, System.Globalization.NumberStyles.HexNumber))
                      .ToArray();

        private static string PatternToString(ReadOnlySpan<byte> pattern)
            => string.Join(' ', pattern.ToArray()
                                       .Select(b => b == 0x00 ? "?" : b.ToString("X2")));
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
