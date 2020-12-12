﻿using System;
using System.Collections.Generic;
using System.Linq;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars
{
    abstract class AvatarSyntaxFactory
    {
        public static AvatarSyntaxFactory CreateFactory(string language)
        {
            if (language != LanguageNames.CSharp)
                throw new NotSupportedException("The only supported language at the moment is " + LanguageNames.CSharp);

            return new CSharpAvatarSyntaxFactory();
        }

        public abstract SyntaxNode CreateSyntax(NamingConvention naming, INamedTypeSymbol[] symbols);

        class CSharpAvatarSyntaxFactory : AvatarSyntaxFactory
        {
            public override SyntaxNode CreateSyntax(NamingConvention naming, INamedTypeSymbol[] symbols)
            {
                var name = naming.GetName(symbols);
                var imports = new HashSet<string>();
                var (baseType, implementedInterfaces) = symbols.ValidateGeneratorTypes();

                if (baseType != null)
                    AddImports(imports, baseType);

                foreach (var iface in implementedInterfaces)
                    AddImports(imports, iface);

                return CompilationUnit()
                    .WithUsings(
                        List(
                            imports.Select(ns => UsingDirective(ParseName(ns)))))
                    .WithMembers(
                        SingletonList<MemberDeclarationSyntax>(
                            NamespaceDeclaration(ParseName(naming.GetNamespace(symbols)))
                            .WithMembers(
                                SingletonList<MemberDeclarationSyntax>(
                                    ClassDeclaration(name)
                                    .WithModifiers(TokenList(Token(SyntaxKind.PartialKeyword)))
                                    .WithBaseList(
                                        BaseList(
                                            SeparatedList<BaseTypeSyntax>(
                                                symbols.Select(AsTypeSyntax).Select(t => SimpleBaseType(t)))))))));
            }

            void AddImports(HashSet<string> imports, ITypeSymbol symbol)
            {
                if (symbol != null && symbol.ContainingNamespace != null && symbol.ContainingNamespace.CanBeReferencedByName)
                    imports.Add(symbol.ContainingNamespace.ToDisplayString());

                if (symbol is INamedTypeSymbol named && named.IsGenericType)
                {
                    foreach (var typeArgument in named.TypeArguments)
                        AddImports(imports, typeArgument);
                }
            }

            TypeSyntax AsTypeSyntax(ITypeSymbol symbol)
            {
                var prefix = symbol.ContainingType == null ? "" : symbol.ContainingType.Name + ".";
                if (symbol is INamedTypeSymbol named && named.IsGenericType)
                    return GenericName(Identifier(prefix + symbol.Name))
                        .WithTypeArgumentList(
                            TypeArgumentList(
                                SeparatedList(
                                    named.TypeArguments.Select(AsTypeSyntax))
                            )
                        );

                return IdentifierName(prefix + symbol.Name);
            }
        }
    }
}
