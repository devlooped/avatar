using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars.Processors
{
    /// <summary>
    /// Adds the <see cref="IAvatar"/> interface implementation.
    /// </summary>
    public class CSharpAvatar : IDocumentProcessor
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
        /// Adds the <see cref="IAvatar"/> interface implementation to the document.
        /// </summary>
        public async Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default)
        {
            var syntax = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntax == null)
                return document;

            syntax = new CSharpAvatarVisitor(document).Visit(syntax);

            return document.WithSyntaxRoot(syntax);
        }

        class CSharpAvatarVisitor : CSharpSyntaxRewriter
        {
            readonly SyntaxGenerator generator;
            readonly Document document;

            public CSharpAvatarVisitor(Document document)
            {
                this.document = document;
                generator = SyntaxGenerator.GetGenerator(document);
            }

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;

                if (!generator.GetBaseAndInterfaceTypes(node).Any(x => 
                    x.ToString() == nameof(IAvatar) ||
                    x.ToString() == typeof(IAvatar).FullName))
                {
                    // Only add the base type if it isn't already there
                    node = node.AddBaseListTypes(SimpleBaseType(IdentifierName(nameof(IAvatar))));
                }

                if (!generator.GetMembers(node).Any(x => generator.GetName(x) == nameof(IAvatar.Behaviors)))
                {
                    node = (ClassDeclarationSyntax)generator.InsertMembers(node, 0,
                        PropertyDeclaration(
                            GenericName(
                                Identifier("IList"),
                                TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(nameof(IAvatarBehavior))))),
                            Identifier(nameof(IAvatar.Behaviors)))
                            .WithExplicitInterfaceSpecifier(ExplicitInterfaceSpecifier(IdentifierName(nameof(IAvatar))))
                            .WithExpressionBody(ArrowExpressionClause(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("pipeline"),
                                    IdentifierName("Behaviors"))))
                             .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                            .NormalizeWhitespace()
                            .WithTrailingTrivia(CarriageReturnLineFeed, CarriageReturnLineFeed)
                        );
                }

                if (!generator.GetMembers(node).Any(x => generator.GetName(x) == "pipeline"))
                {
                    node = (ClassDeclarationSyntax)generator.InsertMembers(node, 0,
                        FieldDeclaration(
                            VariableDeclaration(IdentifierName(Identifier(nameof(BehaviorPipeline))))
                            .WithVariables(
                                SingletonSeparatedList(
                                    VariableDeclarator(Identifier("pipeline"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            ObjectCreationExpression(IdentifierName(nameof(BehaviorPipeline)))
                                            .WithArgumentList(ArgumentList())))))
                            .NormalizeWhitespace()
                        ).WithModifiers(TokenList(Token(SyntaxKind.ReadOnlyKeyword))));
                }

                return node;
            }
        }
    }
}