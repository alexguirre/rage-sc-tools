namespace ScTools.Tests
{
    using System;
    using System.IO;
    using System.Linq;

    using ScTools.GameFiles;
    using ScTools.ScriptAssembly;
    using ScTools.ScriptAssembly.Targets.Five;
    using ScTools.ScriptLang;

    using Xunit;

    internal static class Util
    {
        public static Assembler Assemble(string src, string path = "test.sc")
        {
            using var r = new StringReader(src);
            return Assembler.Assemble(r, path);
        }

        public static string Disassemble(GameFiles.Five.Script sc)
        {
            using var w = new StringWriter();
            Disassembler.Disassemble(w, sc);
            return w.ToString();
        }

        public static void AssertScriptsAreEqual(GameFiles.Five.Script sc1, GameFiles.Five.Script sc2)
        {
            static void FailIf(bool condition, string message) => Assert.False(condition, message);
            static string S(string str) => str;

            FailIf(sc1.GlobalsSignature != sc2.GlobalsSignature, S("Hash is different"));
            FailIf(sc1.Name != sc2.Name, S("Name is different"));
            FailIf(sc1.NameHash != sc2.NameHash, S("NameHash is different"));
            FailIf(sc1.NumRefs != sc2.NumRefs, S("NumRefs is different"));

            FailIf(sc1.CodeLength != sc2.CodeLength, S("CodeLength is different"));
            if (sc1.CodePages != null && sc2.CodePages != null)
            {
                var codePageIdx = 0;
                foreach (var (page1, page2) in sc1.CodePages.Zip(sc2.CodePages))
                {
                    var equal = page1.Data.SequenceEqual(page2.Data);
                    FailIf(!equal, S($"CodePage #{codePageIdx} is different"));
                    if (!equal)
                    {
                        //File.WriteAllText($"test_original_code_page_{codePageIdx}.txt", string.Join(' ', page1.Data.Select(b => b.ToString("X2"))));
                        //File.WriteAllText($"test_new_code_page_{codePageIdx}.txt", string.Join(' ', page2.Data.Select(b => b.ToString("X2"))));
                    }
                    codePageIdx++;
                }
            }

            FailIf(sc1.StaticsCount != sc2.StaticsCount, S("StaticsCount is different"));
            FailIf(sc1.ArgsCount != sc2.ArgsCount, S("ArgsCount is different"));
            FailIf((sc1.Statics != null) != (sc2.Statics != null), S("Statics null"));
            if (sc1.Statics != null && sc2.Statics != null)
            {
                var staticIdx = 0;
                foreach (var (static1, static2) in sc1.Statics.Zip(sc2.Statics))
                {
                    FailIf(static1.AsUInt64 != static2.AsUInt64, S($"Static #{staticIdx} is different"));
                    staticIdx++;
                }
            }

            FailIf(sc1.GlobalsLength != sc2.GlobalsLength, S("GlobalsLength is different"));
            FailIf(sc1.GlobalsBlock != sc2.GlobalsBlock, S("GlobalsBlock is different"));
            FailIf((sc1.GlobalsPages != null) != (sc2.GlobalsPages != null), S("Globals null"));
            if (sc1.GlobalsPages != null && sc2.GlobalsPages != null)
            {
                var globalIdx = 0;
                foreach (var (page1, page2) in sc1.GlobalsPages.Zip(sc2.GlobalsPages))
                {
                    foreach (var (global1, global2) in page1.Data.Zip(page2.Data))
                    {
                        FailIf(global1.AsUInt64 != global2.AsUInt64, S($"Global #{globalIdx} is different"));
                        globalIdx++;
                    }
                }
            }

            FailIf(sc1.NativesCount != sc2.NativesCount, S("NativesCount is different"));
            FailIf((sc1.Natives != null) != (sc2.Natives != null), S("Natives null"));
            if (sc1.Natives != null && sc2.Natives != null)
            {
                FailIf(!sc1.Natives.SequenceEqual(sc2.Natives), S("Natives is different"));
            }

            FailIf(sc1.StringsLength != sc2.StringsLength, S("StringsLength is different"));
            if (sc1.StringsPages != null && sc2.StringsPages != null)
            {
                var stringPageIdx = 0;
                foreach (var (page1, page2) in sc1.StringsPages.Zip(sc2.StringsPages))
                {
                    FailIf(!page1.Data.SequenceEqual(page2.Data), S($"StringsPage #{stringPageIdx} is different"));
                    stringPageIdx++;
                }
            }
        }

        public static void AssertScriptsAreEqual(GameFiles.ScriptNY sc1, GameFiles.ScriptNY sc2)
        {
            static void FailIf(bool condition, string message) => False(condition, message);
            static string S(string str) => str;

            FailIf(sc1.GlobalsSignature != sc2.GlobalsSignature, S("Globals signature is different"));

            FailIf(sc1.CodeLength != sc2.CodeLength, S("CodeLength is different"));
            if (sc1.Code != null && sc2.Code != null)
            {
                True(sc1.Code.SequenceEqual(sc2.Code));
            }

            FailIf(sc1.StaticsCount != sc2.StaticsCount, S("StaticsCount is different"));
            FailIf(sc1.ArgsCount != sc2.ArgsCount, S("ArgsCount is different"));
            FailIf((sc1.Statics != null) != (sc2.Statics != null), S("Statics null"));
            if (sc1.Statics != null && sc2.Statics != null)
            {
                var staticIdx = 0;
                foreach (var (static1, static2) in sc1.Statics.Zip(sc2.Statics))
                {
                    FailIf(static1.AsUInt32 != static2.AsUInt32, S($"Static #{staticIdx} is different"));
                    staticIdx++;
                }
            }

            FailIf(sc1.GlobalsCount != sc2.GlobalsCount, S("GlobalsCount is different"));
            if (sc1.Globals != null && sc2.Globals != null)
            {
                var globalIdx = 0;
                foreach (var (global1, global2) in sc1.Globals.Zip(sc2.Globals))
                {
                    FailIf(global1.AsUInt32 != global2.AsUInt32, S($"Global #{globalIdx} is different"));
                    globalIdx++;
                }
            }
        }
    }
}
