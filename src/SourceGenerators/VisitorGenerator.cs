namespace ScTools.SourceGenerators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

[Generator]
public class VisitorGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        var receiver = (SyntaxReceiver)context.SyntaxContextReceiver!;
        foreach (var (prefix, baseType, leafTypes) in receiver.Infos)
        {
            context.AddSource($"{prefix}Visitor.g.cs", GenerateVisitorsSourceFor(prefix, baseType, leafTypes));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(pi => pi.AddSource(AttributeHintName, AttributeSource));
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    private string? currentNamespace = null;
    private void OpenNamespace(StringBuilder sb, string ns)
    {
        if (currentNamespace is null || currentNamespace != ns)
        {
            if (currentNamespace is not null)
            {
                CloseNamespace(sb);
            }
            sb.AppendLine($@"namespace {ns}
{{");
            currentNamespace = ns;
        }
    }

    private void CloseNamespace(StringBuilder sb)
    {
        sb.AppendLine("}");
        currentNamespace = null;
    }

    private string GenerateVisitorsSourceFor(string prefix, INamedTypeSymbol baseType, ImmutableArray<INamedTypeSymbol> leafTypes)
    {
        var sb = new StringBuilder();
        var visitorInterfaceName = $"I{prefix}Visitor";
        var baseNamespace = baseType.ContainingNamespace.ToDisplayString();
        OpenNamespace(sb, baseNamespace);
        GenerateVisitorInterfaces(sb, prefix, baseType, leafTypes);

        // abstract Accept methods
        sb.Append($@"
    partial {TypeToKindKeyword(baseType)} {baseType.Name}
    {{
        public abstract void Accept({visitorInterfaceName} visitor);
        public abstract TReturn Accept<TReturn>({visitorInterfaceName}<TReturn> visitor);
        public abstract TReturn Accept<TReturn, TParam>({visitorInterfaceName}<TReturn, TParam> visitor, TParam param);
    }}
");

        // Accept overrides
        foreach (var leafType in leafTypes)
        {
            var leafNamespace = leafType.ContainingNamespace.ToDisplayString();
            OpenNamespace(sb, leafNamespace);
            var methodModifier = "override"; // assume a base class defines the Accept methods to override
            if (leafType.BaseType is { SpecialType: SpecialType.System_Object })
            {
                // if it only inherits from System.Object, don't use `override`
                methodModifier = leafType.IsSealed ? "" : "virtual";
            }
            sb.Append($@"
    partial {TypeToKindKeyword(leafType)} {leafType.Name}
    {{
        public {methodModifier} void Accept({visitorInterfaceName} visitor) => visitor.Visit(this);
        public {methodModifier} TReturn Accept<TReturn>({visitorInterfaceName}<TReturn> visitor) => visitor.Visit(this);
        public {methodModifier} TReturn Accept<TReturn, TParam>({visitorInterfaceName}<TReturn, TParam> visitor, TParam param) => visitor.Visit(this, param);
    }}
");
        }

        CloseNamespace(sb);
        return sb.ToString();

        static string TypeToKindKeyword(ITypeSymbol type)
            => type.TypeKind switch
            {
                TypeKind.Class when type.IsRecord => "record",
                TypeKind.Class => "class",
                TypeKind.Interface => "interface",
                _ => throw new InvalidOperationException($"Unexpected type kind '{type.TypeKind}'"),
            };
    }

    private void GenerateVisitorInterfaces(StringBuilder sb, string prefix, INamedTypeSymbol baseType, ImmutableArray<INamedTypeSymbol> leafTypes)
    {
        var requiredNamespaces = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var leafType in leafTypes)
        {
            if (!SymbolEqualityComparer.Default.Equals(leafType.ContainingNamespace, baseType.ContainingNamespace))
            {
                requiredNamespaces.Add(leafType.ContainingNamespace.ToDisplayString());
            }
        }

        // usings
        foreach (var ns in requiredNamespaces)
        {
            sb.AppendLine($"    using {ns};");
        }

        var interfaceName = $"I{prefix}Visitor";
        var implName = $"{prefix}Visitor";
        // non-generic visitor
        sb.AppendLine($@"
    public interface {interfaceName}
    {{");
        foreach (var leafType in leafTypes)
        {
            sb.AppendLine($@"        void Visit({leafType.Name} node);");
        }
        sb.AppendLine("    }");

        sb.AppendLine($@"
    /// <summary>
    /// Default implementation of <see cref=""{interfaceName}""/> where all visit methods throw <see cref=""System.NotImplementedException""/>.
    /// </summary>
    public abstract class {implName} : {interfaceName}
    {{
        protected virtual void DefaultVisit({baseType.Name} node) => throw new System.NotImplementedException();
");
        foreach (var leafType in leafTypes)
        {
            sb.AppendLine($@"        public virtual void Visit({leafType.Name} node) => DefaultVisit(node);");
        }
        sb.AppendLine("    }");

        // visitor with generic return type
        sb.AppendLine($@"
    public interface {interfaceName}<TReturn>
    {{");
        foreach (var leafType in leafTypes)
        {
            sb.AppendLine($@"        TReturn Visit({leafType.Name} node);");
        }
        sb.AppendLine("    }");

        sb.AppendLine($@"
    /// <summary>
    /// Default implementation of <see cref=""{interfaceName}{{TReturn}}""/> where all visit methods throw <see cref=""System.NotImplementedException""/>.
    /// </summary>
    public abstract class {implName}<TReturn> : {interfaceName}<TReturn>
    {{
        protected virtual TReturn DefaultVisit({baseType.Name} node) => throw new System.NotImplementedException();
");
        foreach (var leafType in leafTypes)
        {
            sb.AppendLine($@"        public virtual TReturn Visit({leafType.Name} node) => DefaultVisit(node);");
        }
        sb.AppendLine("    }");

        // visitor with generic return and param types
        sb.AppendLine($@"
    public interface {interfaceName}<TReturn, TParam>
    {{");
        foreach (var leafType in leafTypes)
        {
            sb.AppendLine($@"        TReturn Visit({leafType.Name} node, TParam param);");
        }
        sb.AppendLine("    }");

        sb.AppendLine($@"
    /// <summary>
    /// Default implementation of <see cref=""{interfaceName}{{TReturn,TParam}}""/> where all visit methods throw <see cref=""System.NotImplementedException""/>.
    /// </summary>
    public abstract class {implName}<TReturn, TParam> : {interfaceName}<TReturn, TParam>
    {{
        protected virtual TReturn DefaultVisit({baseType.Name} node, TParam param) => throw new System.NotImplementedException();
");
        foreach (var leafType in leafTypes)
        {
            sb.AppendLine($@"        public virtual TReturn Visit({leafType.Name} node, TParam param) => DefaultVisit(node, param);");
        }
        sb.AppendLine("    }");
    }

    private const string AttributeName = "GenerateVisitorAttribute";
    private const string AttributeFullName = $"ScTools.SourceGenerators.{AttributeName}";
    private const string AttributeHintName = $"{AttributeName}.g.cs";
    private const string AttributeSource = $@"
namespace ScTools.SourceGenerators;

using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
internal sealed class {AttributeName} : Attribute
{{
    public string Prefix {{ get; }}

    public {AttributeName}(string prefix)
    {{
        Prefix = prefix;
    }}
}}
";

    private sealed class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<(string VisitorPrefix, INamedTypeSymbol BaseType, ImmutableArray<INamedTypeSymbol> LeafTypes)> Infos { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            // find all valid GenerateVisitor attributes
            if (context.Node is AttributeSyntax attrib
                && attrib.ArgumentList?.Arguments.Count is 1
                && context.SemanticModel.GetTypeInfo(attrib).Type?.ToDisplayString() == AttributeFullName)
            {
                var prefix = context.SemanticModel.GetConstantValue(attrib.ArgumentList.Arguments[0].Expression).ToString();
                var attrList = (AttributeListSyntax)attrib.Parent!;
                var typeDecl = (TypeDeclarationSyntax)attrList.Parent!;
                var typeSymbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(typeDecl)!;
                var hierarchyLeafTypes = GetHierarchyLeafTypes(typeSymbol.ContainingNamespace, typeSymbol).ToImmutableArray();

                Infos.Add((prefix, typeSymbol, hierarchyLeafTypes));
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetHierarchyLeafTypes(INamespaceOrTypeSymbol container, INamedTypeSymbol baseType)
        {
            var isInterface = baseType.TypeKind is TypeKind.Interface;
            foreach (var member in container.GetMembers())
            {
                if (member.Kind == SymbolKind.NamedType)
                {
                    var type = (INamedTypeSymbol)member;
                    if (type.IsSealed && (isInterface && Implements(type, baseType) || InheritsFrom(type, baseType)))
                    {
                        yield return type;
                    }
                }
                else if (member.Kind == SymbolKind.Namespace)
                {
                    foreach (var leaf in GetHierarchyLeafTypes((INamespaceSymbol)member, baseType))
                    {
                        yield return leaf;
                    }
                }
            }
        }

        private static bool InheritsFrom(ITypeSymbol type, ITypeSymbol baseType)
            => GetBaseTypes(type).Contains(baseType, SymbolEqualityComparer.Default);
        private static bool Implements(ITypeSymbol type, ITypeSymbol interfaceType)
            => type.AllInterfaces.Contains(interfaceType, SymbolEqualityComparer.Default);

        private static IEnumerable<ITypeSymbol> GetBaseTypes(ITypeSymbol type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
