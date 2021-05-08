#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;

    using ScTools.GameFiles;
    using System.Linq;

    public class Disassembler
    {
        private readonly byte[] code;
        private (string Label, ulong Hash)[] nativesTable = Array.Empty<(string, ulong)>();
        // TODO: use string labels in code instruction
        private (string Label, string String)[] stringsTable = Array.Empty<(string, string)>();
        private Dictionary<uint, int> stringIndicesById = new(); // value is index into stringsTable
        private Dictionary<uint, string> codeLabels = new();

        public Script Script { get; }
        public NativeDB? NativeDB { get; }

        public Disassembler(Script sc, NativeDB? nativeDB = null)
        {
            Script = sc ?? throw new ArgumentNullException(nameof(sc));
            NativeDB = nativeDB;
            code = MergeCodePages(sc);
        }

        public void Disassemble(TextWriter w)
        {
            var sc = Script;

            BuildNativesTable();
            BuildStringsTable();
            IdentifyCodeLabels();

            w.WriteLine(".script_name {0}", sc.Name);
            if (sc.Hash != 0)
            {
                w.WriteLine(".script_hash 0x{0:X8}", sc.Hash);
            }

            if (sc.GlobalsLengthAndBlock != 0)
            {
                w.WriteLine(".global_block {0}", sc.GlobalsBlock);
            
                if (sc.GlobalsLength != 0)
                {
                    w.WriteLine(".global");
                    var repeatedValue = 0;
                    var repeatedCount = 0;
                    foreach (var page in sc.GlobalsPages)
                    {
                        for (int i = 0; i < page.Data.Length; i++)
                        {
                            if (page.Data[i].AsUInt64 > uint.MaxValue)
                            {
                                throw new InvalidOperationException();
                            }

                            if (repeatedCount > 0 && page.Data[i].AsInt32 == repeatedValue)
                            {
                                repeatedCount++;
                            }
                            else
                            {
                                if (repeatedCount == 1)
                                {
                                    w.WriteLine(".int {0}", repeatedValue);
                                }
                                else if (repeatedCount > 0)
                                {
                                    w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                                }

                                repeatedValue = page.Data[i].AsInt32;
                                repeatedCount = 1;
                            }
                        }
                    }

                    if (repeatedCount == 1)
                    {
                        w.WriteLine(".int {0}", repeatedValue);
                    }
                    else if (repeatedCount > 0)
                    {
                        w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                    }
                }
            }

            if (sc.StaticsCount != 0)
            {
                w.WriteLine(".static");
                var repeatedValue = 0;
                var repeatedCount = 0;
                for (int i = 0; i < (sc.StaticsCount - sc.ArgsCount); i++)
                {
                    if (sc.Statics[i].AsUInt64 > uint.MaxValue)
                    {
                        throw new InvalidOperationException();
                    }

                    if (repeatedCount > 0 && sc.Statics[i].AsInt32 == repeatedValue)
                    {
                        repeatedCount++;
                    }
                    else
                    {
                        if (repeatedCount == 1)
                        {
                            w.WriteLine(".int {0}", repeatedValue);
                        }
                        else if (repeatedCount > 0)
                        {
                            w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                        }

                        repeatedValue = sc.Statics[i].AsInt32;
                        repeatedCount = 1;
                    }
                }

                if (repeatedCount == 1)
                {
                    w.WriteLine(".int {0}", repeatedValue);
                }
                else if (repeatedCount > 0)
                {
                    w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                }
            }

            if (sc.ArgsCount != 0)
            {
                w.WriteLine(".arg");
                var repeatedValue = 0;
                var repeatedCount = 0;
                for (int i = (int)(sc.StaticsCount - sc.ArgsCount); i < sc.StaticsCount; i++)
                {
                    if (sc.Statics[i].AsUInt64 > uint.MaxValue)
                    {
                        throw new InvalidOperationException();
                    }

                    if (repeatedCount > 0 && sc.Statics[i].AsInt32 == repeatedValue)
                    {
                        repeatedCount++;
                    }
                    else
                    {
                        if (repeatedCount == 1)
                        {
                            w.WriteLine(".int {0}", repeatedValue);
                        }
                        else if (repeatedCount > 0)
                        {
                            w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                        }

                        repeatedValue = sc.Statics[i].AsInt32;
                        repeatedCount = 1;
                    }
                }

                if (repeatedCount == 1)
                {
                    w.WriteLine(".int {0}", repeatedValue);
                }
                else if (repeatedCount > 0)
                {
                    w.WriteLine(".int {0} dup ({1})", repeatedCount, repeatedValue);
                }
            }

            if (stringsTable.Length != 0)
            {
                w.WriteLine(".string");
                for (int i = 0; i < stringsTable.Length; i++)
                {
                    w.WriteLine("{0}:\t.str \"{1}\"", stringsTable[i].Label, stringsTable[i].String);
                }
            }

            if (nativesTable.Length != 0)
            {
                w.WriteLine(".include");
                for (int i = 0; i < nativesTable.Length; i++)
                {
                    w.WriteLine("{0}:\t.native 0x{1:X16}", nativesTable[i].Label, nativesTable[i].Hash);
                }
            }

            if (code.Length != 0)
            {
                w.WriteLine(".code");
                IterateCode((ip, instruction) =>
                {
                    if (codeLabels.TryGetValue(ip, out var label))
                    {
                        if (label.StartsWith("lbl"))
                        {
                            w.WriteLine("\t{0}:", label);
                        }
                        else
                        {
                            if (label.StartsWith("func"))
                            {
                                w.WriteLine();
                            }
                            w.WriteLine("{0}:", label);
                        }
                    }

                    DisassembleInstruction(w, ip, instruction);
                });
            }
        }

        private void DisassembleInstruction(TextWriter w, uint ip, ReadOnlySpan<byte> inst)
        {
            var opcode = (Opcode)inst[0];
            inst = inst[1..];

            w.Write("\t\t");
            w.Write(opcode.ToString());
            if (opcode.GetNumberOfOperands() != 0)
            {
                w.Write(' ');
            }

            switch (opcode)
            {
                case Opcode.PUSH_CONST_U8:
                case Opcode.ARRAY_U8:
                case Opcode.ARRAY_U8_LOAD:
                case Opcode.ARRAY_U8_STORE:
                case Opcode.LOCAL_U8:
                case Opcode.LOCAL_U8_LOAD:
                case Opcode.LOCAL_U8_STORE:
                case Opcode.STATIC_U8:
                case Opcode.STATIC_U8_LOAD:
                case Opcode.STATIC_U8_STORE:
                case Opcode.IADD_U8:
                case Opcode.IMUL_U8:
                case Opcode.IOFFSET_U8:
                case Opcode.IOFFSET_U8_LOAD:
                case Opcode.IOFFSET_U8_STORE:
                case Opcode.TEXT_LABEL_ASSIGN_STRING:
                case Opcode.TEXT_LABEL_ASSIGN_INT:
                case Opcode.TEXT_LABEL_APPEND_STRING:
                case Opcode.TEXT_LABEL_APPEND_INT:
                    w.Write(inst[0]);
                    inst = inst[1..];
                    break;
                case Opcode.PUSH_CONST_U8_U8:
                case Opcode.LEAVE:
                    w.Write(inst[0]);
                    w.Write(", ");
                    w.Write(inst[1]);
                    inst = inst[2..];
                    break;
                case Opcode.PUSH_CONST_U8_U8_U8:
                    w.Write(inst[0]);
                    w.Write(", ");
                    w.Write(inst[1]);
                    w.Write(", ");
                    w.Write(inst[2]);
                    inst = inst[3..];
                    break;
                case Opcode.PUSH_CONST_U32:
                    w.Write(MemoryMarshal.Read<uint>(inst));
                    inst = inst[4..];
                    break;
                case Opcode.PUSH_CONST_F:
                    w.Write(MemoryMarshal.Read<float>(inst).ToString("R", CultureInfo.InvariantCulture));
                    inst = inst[4..];
                    break;
                case Opcode.NATIVE:
                    var argReturn = inst[0];
                    var nativeIndexHi = inst[1];
                    var nativeIndexLo = inst[2];

                    var argCount = (argReturn >> 2) & 0x3F;
                    var returnCount = argReturn & 0x3;
                    var nativeIndex = (nativeIndexHi << 8) | nativeIndexLo;
                    w.Write(argCount);
                    w.Write(", ");
                    w.Write(returnCount);
                    w.Write(", ");
                    if (nativeIndex >= 0 && nativeIndex < nativesTable.Length)
                    {
                        w.Write(nativesTable[nativeIndex].Label);
                    }
                    else
                    {
                        w.Write(nativeIndex);
                    }
                    inst = inst[3..];
                    break;
                case Opcode.ENTER:
                    w.Write(inst[0]);
                    w.Write(", ");
                    w.Write(MemoryMarshal.Read<ushort>(inst[1..]));
                    var nameLen = inst[3];  // TODO: get label name from here
                    inst = inst[(4 + nameLen)..];
                    break;
                case Opcode.PUSH_CONST_S16:
                case Opcode.IADD_S16:
                case Opcode.IMUL_S16:
                case Opcode.IOFFSET_S16:
                case Opcode.IOFFSET_S16_LOAD:
                case Opcode.IOFFSET_S16_STORE:
                    w.Write(MemoryMarshal.Read<short>(inst));
                    inst = inst[2..];
                    break;
                case Opcode.ARRAY_U16:
                case Opcode.ARRAY_U16_LOAD:
                case Opcode.ARRAY_U16_STORE:
                case Opcode.LOCAL_U16:
                case Opcode.LOCAL_U16_LOAD:
                case Opcode.LOCAL_U16_STORE:
                case Opcode.STATIC_U16:
                case Opcode.STATIC_U16_LOAD:
                case Opcode.STATIC_U16_STORE:
                case Opcode.GLOBAL_U16:
                case Opcode.GLOBAL_U16_LOAD:
                case Opcode.GLOBAL_U16_STORE:
                    w.Write(MemoryMarshal.Read<ushort>(inst));
                    inst = inst[2..];
                    break;
                case Opcode.J:
                case Opcode.JZ:
                case Opcode.IEQ_JZ:
                case Opcode.INE_JZ:
                case Opcode.IGT_JZ:
                case Opcode.IGE_JZ:
                case Opcode.ILT_JZ:
                case Opcode.ILE_JZ:
                    var jumpOffset = MemoryMarshal.Read<short>(inst);
                    var jumpAddress = ip + 3 + jumpOffset;
                    w.Write(codeLabels.TryGetValue((uint)jumpAddress, out var label) ? label : jumpOffset);
                    inst = inst[2..];
                    break;
                case Opcode.CALL:
                case Opcode.GLOBAL_U24:
                case Opcode.GLOBAL_U24_LOAD:
                case Opcode.GLOBAL_U24_STORE:
                case Opcode.PUSH_CONST_U24:
                    var lo = inst[0];
                    var mi = inst[1];
                    var hi = inst[2];

                    var value = (hi << 16) | (mi << 8) | lo;
                    if (opcode is Opcode.CALL)
                    {
                        w.Write(codeLabels.TryGetValue((uint)value, out var funcLabel) ? funcLabel : value);
                    }
                    else
                    {
                        w.Write(value);
                    }
                    inst = inst[3..];
                    break;
                case Opcode.SWITCH:
                    var caseCount = inst[0];
                    inst = inst[1..];
                    for (int i = 0; i < caseCount; i++, inst = inst[6..])
                    {
                        var caseValue = MemoryMarshal.Read<uint>(inst);
                        var caseJumpToOffset = MemoryMarshal.Read<short>(inst[4..]);
                        var caseJumpToAddress = ip + 2 + 6 * (i + 1) + caseJumpToOffset;

                        if (i != 0)
                        {
                            w.Write(", ");
                        }
                        w.Write("{0}:{1}", caseValue, codeLabels.TryGetValue((uint)caseJumpToAddress, out var caseLabel) ? caseLabel : caseJumpToOffset);
                    }
                    break;
            }

            w.WriteLine();
        }

        private void BuildNativesTable()
        {
            var sc = Script;

            nativesTable = new (string, ulong)[sc.NativesCount];
            for (int i = 0; i < sc.NativesCount; i++)
            {
                var hash = sc.NativeHash(i);
                var origHash = NativeDB?.FindOriginalHash(hash) ?? hash;
                var label = NativeDB?.GetDefinition(origHash)?.Name ?? $"_0x{origHash:X16}";

                nativesTable[i] = (label, origHash);
            }
        }

        private void BuildStringsTable()
        {
            stringIndicesById.Clear();
            stringsTable = Array.Empty<(string, string)>();

            var sc = Script;
            if (sc.StringsLength != 0)
            {
                var usedLabels = new Dictionary<string, int>();
                var table = new List<(string Label, string String)>();

                int i = 0;
                foreach (uint sid in sc.StringIds())
                {
                    var str = sc.String(sid).Escape();
                    var label = CreateLabelForString(str, usedLabels);
                    table.Add((label, str));
                    i++;
                    stringIndicesById.Add(sid, i);
                }

                stringsTable = table.ToArray();
            }

            static string CreateLabelForString(string s, Dictionary<string, int> usedLabels)
            {
                const string Prefix = "a";
                const int MaxLength = 25;

                var label = string.IsNullOrWhiteSpace(s) ?
                    Prefix + "EmptyString" :
                    Prefix + char.ToUpperInvariant(s[0]) + string.Concat(s.Skip(1).Where(IsIdentifierChar).Take(MaxLength));

                // check if the string label is repeated
                if (usedLabels.TryGetValue(label, out var n))
                {
                    usedLabels[label]++;
                    label += "_" + (n + 1);
                }
                else
                {
                    usedLabels.Add(label, 1);
                }

                return label;
            }

            // char is [a-zA-Z_0-9]
            static bool IsIdentifierChar(char c)
                => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9');
        }

        private void IdentifyCodeLabels()
        {
            codeLabels.Clear();

            if (code.Length != 0)
            {
                codeLabels.Add(0, "main");
                IterateCode((ip, inst) =>
                {
                    var opcode = (Opcode)inst[0];
                    switch (opcode)
                    {
                        case Opcode.J:
                        case Opcode.JZ:
                        case Opcode.IEQ_JZ:
                        case Opcode.INE_JZ:
                        case Opcode.IGT_JZ:
                        case Opcode.IGE_JZ:
                        case Opcode.ILT_JZ:
                        case Opcode.ILE_JZ:
                            var jumpOffset = MemoryMarshal.Read<short>(inst[1..]);
                            var jumpAddress = ip + 3 + jumpOffset;
                            AddLabel(codeLabels, (uint)jumpAddress);
                            break;
                        case Opcode.SWITCH:
                            var caseCount = inst[1];
                            for (int i = 0; i < caseCount; i++)
                            {
                                var caseSpan = inst.Slice(2 + 6 * i, 6);
                                var caseValue = MemoryMarshal.Read<uint>(caseSpan);
                                var caseJumpToOffset = MemoryMarshal.Read<short>(caseSpan[4..]);
                                var caseJumpToAddress = ip + 2 + 6 * (i + 1) + caseJumpToOffset;
                                AddLabel(codeLabels, (uint)caseJumpToAddress);
                            }
                            break;
                        case Opcode.CALL:
                            var lo = inst[0];
                            var mi = inst[1];
                            var hi = inst[2];

                            var callAddress = (hi << 16) | (mi << 8) | lo;
                            AddFuncLabel(codeLabels, (uint)callAddress);
                            break;
                        case Opcode.ENTER:
                            AddFuncLabel(codeLabels, ip);
                            break;
                    }
                });
            }

            static void AddFuncLabel(Dictionary<uint, string> codeLabels, uint address)
                => codeLabels.TryAdd(address, "func_" + address);
            static void AddLabel(Dictionary<uint, string> codeLabels, uint address)
                => codeLabels.TryAdd(address, "lbl_" + address);
        }

        private delegate void IterateCodeCallback(uint ip, ReadOnlySpan<byte> instruction);
        private void IterateCode(IterateCodeCallback callback)
        {
            uint ip = 0;
            var codeSpan = code.AsSpan();
            while (!codeSpan.IsEmpty)
            {
                var instructionLength = 1;
                var opcode = (Opcode)codeSpan[0];

                switch (opcode)
                {
                    case Opcode.PUSH_CONST_U8:
                    case Opcode.ARRAY_U8:
                    case Opcode.ARRAY_U8_LOAD:
                    case Opcode.ARRAY_U8_STORE:
                    case Opcode.LOCAL_U8:
                    case Opcode.LOCAL_U8_LOAD:
                    case Opcode.LOCAL_U8_STORE:
                    case Opcode.STATIC_U8:
                    case Opcode.STATIC_U8_LOAD:
                    case Opcode.STATIC_U8_STORE:
                    case Opcode.IADD_U8:
                    case Opcode.IMUL_U8:
                    case Opcode.IOFFSET_U8:
                    case Opcode.IOFFSET_U8_LOAD:
                    case Opcode.IOFFSET_U8_STORE:
                    case Opcode.TEXT_LABEL_ASSIGN_STRING:
                    case Opcode.TEXT_LABEL_ASSIGN_INT:
                    case Opcode.TEXT_LABEL_APPEND_STRING:
                    case Opcode.TEXT_LABEL_APPEND_INT:
                        instructionLength = 2;
                        break;
                    case Opcode.PUSH_CONST_U8_U8:
                    case Opcode.LEAVE:
                    case Opcode.PUSH_CONST_S16:
                    case Opcode.IADD_S16:
                    case Opcode.IMUL_S16:
                    case Opcode.IOFFSET_S16:
                    case Opcode.IOFFSET_S16_LOAD:
                    case Opcode.IOFFSET_S16_STORE:
                    case Opcode.ARRAY_U16:
                    case Opcode.ARRAY_U16_LOAD:
                    case Opcode.ARRAY_U16_STORE:
                    case Opcode.LOCAL_U16:
                    case Opcode.LOCAL_U16_LOAD:
                    case Opcode.LOCAL_U16_STORE:
                    case Opcode.STATIC_U16:
                    case Opcode.STATIC_U16_LOAD:
                    case Opcode.STATIC_U16_STORE:
                    case Opcode.GLOBAL_U16:
                    case Opcode.GLOBAL_U16_LOAD:
                    case Opcode.GLOBAL_U16_STORE:
                    case Opcode.J:
                    case Opcode.JZ:
                    case Opcode.IEQ_JZ:
                    case Opcode.INE_JZ:
                    case Opcode.IGT_JZ:
                    case Opcode.IGE_JZ:
                    case Opcode.ILT_JZ:
                    case Opcode.ILE_JZ:
                        instructionLength = 3;
                        break;
                    case Opcode.PUSH_CONST_U8_U8_U8:
                    case Opcode.NATIVE:
                    case Opcode.CALL:
                    case Opcode.GLOBAL_U24:
                    case Opcode.GLOBAL_U24_LOAD:
                    case Opcode.GLOBAL_U24_STORE:
                    case Opcode.PUSH_CONST_U24:
                        instructionLength = 4;
                        break;
                    case Opcode.PUSH_CONST_U32:
                    case Opcode.PUSH_CONST_F:
                        instructionLength = 5;
                        break;
                    case Opcode.ENTER:
                        var nameLen = codeSpan[4];
                        instructionLength = 5 + nameLen;
                        break;
                    case Opcode.SWITCH:
                        var caseCount = codeSpan[1];
                        instructionLength = 2 + 6 * caseCount;
                        break;
                }

                callback(ip, codeSpan[0..instructionLength]);
                codeSpan = codeSpan[instructionLength..];
                ip += (uint)instructionLength;
            }
        }

        private static byte[] MergeCodePages(Script sc)
        {
            var buffer = new byte[sc.CodeLength];
            var offset = 0;
            foreach (var page in sc.CodePages)
            {
                page.Data.CopyTo(buffer.AsSpan(offset));
                offset += page.Data.Length;
            }
            return buffer;
        }

        public static void Disassemble(TextWriter output, Script sc, NativeDB? nativeDB = null)
        {
            var a = new Disassembler(sc, nativeDB);
            a.Disassemble(output);
        }
    }
}
