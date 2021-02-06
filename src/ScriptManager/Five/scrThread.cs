namespace ScTools.Five
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    internal struct scrThreadId
    {
        public uint Value;

        public override string ToString() => Value.ToString();

        public static implicit operator scrThreadId(uint id) => new scrThreadId { Value = id };
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x168)]
    internal unsafe struct scrThread
    {
        [StructLayout(LayoutKind.Explicit, Size = 0x18)]
        public struct ThreadStack
        {
            [FieldOffset(0x00)] public scrThread* Thread;
            [FieldOffset(0x08)] public uint Size;
            [FieldOffset(0x10)] public IntPtr Buffer;

            public bool IsUsed => Thread != null;
        }

        public enum State : uint
        {
            Idle = 0,
            Running = 1,
            Killed = 2,
            Unk3 = 3,
            Unk4 = 4,
        }

        [StructLayout(LayoutKind.Explicit, Size = 0xA8)]
        public struct Serialized
        {
            [FieldOffset(0x00)] public scrThreadId ThreadId;
            [FieldOffset(0x04)] public scrProgramId ProgramId;
            [FieldOffset(0x08)] public State State;
        }

        [FieldOffset(0x08)] public Serialized Info;

        [FieldOffset(0xD0)] public fixed sbyte Name[64];

        public string GetName()
        {
            fixed (sbyte* n = Name)
            {
                return new string(n);
            }
        }

        public static readonly atArrayOfPtrs<scrThread>* ThreadsPtr = Util.IsInGame ? (atArrayOfPtrs<scrThread>*)Util.RVA(0x2D9C368/*b2189*/) : null;
        public static readonly atArray<ThreadStack>* StacksPtr = Util.IsInGame ? (atArray<ThreadStack>*)Util.RVA(0x2D9C378/*b2189*/) : null;
        public static ref atArrayOfPtrs<scrThread> Threads => ref Unsafe.AsRef<atArrayOfPtrs<scrThread>>(ThreadsPtr);
        public static ref atArray<ThreadStack> Stacks => ref Unsafe.AsRef<atArray<ThreadStack>>(StacksPtr);

        private static readonly void* KillThreadAddress = Util.IsInGame ? (void*)Util.RVA(0x15A9F68/*b2189*/) : null;
        public static void KillThread(scrThreadId id) => ((delegate* unmanaged<uint, void>)KillThreadAddress)(id.Value);

        private static readonly void* StartNewThreadWithNameAddress = Util.IsInGame ? (void**)Util.RVA(0xA30984/*b2189*/) : null;
        public static scrThreadId StartNewThreadWithName(string programName, IntPtr args, int argsSize, uint stackSize)
        {
            var programNameCopy = Marshal.StringToHGlobalAnsi(programName);
            var id = ((delegate* unmanaged<IntPtr, IntPtr, int, uint, uint>)StartNewThreadWithNameAddress)(programNameCopy, args, argsSize, stackSize);
            Marshal.FreeHGlobal(programNameCopy);
            return id;
        }
    }
}
