using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Avatars.Processors
{
    /// <summary>
    /// Removes unnecessary imports and sorts the used ones.
    /// </summary>
    public class FixupImports : IDocumentProcessor
    {
        /// <summary>
        /// Applies to <see cref="LanguageNames.CSharp"/> only.
        /// </summary>
        public string[] Languages { get; } = new[] { LanguageNames.CSharp };

        /// <summary>
        /// Runs in the last phase of code gen, <see cref="ProcessorPhase.Fixup"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Fixup;

        /// <summary>
        /// Removes and sorts namespaces.
        /// </summary>
        public async Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default)
        {
            // This codefix is available for both C# and VB
            //document = await document.ApplyCodeFixAsync(CodeFixNames.All.RemoveUnnecessaryImports);

            var generator = SyntaxGenerator.GetGenerator(document);
            var syntax = await document.GetSyntaxRootAsync();
            var imports = generator.GetNamespaceImports(syntax).Select(generator.GetName).ToArray();

            Array.Sort(imports);

            syntax = new CSharpRewriteVisitor().Visit(syntax);

            return document.WithSyntaxRoot(generator.AddNamespaceImports(syntax,
                imports.Select(generator.NamespaceImportDeclaration)));
        }

        class CSharpRewriteVisitor : CSharpSyntaxRewriter
        {
            public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
                => base.VisitCompilationUnit(node.WithUsings(
                    SyntaxFactory.List<UsingDirectiveSyntax>()));
        }
    }
}
