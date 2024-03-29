﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BitDivisibleGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class BitDivisibleGenerator : IIncrementalGenerator
{
    const string Ns = "BitDivisible";
    const string Atr = "BitDivisibleAttribute";
    const string AtrDisp = Ns + "." + Atr;
    const string AtrField = "BitDivisibleFieldAttribute";
    const string AtrFieldDisp = Ns + "." + AtrField;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource($"{Ns}.g.cs", PostAtr()));

        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
            AtrDisp,
            static (node, token) => node is ClassDeclarationSyntax,
            static (context, token) => context
        );
        context.RegisterSourceOutput(source, Emit);
    }

    static void Emit(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        var typeSymbol = (INamedTypeSymbol)source.TargetSymbol;
        var typeNode = (TypeDeclarationSyntax)source.TargetNode;
        var className = typeSymbol.Name;

        var accessibility = typeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.Public => "public",
            _ => null,
        };
        if (string.IsNullOrEmpty(accessibility))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.E0001, typeNode.Identifier.GetLocation(), typeSymbol.Name));
            return;
        }

        if (!typeNode.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.E0002, typeNode.Identifier.GetLocation(), typeSymbol.Name));
            return;
        }

        var sb = new StringBuilder();
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IFieldSymbol field) continue;

            var isStatic = field.IsStatic;
            var memberType = field.Type.ToString();

            var argSb = new StringBuilder();
            var implSb = new StringBuilder();
            var lastBit = 0;
            foreach (var atr in member.GetAttributes())
            {
                if (atr?.AttributeClass?.ToDisplayString() == AtrFieldDisp &&
                    atr.ConstructorArguments.Length > 0)
                {
                    var type = atr.ConstructorArguments[0].Value!.ToString();
                    var bit = int.Parse(atr.ConstructorArguments[1].Value!.ToString());
                    var fname = atr.ConstructorArguments[2].Value!.ToString();
                    if (sb.Length > 0) sb.AppendLine();
                    if (type == "bool")
                    {
                        bit = 1;
                        if (isStatic)
                        {
                            sb.Append(lastBit > 0
                                ? $"    public static {type} {fname} => (({member.Name} >> {lastBit}) & 0b1) == 1;"
                                : $"    public static {type} {fname} => ({member.Name} & 0b1) == 1;"
                            );
                        }
                        else
                        {
                            sb.Append(lastBit > 0
                                ? $"    public {type} {fname} => (({member.Name} >> {lastBit}) & 0b1) == 1;"
                                : $"    public {type} {fname} => ({member.Name} & 0b1) == 1;"
                            );
                        }

                        if (implSb.Length > 0) implSb.Append(" |\n            ");
                        implSb.Append(lastBit > 0
                            ? $"({memberType})({ToFirstLower(fname)} ? 1 : 0) << {lastBit}"
                            : $"({memberType})({ToFirstLower(fname)} ? 1 : 0)"
                        );
                    }
                    else
                    {
                        if (isStatic)
                        {
                            sb.Append(lastBit > 0
                                ? $"    public static {type} {fname} => ({type})(({member.Name} >> {lastBit}) & 0b"
                                : $"    public static {type} {fname} => ({type})({member.Name} & 0b"
                            );
                        }
                        else
                        {
                            sb.Append(lastBit > 0
                                ? $"    public {type} {fname} => ({type})(({member.Name} >> {lastBit}) & 0b"
                                : $"    public {type} {fname} => ({type})({member.Name} & 0b"
                            );
                        }
                        for (int i = 0; i < bit; i++) sb.Append("1");
                        sb.Append(");");

                        if (implSb.Length > 0) implSb.Append(" |\n            ");
                        implSb.Append(lastBit > 0
                            ? $"({memberType}){ToFirstLower(fname)} << {lastBit}"
                            : $"({memberType}){ToFirstLower(fname)}"
                        );
                    }
                    lastBit += bit;

                    if (argSb.Length > 0) argSb.Append(", ");
                    argSb.Append($"{type} {ToFirstLower(fname)}");
                }
            }
            if (argSb.Length > 0)
            {
                if (isStatic)
                {
                    sb.Append($$"""

    public static void Set{{ToFirstUpper(member.Name)}}({{argSb}})
    {
        {{className}}.{{member.Name}} = {{implSb}};
    }
""");
                }
                else
                {
                    sb.Append($$"""

    public void Set{{ToFirstUpper(member.Name)}}({{argSb}})
    {
        this.{{member.Name}} = {{implSb}};
    }
""");
                }
            }
        }

        if (sb.Length <= 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.W0001, typeNode.Identifier.GetLocation(), typeSymbol.Name));
            return;
        }

        var isNamespace = !typeSymbol.ContainingNamespace.IsGlobalNamespace;

        context.AddSource($"{className}.{Ns}.g.cs", $$"""
// <auto-generated/>
{{(isNamespace ? $$"""namespace {{typeSymbol.ContainingNamespace}} {""" : "")}}
{{accessibility}} partial class {{className}}
{
{{sb}}
}
{{(isNamespace ? "}" : "")}}
""");
    }

    static string PostAtr()
    {
        return $$"""
// <auto-generated/>
using System;
namespace {{Ns}}
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class {{Atr}} : Attribute
    {
        public {{Atr}}() { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class {{AtrField}} : Attribute
    {
        public {{AtrField}}(Type type, int bit, string fieldName) { }
    }
}
""";
    }

    static string ToFirstLower(string s)
    {
        return char.ToLower(s[0]) + s.Substring(1);
    }
    static string ToFirstUpper(string s)
    {
        return char.ToUpper(s[0]) + s.Substring(1);
    }
}
