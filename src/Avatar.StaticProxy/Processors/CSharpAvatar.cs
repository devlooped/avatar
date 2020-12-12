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
    public class CSharpAvatar : IAvatarProcessor
    {
        /// <summary>
        /// Applies to <see cref="LanguageNames.CSharp"/>.
        /// </summary>
        public string Language { get; } = LanguageNames.CSharp;

        /// <summary>
        /// Runs in the final phase, <see cref="ProcessorPhase.Fixup"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Fixup;

        /// <summary>
        /// Adds the <see cref="IAvatar"/> interface implementation to the document.
        /// </summary>
        public SyntaxNode Process(SyntaxNode syntax, ProcessorContext context)
            => new CSharpAvatarVisitor().Visit(syntax)!;

        class CSharpAvatarVisitor : CSharpSyntaxRewriter
        {
            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;

                if (node.BaseList != null && !node.BaseList.Types.Any(x =>
                    x.ToString() == nameof(IAvatar) ||
                    x.ToString() == typeof(IAvatar).FullName))
                {
                    // Only add the base type if it isn't already there
                    node = node.AddBaseListTypes(SimpleBaseType(IdentifierName(nameof(IAvatar))));
                }

                if (!node.Members.OfType<PropertyDeclarationSyntax>().Any(prop => prop.Identifier.ToString() == nameof(IAvatar.Behaviors)))
                {
                    var behaviors = PropertyDeclaration(
                        GenericName(
                            Identifier("IList"),
                                TypeArgumentList(
                                    SingletonSeparatedList<TypeSyntax>(
                                        IdentifierName(nameof(IAvatarBehavior))))),
                            Identifier(nameof(IAvatar.Behaviors)))
                        .WithExplicitInterfaceSpecifier(
                            ExplicitInterfaceSpecifier(
                                IdentifierName(nameof(IAvatar))))
                        .WithExpressionBody(
                            ArrowExpressionClause(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("pipeline"),
                                    IdentifierName("Behaviors"))))
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                        .NormalizeWhitespace()
                        .WithTrailingTrivia(CarriageReturnLineFeed, CarriageReturnLineFeed);

                    if (node.Members.Count > 0)
                        node = node.InsertNodesAfter(node.Members.First(), new[] { behaviors });
                    else
                        node = node.AddMembers(behaviors);
                }

                if (!node.Members.OfType<FieldDeclarationSyntax>().Any(x => x.Declaration.Variables.Any(v => v.Identifier.ToString() == "pipeline")))
                {
                    node = node.InsertNodesBefore(node.Members.First(), new[]
                    {
                        FieldDeclaration(
                            VariableDeclaration(
                                IdentifierName(
                                    Identifier(nameof(BehaviorPipeline))))
                            .WithVariables(
                                SingletonSeparatedList(
                                    VariableDeclarator(Identifier("pipeline"))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        IdentifierName(nameof(BehaviorPipelineFactory)),
                                                        IdentifierName(nameof(BehaviorPipelineFactory.Default))),
                                                    GenericName(
                                                        Identifier(nameof(IBehaviorPipelineFactory.CreatePipeline)))
                                                    .WithTypeArgumentList(
                                                        TypeArgumentList(
                                                            SingletonSeparatedList<TypeSyntax>(IdentifierName(node.Identifier.ValueText))))))))))
                            .NormalizeWhitespace()
                        ).WithModifiers(TokenList(Token(SyntaxKind.ReadOnlyKeyword)))
                    });
                }

                return node;
            }
        }
    }
}