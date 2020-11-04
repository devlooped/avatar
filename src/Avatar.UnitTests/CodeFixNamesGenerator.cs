using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars
{
    // Automatic generator for the CodeFixNames.g.cs
    class CodeFixNamesGenerator
    {
        // Re-run this method with TD.NET AdHoc runner to regenerate CodeFixNames.g.cs as needed.
        public void GenerateCodeFixNames()
        {
            var host = MefHostServices.Create(MefHostServices.DefaultAssemblies.Concat(new[] { typeof(CodeFixNamesGenerator).Assembly }));
            var codeFixProviders = host.GetExports<CodeFixProvider, IDictionary<string, object>>().Select(x => x.Metadata);
            var refactoringProviders = MefHostServices.DefaultAssemblies.Select(asm => asm.GetTypes())
                .SelectMany(types => types.Select(type => type.GetCustomAttribute<ExportCodeRefactoringProviderAttribute>()))
                .Where(attr => attr != null)
                .Select(attr => new Dictionary<string, object>
                {
                    { nameof(ExportCodeRefactoringProviderAttribute.Name), attr.Name },
                    { nameof(ExportCodeRefactoringProviderAttribute.Languages), attr.Languages },
                });

            var allFixes = new HashSet<string>();
            var codeFixes = new Dictionary<string, HashSet<string>>
            {
                { "All", allFixes }
            };

            var allRefactorings = new HashSet<string>();
            var codeRefactorings = new Dictionary<string, HashSet<string>>
            {
                { "All", allFixes }
            };

            GroupActions(codeFixProviders, allFixes, codeFixes);
            GroupActions(refactoringProviders, allRefactorings, codeRefactorings);

            var ns = NamespaceDeclaration(ParseName("Avatars.CodeActions"))
                .AddMembers(GetConstants("CodeFixes", codeFixes))
                .AddMembers(GetConstants("CodeRefactorings", codeRefactorings));

            var file = Path.GetFullPath(@$"{ThisAssembly.Project.MSBuildProjectDirectory}\..\Avatar.StaticProxy\CodeActions.g.cs");

            using (var output = new StreamWriter(file, false))
            {
                output.WriteLine("#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member");
                ns.NormalizeWhitespace().WriteTo(output);
                output.WriteLine();
                output.WriteLine("#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member");
            }

            Console.WriteLine(file);
        }

        static ClassDeclarationSyntax GetConstants(string className, Dictionary<string, HashSet<string>> codeFixes) => ClassDeclaration(className)
            .WithModifiers(TokenList(Token(SyntaxKind.PartialKeyword)))
            .WithMembers(List<MemberDeclarationSyntax>(codeFixes.Select(lang
                => ClassDeclaration(lang.Key.Replace(" ", "").Replace("#", "Sharp"))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.PartialKeyword)))
                .WithMembers(List<MemberDeclarationSyntax>(lang.Value.OrderBy(x => x).Select(fix
                    => FieldDeclaration(VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.StringKeyword)),
                        SeparatedList(new[] {
                VariableDeclarator(fix.Replace(" ", ""))
                .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(fix))))
                        })
                        ))
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword)))
                    )
                ))
                ))
            );

        static void GroupActions(IEnumerable<IDictionary<string, object>> providers, HashSet<string> allActions, Dictionary<string, HashSet<string>> codeActions)
        {
            foreach (var provider in providers.Where(x =>
                x.TryGetValue("Name", out var value) &&
                value is string name &&
                !string.IsNullOrEmpty(name) &&
                x.TryGetValue("Languages", out value) &&
                value is string[]))
            {
                foreach (var language in (string[])provider["Languages"])
                {
                    if (!codeActions.ContainsKey(language))
                        codeActions.Add(language, new HashSet<string>());

                    codeActions[language].Add((string)provider["Name"]);
                    allActions.Add((string)provider["Name"]);
                }
            }
        }
    }
}
