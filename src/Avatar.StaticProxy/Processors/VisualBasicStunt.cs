using System.Linq;
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
    /// Adds the <see cref="IAvatar"/> interface implementation.
    /// </summary>
    public class VisualBasicAvatar : IDocumentProcessor
    {
        /// <summary>
        /// Applies to <see cref="LanguageNames.VisualBasic"/> only.
        /// </summary>
        public string[] Languages { get; } = new[] { LanguageNames.VisualBasic };

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

            syntax = new VisualBasicAvatarVisitor(SyntaxGenerator.GetGenerator(document)).Visit(syntax);

            return document.WithSyntaxRoot(syntax);
        }

        class VisualBasicAvatarVisitor : VisualBasicSyntaxRewriter
        {
            readonly SyntaxGenerator generator;

            public VisualBasicAvatarVisitor(SyntaxGenerator generator) => this.generator = generator;

            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                if (!node.Options.Any(opt => !opt.ChildTokens().Any(t => t.Kind() == SyntaxKind.StrictKeyword)))
                    node = node.AddOptions(OptionStatement(Token(SyntaxKind.StrictKeyword), Token(SyntaxKind.OnKeyword)));

                return base.VisitCompilationUnit(node);
            }

            public override SyntaxNode VisitClassBlock(ClassBlockSyntax node)
            {
                var result = base.VisitClassBlock(node);

                if (!generator.GetBaseAndInterfaceTypes(result).Any(x =>
                    x.ToString() == nameof(IAvatar) ||
                    x.ToString() == typeof(IAvatar).FullName))
                {
                    // Only add the base type if it isn't already there
                    result = generator.AddInterfaceType(
                        result,
                        generator.IdentifierName(nameof(IAvatar)));
                }

                if (!generator.GetMembers(result).Any(x => generator.GetName(x) == nameof(IAvatar.Behaviors)))
                {
                    var property = (PropertyBlockSyntax)generator.PropertyDeclaration(
                        nameof(IAvatar.Behaviors),
                        GenericName("IList", TypeArgumentList(IdentifierName(nameof(IAvatarBehavior)))),
                        modifiers: DeclarationModifiers.ReadOnly,
                        getAccessorStatements: new[]
                        {
                            generator.ReturnStatement(
                                generator.MemberAccessExpression(
                                    IdentifierName("pipeline"),
                                    nameof(BehaviorPipeline.Behaviors)))
                        });

                    property = property.WithPropertyStatement(
                        property.PropertyStatement.WithImplementsClause(
                            ImplementsClause(QualifiedName(IdentifierName(nameof(IAvatar)), IdentifierName(nameof(IAvatar.Behaviors))))));

                    result = generator.InsertMembers(result, 0, property);
                }

                if (!generator.GetMembers(result).Any(x => generator.GetName(x) == "pipeline"))
                {
                    var field = generator.FieldDeclaration(
                        "pipeline",
                        generator.IdentifierName(nameof(BehaviorPipeline)),
                        modifiers: DeclarationModifiers.ReadOnly,
                        initializer: generator.ObjectCreationExpression(generator.IdentifierName(nameof(BehaviorPipeline))));

                    result = generator.InsertMembers(result, 0, field);
                }

                return result;
            }
        }
    }
}