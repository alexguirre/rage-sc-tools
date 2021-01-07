namespace ScTools.Tests.ScriptLang
{
    using System.IO;

    using ScTools.ScriptLang;
    using ScTools.ScriptLang.Ast;
    using ScTools.ScriptLang.Semantics;
    using ScTools.ScriptLang.Semantics.Binding;

    using Xunit;

    public class ImplicitConversionsTests
    {
        [Fact]
        public void TestConversionToEntityIndex()
        {
            var pedToEnt = Util.Compile($@"
                PROC MAIN()
                    PED_INDEX ped
                    ENTITY_INDEX ent = ped
                    P(ped)
                ENDPROC

                PROC P(ENTITY_INDEX e)
                ENDPROC
            ");
            Assert.False(pedToEnt.GetAllDiagnostics().HasErrors);

            var vehToEnt = Util.Compile($@"
                PROC MAIN()
                    VEHICLE_INDEX veh
                    ENTITY_INDEX ent = veh
                    P(veh)
                ENDPROC

                PROC P(ENTITY_INDEX e)
                ENDPROC
            ");
            Assert.False(vehToEnt.GetAllDiagnostics().HasErrors);

            var objToEnt = Util.Compile($@"
                PROC MAIN()
                    OBJECT_INDEX obj
                    ENTITY_INDEX ent = obj
                    P(obj)
                ENDPROC

                PROC P(ENTITY_INDEX e)
                ENDPROC
            ");
            Assert.False(objToEnt.GetAllDiagnostics().HasErrors);
        }
    }
}
