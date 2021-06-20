namespace ScTools.ScriptLang.Semantics
{
    using System.Linq;

    using ScTools.ScriptLang.Ast.Declarations;
    using ScTools.ScriptLang.Ast.Types;

    internal static class TypeHelper
    {
        /// <summary>
        /// Gets whether <paramref name="type"/> matches the specified struct declaration or contains it recursively (as an array item type or as a inner field).
        /// </summary>
        public static bool IsOrContainsStruct(IType type, StructDeclaration structDecl)
        {
            if (type is StructType structTy)
            {
                return structTy.Declaration == structDecl || structTy.Declaration.Fields.Any(f => IsOrContainsStruct(f.Type, structDecl));
            }
            else if (type is ArrayType arrayTy)
            {
                return IsOrContainsStruct(arrayTy.ItemType, structDecl);
            }

            return false;
        }

        /// <summary>
        /// Gets whether <paramref name="type"/> can be passed between script threads safely.
        /// <para>
        /// Unsafe types:
        /// <list type="bullet">
        /// <item>
        ///     <term>References</term>
        ///     <description>
        ///         References are stored as a native memory address, normally from memory
        ///         owned by the script thread. When the owning script thread is terminated,
        ///         the memory will be freed and re-used by another script thread.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term>Function Pointers</term>
        ///     <description>
        ///         Function pointers are stored as an address specific to the script program,
        ///         so in script threads with different program they will point to completely
        ///         different code.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term>STRING</term>
        ///     <description>
        ///         STRINGs are stored as a native memory address, normally from memory owned by
        ///         the script program. When the owning script program is unloaded, the memory
        ///         will be freed.
        ///         Instead, TEXT_LABELs can be used.
        ///     </description>
        /// </item>
        /// <item>
        ///     <term>Structs or arrays containing any other unsafe type</term>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        public static bool IsCrossScriptThreadSafe(IType type)
        {
            if (type is RefType or FuncType or StringType)
            {
                return false;
            }
            else if (type is StructType structTy)
            {
                return structTy.Declaration.Fields.All(f => IsCrossScriptThreadSafe(f.Type));
            }
            else if (type is IArrayType arrayTy)
            {
                return IsCrossScriptThreadSafe(arrayTy.ItemType);
            }

            return true;
        }
    }
}
