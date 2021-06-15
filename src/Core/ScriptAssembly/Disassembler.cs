#nullable enable
namespace ScTools.ScriptAssembly
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;

    using ScTools.GameFiles;

    public class Disassembler
    {
        private const string CodeFuncPrefix = "func_",
                             CodeLabelPrefix = "lbl_",
                             StringLabelPrefix = "a",
                             StaticLabelPrefix = "s_",
                             ArgLabelPrefix = "arg_";

        private readonly byte[] code;
        private (string Label, ulong Hash)[] nativesTable = Array.Empty<(string, ulong)>();
        private (string Label, string String)[] stringsTable = Array.Empty<(string, string)>();
        private readonly Dictionary<uint, int> stringIndicesById = new(); // value is index into stringsTable
        private readonly Dictionary<uint, string> codeLabels = new();
        private readonly Dictionary<uint, string> staticsLabels = new();
        private bool hasIndirectCalls = false;

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
            IdentifyStaticsLabels();

            w.WriteLine(".script_name {0}", sc.Name);
            if (sc.Hash != 0)
            {
                w.WriteLine(".script_hash 0x{0:X8}", sc.Hash);
            }

            WriteGlobalsSegment(w);

            WriteStaticsSegment(w);

            WriteArgsSegment(w);

            WriteStringsSegment(w);

            WriteIncludeSegment(w);

            WriteCodeSegment(w);
        }

        private void WriteGlobalsValues(TextWriter w)
        {
            var sc = Script;
            int repeatedValue = 0;
            int repeatedCount = 0;
            foreach (var page in sc.GlobalsPages)
            {
                var values = page.Data;
                for (int i = 0; i < values.Length; i++)
                {
                    Debug.Assert(values[i].AsUInt64 <= uint.MaxValue, $"{nameof(WriteGlobalsValues)} only handles 32-bit values");

                    var v = values[i].AsInt32;
                    if (repeatedCount > 0 && v != repeatedValue)
                    {
                        FlushValue();
                    }

                    repeatedValue = v;
                    repeatedCount++;
                }
            }

            FlushValue();

            void FlushValue()
            {
                if (repeatedCount > 1)
                {
                    w.WriteLine("\t\t.int {0} dup ({1})", repeatedCount, repeatedValue);
                }
                else if (repeatedCount == 1)
                {
                    w.WriteLine("\t\t.int {0}", repeatedValue);
                }

                repeatedCount = 0;
            }
        }

        private void WriteGlobalsSegment(TextWriter w)
        {
            var sc = Script;
            if (sc.GlobalsLengthAndBlock == 0)
            {
                return;
            }

            w.WriteLine(".global_block {0}", sc.GlobalsBlock);

            if (sc.GlobalsLength == 0)
            {
                return;
            }
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

        private void WriteStaticsValues(TextWriter w, uint from, uint toExclusive)
        {
            var sc = Script;
            int repeatedValue = 0;
            int repeatedCount = 0;
            for (uint i = from; i < toExclusive; i++)
            {
                Debug.Assert(sc.Statics[i].AsUInt64 <= uint.MaxValue, $"{nameof(WriteStaticsValues)} only handles 32-bit values");

                if (staticsLabels.TryGetValue(i, out var label))
                {
                    FlushValue();
                    w.WriteLine("\t{0}:", label);
                }

                var v = sc.Statics[i].AsInt32;
                if (repeatedCount > 0 && v != repeatedValue)
                {
                    FlushValue();
                }

                repeatedValue = v;
                repeatedCount++;
            }

            FlushValue();

            void FlushValue()
            {
                if (repeatedCount > 1)
                {
                    w.WriteLine("\t\t.int {0} dup ({1})", repeatedCount, repeatedValue);
                }
                else if (repeatedCount == 1)
                {
                    w.WriteLine("\t\t.int {0}", repeatedValue);
                }

                repeatedCount = 0;
            }
        }

        private void WriteStaticsSegment(TextWriter w)
        {
            var sc = Script;
            if (sc.StaticsCount == 0)
            {
                return;
            }

            w.WriteLine(".static");
            var numStatics = sc.StaticsCount - sc.ArgsCount;
            WriteStaticsValues(w, from: 0, toExclusive: numStatics);
            w.WriteLine();
        }

        private void WriteArgsSegment(TextWriter w)
        {
            var sc = Script;
            if (sc.ArgsCount == 0)
            {
                return;
            }

            w.WriteLine(".arg");
            WriteStaticsValues(w, from: sc.StaticsCount - sc.ArgsCount, toExclusive: sc.StaticsCount);
            w.WriteLine();
        }

        private void WriteStringsSegment(TextWriter w)
        {
            if (stringsTable.Length == 0)
            {
                return;
            }

            w.WriteLine(".string");
            for (int i = 0; i < stringsTable.Length; i++)
            {
                w.WriteLine("\t{0}:\t.str \"{1}\"", stringsTable[i].Label, stringsTable[i].String);
            }
            w.WriteLine();
        }

        private void WriteIncludeSegment(TextWriter w)
        {
            if (nativesTable.Length == 0)
            {
                return;
            }

            w.WriteLine(".include");
            for (int i = 0; i < nativesTable.Length; i++)
            {
                w.WriteLine("\t{0}:\t.native 0x{1:X16}", nativesTable[i].Label, nativesTable[i].Hash);
            }
            w.WriteLine();
        }

        private void WriteCodeSegment(TextWriter w)
        {
            if (code.Length == 0)
            {
                return;
            }

            w.WriteLine(".code");
            IterateCode(inst =>
            {
                TryWriteLabel(inst.Address);

                DisassembleInstruction(w, inst, inst.Address, inst.Bytes);
            });

            // in case we have label pointing to the end of the code
            TryWriteLabel((uint)code.Length);


            void TryWriteLabel(uint address)
            {
                if (codeLabels.TryGetValue(address, out var label))
                {
                    if (label.StartsWith(CodeLabelPrefix))
                    {
                        w.WriteLine("\t{0}:", label);
                    }
                    else
                    {
                        // add a new line to visually separate this function from the previous one
                        w.WriteLine();
                        w.WriteLine("{0}:", label);
                    }
                }
            }
        }

        private void DisassembleInstruction(TextWriter w, InstructionContext ctx, uint ip, ReadOnlySpan<byte> inst)
        {
            var opcode = (Opcode)inst[0];
            inst = inst[1..];

            w.Write("\t\t");
            w.Write(opcode.ToString());
            if (opcode.NumberOfOperands() != 0)
            {
                w.Write(' ');
            }

            switch (opcode)
            {
                case Opcode.PUSH_CONST_U8:
                    if (!TryWriteStringLabel(this, w, ctx, inst[0]))
                    {
                        w.Write(inst[0]);
                    }
                    inst = inst[1..];
                    break;
                case Opcode.ARRAY_U8:
                case Opcode.ARRAY_U8_LOAD:
                case Opcode.ARRAY_U8_STORE:
                case Opcode.LOCAL_U8:
                case Opcode.LOCAL_U8_LOAD:
                case Opcode.LOCAL_U8_STORE:
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
                case Opcode.STATIC_U8:
                case Opcode.STATIC_U8_LOAD:
                case Opcode.STATIC_U8_STORE:
                    var staticU8 = inst[0];
                    w.Write(staticsLabels.TryGetValue(staticU8, out var staticU8Label) ? staticU8Label : staticU8);
                    inst = inst[1..];
                    break;
                case Opcode.PUSH_CONST_U8_U8:
                case Opcode.LEAVE:
                    w.Write(inst[0]);
                    w.Write(", ");
                    if (!TryWriteStringLabel(this, w, ctx, inst[1]))
                    {
                        w.Write(inst[1]);
                    }
                    inst = inst[2..];
                    break;
                case Opcode.PUSH_CONST_U8_U8_U8:
                    w.Write(inst[0]);
                    w.Write(", ");
                    w.Write(inst[1]);
                    w.Write(", ");
                    if (!TryWriteStringLabel(this, w, ctx, inst[2]))
                    {
                        w.Write(inst[2]);
                    }
                    inst = inst[3..];
                    break;
                case Opcode.PUSH_CONST_U32:
                    var u32Value = MemoryMarshal.Read<uint>(inst);
                    if (!TryWriteStringLabel(this, w, ctx, u32Value))
                    {
                        w.Write(u32Value);
                    }
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
                    var nameLen = inst[3];
                    inst = inst[(4 + nameLen)..];
                    break;
                case Opcode.PUSH_CONST_S16:
                {
                    var s16Value = MemoryMarshal.Read<short>(inst);
                    if (!TryWriteStringLabel(this, w, ctx, (uint)s16Value))
                    {
                        w.Write(s16Value);
                    }
                    inst = inst[2..];
                    break;
                }
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
                case Opcode.GLOBAL_U16:
                case Opcode.GLOBAL_U16_LOAD:
                case Opcode.GLOBAL_U16_STORE:
                    w.Write(MemoryMarshal.Read<ushort>(inst));
                    inst = inst[2..];
                    break;
                case Opcode.STATIC_U16:
                case Opcode.STATIC_U16_LOAD:
                case Opcode.STATIC_U16_STORE:
                    var staticU16 = MemoryMarshal.Read<ushort>(inst);
                    w.Write(staticsLabels.TryGetValue(staticU16, out var staticU16Label) ? staticU16Label : staticU16);
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
                {
                    var lo = inst[0];
                    var mi = inst[1];
                    var hi = inst[2];

                    var value = (hi << 16) | (mi << 8) | lo;
                    if (!(opcode is Opcode.CALL && TryWriteFuncLabel(this, w, (uint)value)) &&
                        !(opcode is Opcode.PUSH_CONST_U24 && (TryWriteStringLabel(this, w, ctx, (uint)value) || (hasIndirectCalls && TryWriteFuncLabel(this, w, (uint)value)))))
                    {
                        w.Write(value);
                    }
                    inst = inst[3..];
                    break;
                }
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
                case Opcode.PUSH_CONST_0:
                case Opcode.PUSH_CONST_1:
                case Opcode.PUSH_CONST_2:
                case Opcode.PUSH_CONST_3:
                case Opcode.PUSH_CONST_4:
                case Opcode.PUSH_CONST_5:
                case Opcode.PUSH_CONST_6:
                case Opcode.PUSH_CONST_7:
                {
                    var value = opcode - Opcode.PUSH_CONST_0;
                    if (TryGetStringIndex(this, w, ctx, value, out int strIndex))
                    {
                        w.Write("\t; string ref: {0}", stringsTable[strIndex].Label);
                    }
                    break;
                }
            }

            w.WriteLine();

            static bool TryGetStringIndex(Disassembler self, TextWriter w, in InstructionContext ctx, uint strId, out int strIndex)
            {
                var next = ctx.Next();
                if (next.IsValid && next.Opcode is Opcode.STRING && self.stringIndicesById.TryGetValue(strId, out strIndex))
                {
                    return true;
                }
                strIndex = default;
                return false;
            }

            static bool TryWriteStringLabel(Disassembler self, TextWriter w, in InstructionContext ctx, uint strId)
            {
                if (TryGetStringIndex(self, w, ctx, strId, out int strIndex))
                {
                    w.Write(self.stringsTable[strIndex].Label);
                    return true;
                }
                return false;
            }

            static bool TryWriteFuncLabel(Disassembler self, TextWriter w, uint addr)
            {
                if (self.codeLabels.TryGetValue(addr, out var funcLabel) && !funcLabel.StartsWith(CodeLabelPrefix))
                {
                    w.Write(funcLabel);
                    return true;
                }
                return false;
            }
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
                var usedLabels = new Dictionary<string, int>(Assembler.CaseInsensitiveComparer);
                var table = new List<(string Label, string String)>();

                int i = 0;
                foreach (uint sid in sc.StringIds())
                {
                    var str = sc.String(sid).Escape();
                    var label = CreateLabelForString(str, usedLabels);
                    table.Add((label, str));
                    stringIndicesById.Add(sid, i);
                    i++;
                }

                stringsTable = table.ToArray();
            }

            static string CreateLabelForString(string s, Dictionary<string, int> usedLabels)
            {
                const int MaxLength = 25;

                var label = string.IsNullOrWhiteSpace(s) ?
                    StringLabelPrefix + "EmptyString" :
                    StringLabelPrefix + string.Concat(s.Where(IsIdentifierChar)
                                                       .Take(MaxLength)
                                                       .Select((c, i) => i == 0 ? char.ToUpperInvariant(c) : c)); // make the first char uppercase

                // check if the string label is repeated
                if (usedLabels.TryGetValue(label, out var n))
                {
                    string newLabel;
                    do
                    {
                        n++;
                        newLabel = label + "_" + n;
                    } while (usedLabels.ContainsKey(newLabel)); // if another string already generated a label like newLabel, increment n and try again

                    usedLabels[label] = n;
                    label = newLabel;
                }

                // add the label to the dictionary if it is the first time it appears
                usedLabels.TryAdd(label, 1);

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
                var addressAfterLastLeaveInst = 0u;
                IterateCode(inst =>
                {
                    switch (inst.Opcode)
                    {
                        case Opcode.J:
                        case Opcode.JZ:
                        case Opcode.IEQ_JZ:
                        case Opcode.INE_JZ:
                        case Opcode.IGT_JZ:
                        case Opcode.IGE_JZ:
                        case Opcode.ILT_JZ:
                        case Opcode.ILE_JZ:
                            // ignore labels that come after a LEAVE instruction,
                            // R* compiler inserts them sometimes with the next function or label as target, bug?
                            if (addressAfterLastLeaveInst != inst.Address)
                            {
                                var jumpOffset = MemoryMarshal.Read<short>(inst.Bytes[1..]);
                                var jumpAddress = inst.Address + 3 + jumpOffset;
                                AddLabel(codeLabels, (uint)jumpAddress);
                            }
                            break;
                        case Opcode.SWITCH:
                            var caseCount = inst.Bytes[1];
                            for (int i = 0; i < caseCount; i++)
                            {
                                var caseSpan = inst.Bytes.Slice(2 + 6 * i, 6);
                                var caseValue = MemoryMarshal.Read<uint>(caseSpan);
                                var caseJumpToOffset = MemoryMarshal.Read<short>(caseSpan[4..]);
                                var caseJumpToAddress = inst.Address + 2 + 6 * (i + 1) + caseJumpToOffset;
                                AddLabel(codeLabels, (uint)caseJumpToAddress);
                            }
                            break;
                        case Opcode.ENTER:
                            var funcAddress = inst.Address;
                            // Functions at page boundaries may not start with an ENTER instruction, they have NOPs and a J before
                            // the ENTER to skip the page boundary.
                            // To solve those cases, we check if the ENTER comes after a LEAVE instruction, if it doesn't we use the address
                            // after the LEAVE as the function address, which should at least be correct for vanilla scripts
                            if (addressAfterLastLeaveInst != inst.Address)
                            {
                                funcAddress = addressAfterLastLeaveInst;
                            }

                            var funcNameLen = inst.Bytes[4];
                            var funcName = funcNameLen > 0 ?
                                                System.Text.Encoding.UTF8.GetString(inst.Bytes.Slice(5, funcNameLen - 1)) :
                                                (funcAddress == 0 ? "main" : null);
                            AddFuncLabel(codeLabels, funcAddress, funcName);
                            break;
                        case Opcode.LEAVE:
                            addressAfterLastLeaveInst = (uint)(inst.Address + Opcode.LEAVE.ByteSize());
                            break;
                        case Opcode.CALLINDIRECT:
                            hasIndirectCalls = true;
                            break;
                    }
                });
            }

            static void AddFuncLabel(Dictionary<uint, string> codeLabels, uint address, string? name)
                => codeLabels.TryAdd(address, name ?? CodeFuncPrefix + address);
            static void AddLabel(Dictionary<uint, string> codeLabels, uint address)
                => codeLabels.TryAdd(address, CodeLabelPrefix + address);
        }

        private void IdentifyStaticsLabels()
        {
            staticsLabels.Clear();

            if (code.Length != 0)
            {
                IterateCode(inst =>
                {
                    uint? staticAddress = inst.Opcode switch
                    {
                        Opcode.STATIC_U8 or
                        Opcode.STATIC_U8_LOAD or
                        Opcode.STATIC_U8_STORE => inst.Bytes[1],

                        Opcode.STATIC_U16 or
                        Opcode.STATIC_U16_LOAD or
                        Opcode.STATIC_U16_STORE => MemoryMarshal.Read<ushort>(inst.Bytes[1..]),

                        _ => null,
                    };

                    if (staticAddress.HasValue)
                    {
                        AddStaticLabel(Script, staticsLabels, staticAddress.Value);
                    }
                });
            }

            static void AddStaticLabel(Script sc, Dictionary<uint, string> statisLabels, uint address)
            {
                var argsStart = sc.StaticsCount - sc.ArgsCount;
                var label = address < argsStart ?
                    StaticLabelPrefix + address :
                    ArgLabelPrefix + (address - argsStart);

                statisLabels.TryAdd(address, label);
            }
        }

        private delegate void IterateCodeCallback(InstructionContext instruction);
        private void IterateCode(IterateCodeCallback callback)
        {
            InstructionContext.CB previousCB = currInst =>
            {
                uint prevAddress = 0;
                uint address = 0;
                while (address < currInst.Address)
                {
                    prevAddress = address;
                    address += (uint)GetInstructionLength(code, address);
                }
                return GetInstructionContext(code, prevAddress, currInst.PreviousCB, currInst.NextCB);
            };
            InstructionContext.CB nextCB = currInst =>
            {
                var nextAddress = currInst.Address + (uint)currInst.Bytes.Length;
                return GetInstructionContext(code, nextAddress, currInst.PreviousCB, currInst.NextCB);
            };

            uint ip = 0;
            while (ip < code.Length)
            {
                var inst = GetInstructionContext(code, ip, previousCB, nextCB);
                callback(inst);
                ip += (uint)inst.Bytes.Length;
            }

            static InstructionContext GetInstructionContext(byte[] code, uint address, InstructionContext.CB previousCB, InstructionContext.CB nextCB)
                => address >= code.Length ? default : new()
                {
                    Address = address,
                    Bytes = code.AsSpan((int)address, GetInstructionLength(code, address)),
                    PreviousCB = previousCB,
                    NextCB = nextCB,
                };

            static int GetInstructionLength(byte[] code, uint address)
            {
                var opcode = (Opcode)code[address];
                return opcode switch
                {
                    Opcode.ENTER => 5 + code[address + 4],  // 5 + nameLength
                    Opcode.SWITCH => 2 + 6 * code[address + 1], // 2 + 6 * caseCount
                    _ => opcode.ByteSize(),
                };
            }
        }

        private readonly ref struct InstructionContext
        {
            public delegate InstructionContext CB(InstructionContext curr);

            public bool IsValid => Bytes.Length > 0;
            public uint Address { get; init; }
            public ReadOnlySpan<byte> Bytes { get; init; }
            public Opcode Opcode => (Opcode)Bytes[0];
            public CB PreviousCB { get; init; }
            public CB NextCB { get; init; }

            public InstructionContext Previous() => PreviousCB(this);
            public InstructionContext Next() => NextCB(this);
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
