﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars.Processors
{
    /// <summary>
    /// Disables all nullable warnings since project may or may not 
    /// have nullable annotations enabled.
    /// </summary>
    public class CSharpPragmas : IDocumentProcessor
    {
        /// <summary>
        /// Applies to <see cref="LanguageNames.CSharp"/> only.
        /// </summary>
        public string[] Languages { get; } = new[] { LanguageNames.CSharp };

        /// <summary>
        /// Runs in the final phase of codegen, <see cref="ProcessorPhase.Fixup"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Fixup;

        /// <summary>
        /// Adds the <c>auto-generated</c> file header to the document.
        /// </summary>
        public async Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default)
        {
            var syntax = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntax == null)
                return document;

            return document.WithSyntaxRoot(syntax.WithLeadingTrivia(syntax.GetLeadingTrivia()
                .Add(Trivia(NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true)))
                .Add(Trivia(PragmaWarningDirectiveTrivia(
                    Token(SyntaxKind.DisableKeyword),
                    SeparatedList(new ExpressionSyntax[]
                    {
                        IdentifierName("CS8600"),
                        IdentifierName("CS8601"),
                        IdentifierName("CS8602"),
                        IdentifierName("CS8603"),
                        IdentifierName("CS8604"),
                        IdentifierName("CS8605"),
                        IdentifierName("CS8618"),
                        IdentifierName("CS8625"),
                        IdentifierName("CS8765"),
                    }), true)))
            ));
        }
    }
}
