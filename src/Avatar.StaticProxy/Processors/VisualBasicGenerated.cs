﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using static Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;

namespace Avatars.Processors
{
    /// <summary>
    /// Adds a <see cref="System.Runtime.CompilerServices.CompilerGeneratedAttribute"/> 
    /// attribute to all generated members, so that it's possible to distinguish user-authored 
    /// members in a partial class from the generated code.
    /// </summary>
    public class VisualBasicGenerated : IDocumentProcessor
    {
        /// <summary>
        /// Applies to the <see cref="LanguageNames.VisualBasic"/>.
        /// </summary>
        public string[] Languages { get; } = new[] { LanguageNames.VisualBasic };

        /// <summary>
        /// Runs in the final phase of codegen, <see cref="ProcessorPhase.Fixup"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Fixup;

        /// <summary>
        /// Applies the attribute to all members in the document.
        /// </summary>
        public async Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default)
        {
            var syntax = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntax == null)
                return document;
            
            syntax = new VisualBasicRewriteVisitor(SyntaxGenerator.GetGenerator(document)).Visit(syntax);

            return document.WithSyntaxRoot(syntax);
        }

        class VisualBasicRewriteVisitor : VisualBasicSyntaxRewriter
        {
            readonly SyntaxGenerator generator;

            public VisualBasicRewriteVisitor(SyntaxGenerator generator) => this.generator = generator;

            public override SyntaxNode VisitClassBlock(ClassBlockSyntax node)
            {
                if (generator.GetAttributes(node).Any(attr => generator.GetName(attr) == "CompilerGenerated"))
                    return base.VisitClassBlock(node);

                return base.VisitClassBlock((ClassBlockSyntax)AddAttributes(node));
            }

            SyntaxNode AddAttributes(SyntaxNode node)
                => generator.AddAttributes(node,
                    Attribute(IdentifierName("CompilerGenerated")));
        }
    }
}