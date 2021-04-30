using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MicroComGenerator.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace MicroComGenerator
{
    public partial class CSharpGen
    {

        CompilationUnitSyntax Unit()
            => CompilationUnit().WithUsings(List(new[]
                {
                    "System", "System.Text", "System.Collections", "System.Collections.Generic", "Avalonia.MicroCom"
                }
                .Concat(_extraUsings).Select(u => UsingDirective(IdentifierName(u)))));
        
        string Format(CompilationUnitSyntax unit)
        {
            var cw = new AdhocWorkspace();
            return
                "#pragma warning disable 108\n" +
                Microsoft.CodeAnalysis.Formatting.Formatter.Format(unit.NormalizeWhitespace(), cw, cw.Options
                    .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInObjectInit, true)
                    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers,
                        true)
                    .WithChangedOption(CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, true)
                    .WithChangedOption(CSharpFormattingOptions.NewLinesForBracesInMethods, true)

                ).ToFullString();
        }
        
        
        SyntaxToken Semicolon() => Token(SyntaxKind.SemicolonToken);

        static VariableDeclarationSyntax DeclareVar(string type, string name,
            ExpressionSyntax initializer = null)
            => VariableDeclaration(ParseTypeName(type),
                SingletonSeparatedList(VariableDeclarator(name)
                    .WithInitializer(initializer == null ? null : EqualsValueClause(initializer))));
        
        FieldDeclarationSyntax DeclareConstant(string type, string name, LiteralExpressionSyntax value)
            => FieldDeclaration(
                    VariableDeclaration(ParseTypeName(type),
                        SingletonSeparatedList(
                            VariableDeclarator(name).WithInitializer(EqualsValueClause(value))
                        ))
                ).WithSemicolonToken(Semicolon())
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword)));

        FieldDeclarationSyntax DeclareField(string type, string name, params SyntaxKind[] modifiers) =>
            DeclareField(type, name, null, modifiers);

        FieldDeclarationSyntax DeclareField(string type, string name, EqualsValueClauseSyntax initializer,
            params SyntaxKind[] modifiers) =>
            FieldDeclaration(
                    VariableDeclaration(ParseTypeName(type),
                        SingletonSeparatedList(
                            VariableDeclarator(name).WithInitializer(initializer))))
                .WithSemicolonToken(Semicolon())
                .WithModifiers(TokenList(modifiers.Select(x => Token(x))));

        bool IsPropertyRewriteCandidate(MethodDeclarationSyntax method)
        {

            return
                method.ReturnType.ToFullString() != "void"
                && method.Identifier.Text.StartsWith("Get")
                && method.ParameterList.Parameters.Count == 0;
        }

        TypeDeclarationSyntax RewriteMethodsToProperties<T>(T decl) where T : TypeDeclarationSyntax
        {
            var replace = new Dictionary<MethodDeclarationSyntax, PropertyDeclarationSyntax>();
            foreach (var method in decl.Members.OfType<MethodDeclarationSyntax>().ToList())
            {
                if (IsPropertyRewriteCandidate(method))
                {
                    var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration);
                    if (method.Body != null)
                        getter = getter.WithBody(method.Body);
                    else
                        getter = getter.WithSemicolonToken(Semicolon());

                    replace[method] = PropertyDeclaration(method.ReturnType,
                            method.Identifier.Text.Substring(3))
                        .WithModifiers(method.Modifiers).AddAccessorListAccessors(getter);

                }
            }

            return decl.ReplaceNodes(replace.Keys, (m, m2) => replace[m]);
        }

        bool IsInterface(string name)
        {
            if (name == "IUnknown")
                return true;
            return _idl.Interfaces.Any(i => i.Name == name);
        }

        private bool IsInterface(AstTypeNode type) => IsInterface(type.Name);
    
    }
}
