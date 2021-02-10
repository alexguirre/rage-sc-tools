namespace ScTools
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal static unsafe class EntryPoint
    {
        private static IntPtr UIThreadHandle;

        [UnmanagedCallersOnly(EntryPoint = nameof(DllMain), CallConvs = new[] { typeof(CallConvStdcall) })]
        public static bool DllMain(IntPtr hDllHandle, uint nReason, IntPtr Reserved)
        {
            const int DLL_PROCESS_ATTACH = 1;

            if (nReason == DLL_PROCESS_ATTACH)
            {
                Console.WriteLine("DLL_PROCESS_ATTACH");

                SetupGameUpdateCallback();

                // CreateThread instead of System.Threading.Thread because it gets stuck in Thread.Start
                UIThreadHandle = Kernel32.CreateThread(IntPtr.Zero, 0, &Init, IntPtr.Zero, 0, IntPtr.Zero);

                Console.WriteLine($"Thread created (handle: {UIThreadHandle.ToString("X")})");
            }

            return true;
        }

        [UnmanagedCallersOnly(EntryPoint = nameof(GetUIThreadHandle))]
        public static IntPtr GetUIThreadHandle() => UIThreadHandle;

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static int Init(IntPtr parameter)
        {
            Console.WriteLine($"Init (in-game: {Util.IsInGame})");
            if (Util.IsInGame)
            {
                while (System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle == IntPtr.Zero)
                {
                    Console.WriteLine("Waiting for game window");
                    Thread.Sleep(5000);
                }
                Thread.Sleep(5000);

                Kernel32.AllocConsole();

                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetIn(new StreamReader(Console.OpenStandardInput()));
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }

            using var scriptMgr = new ScriptManager();
            var cmdMgr = new CommandManager(scriptMgr);

            cmdMgr.MainLoop();

            Console.WriteLine("Init End");

            if (Util.IsInGame)
            {
                Kernel32.FreeConsole();
            }
            return 0;
        }

        private static void SetupGameUpdateCallback()
        {
            if (!Util.IsInGame)
            {
                return;
            }

            PrevGameUpdateFunc = (delegate* unmanaged<void>)*(void**)GameUpdateFuncPtr;
            delegate* unmanaged<void> newGameUpdateFunc = &GameUpdate;
            *(void**)GameUpdateFuncPtr = newGameUpdateFunc;
        }

        [UnmanagedCallersOnly]
        private static void GameUpdate()
        {
            PrevGameUpdateFunc();

            Util.RaiseAfterGameUpdate();
        }

        // a graphics-related function ptr called after CApp::GameUpdate
        private static void* GameUpdateFuncPtr = Util.IsInGame ? (void*)Util.RVA(0x1DE32B0/*b2215*/) : null;
        private static delegate* unmanaged<void> PrevGameUpdateFunc = null;
    }
}
