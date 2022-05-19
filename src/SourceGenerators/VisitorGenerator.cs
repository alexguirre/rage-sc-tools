namespace ScTools.SourceGenerators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    private static string GenerateVisitorsSourceFor(string prefix, INamedTypeSymbol baseType, ImmutableArray<INamedTypeSymbol> LeafTypes)
    {
        var sb = new StringBuilder();
        var visitorInterfaceName = $"I{prefix}Visitor";
        sb.AppendLine($"namespace {baseType.ContainingNamespace.ToDisplayString()};");

        // non-generic visitor
        sb.AppendLine($"public interface {visitorInterfaceName} {{");
        foreach (var leafType in LeafTypes)
        {
            sb.AppendLine($@"    void Visit({leafType.Name} inst);");
        }
        sb.AppendLine("}");

        // visitor with generic return type
        sb.AppendLine($"public interface {visitorInterfaceName}<TReturn> {{");
        foreach (var leafType in LeafTypes)
        {
            sb.AppendLine($@"    TReturn Visit({leafType.Name} inst);");
        }
        sb.AppendLine("}");

        // visitor with generic return and param types
        sb.AppendLine($"public interface {visitorInterfaceName}<TReturn, TParam> {{");
        foreach (var leafType in LeafTypes)
        {
            sb.AppendLine($@"    TReturn Visit({leafType.Name} inst, TParam param);");
        }
        sb.AppendLine("}");

        // abstract Accept methods
        sb.Append($@"
partial {(baseType.IsRecord ? "record" : "class")} {baseType.Name}
{{
    public abstract void Accept({visitorInterfaceName} visitor);
    public abstract TReturn Accept<TReturn>({visitorInterfaceName}<TReturn> visitor);
    public abstract TReturn Accept<TReturn, TParam>({visitorInterfaceName}<TReturn, TParam> visitor, TParam param);
}}
");

        // Accept overrides
        foreach (var leafType in LeafTypes)
        {
            sb.Append($@"
partial {(leafType.IsRecord ? "record" : "class")} {leafType.Name}
{{
    public override void Accept({visitorInterfaceName} visitor) => visitor.Visit(this);
    public override TReturn Accept<TReturn>({visitorInterfaceName}<TReturn> visitor) => visitor.Visit(this);
    public override TReturn Accept<TReturn, TParam>({visitorInterfaceName}<TReturn, TParam> visitor, TParam param) => visitor.Visit(this, param);
}}
");
        }

        return sb.ToString();
    }

    private const string AttributeName = "GenerateVisitorAttribute";
    private const string AttributeFullName = $"ScTools.SourceGenerators.{AttributeName}";
    private const string AttributeHintName = $"{AttributeName}.g.cs";
    private const string AttributeSource = $@"
namespace ScTools.SourceGenerators;

using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
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
            foreach (var member in container.GetMembers())
            {
                if (member.Kind == SymbolKind.NamedType)
                {
                    var type = (INamedTypeSymbol)member;
                    if (type.IsSealed && InheritsFrom(type, baseType))
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
