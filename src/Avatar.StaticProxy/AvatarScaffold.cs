using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avatars.CodeActions;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Avatars.SyntaxFactoryGenerator;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars
{
    /// <summary>
    /// This class encapsulates the interactions with non publicly consumable 
    /// Roslyn features (code fixers and refactorings) that we use to scaffold 
    /// a class just like the IDE would if you auto-implemented all interface 
    /// members and overrode all base class members. This allows us to be 100% 
    /// consistent with what users would expect from manually coded proxies while 
    /// allowing us to avoid having to reimplement it all, since it depends on 
    /// quite a few internal helpers that aren't easy to extract from Roslyn'
    /// source.
    /// </summary>
    class AvatarScaffold : IDisposable
    {
        static string[] DefaultRefactorings { get; } =
        {
            //CodeRefactorings.CSharp.GenerateDefaultConstructorsCodeActionProvider,
            // NOTE: we cannot use GenerateOverridesCodeActionProvider because that 
            // depends on a IPickMembersService which allows picking the members in a UI
        };
        static string[] DefaultCodeFixes { get; } =
        {
            CodeFixes.CSharp.ImplementAbstractClass,
            CodeFixes.CSharp.ImplementInterface,
            "OverrideAllMembersCodeFix",
        };

        readonly ProcessorContext context;
        readonly AdhocWorkspace workspace;
        Project project;

        public AvatarScaffold(ProcessorContext context)
        {
            this.context = context;

            workspace = new AdhocWorkspace(WorkspaceServices.HostServices);
            var projectId = ProjectId.CreateNewId();
            var projectDir = Path.Combine(Path.GetTempPath(), nameof(AvatarGenerator), projectId.Id.ToString());
            var documents = context.Compilation.SyntaxTrees.Select(tree => DocumentInfo.Create(
                DocumentId.CreateNewId(projectId),
                Path.GetFileName(tree.FilePath),
                filePath: tree.FilePath,
                loader: TextLoader.From(TextAndVersion.Create(tree.GetText(), VersionStamp.Create()))))
                .ToArray();

            var projectInfo = ProjectInfo.Create(
                projectId, VersionStamp.Create(),
                nameof(AvatarGenerator),
                nameof(AvatarGenerator),
                context.Compilation.Language,
                filePath: Path.Combine(projectDir, "code.csproj"),
                compilationOptions: context.Compilation.Options,
                parseOptions: context.ParseOptions,
                metadataReferences: context.Compilation.References,
                documents: documents);

            project = workspace.AddProject(projectInfo);
        }

        public void Dispose() => workspace?.Dispose();

        public async Task<Document> ScaffoldAsync(SyntaxNode syntax, NamingConvention naming, string[]? codeFixes = null, string[]? refactorings = null)
        {
            var declaration = syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            Debug.Assert(declaration != null, "Received syntax does not have a class declaration.");

            var name = declaration!.Identifier.Text;
            var document = project.AddDocument(nameof(AvatarScaffold),
                // NOTE: if we don't force re-parsing in the context of our own 
                // project, nested types can't be resolved, for some reason :/
                syntax.NormalizeWhitespace().ToFullString(),
                folders: naming.RootNamespace.Split('.'),
                filePath: name);

            var root = await document.GetSyntaxRootAsync(context.CancellationToken)!;
            root = new CSharpConstructorsRewriter(await document.Project.GetCompilationAsync(context.CancellationToken)).Visit(root)!;
            document = document.WithSyntaxRoot(root);

            foreach (var refactoring in refactorings ?? DefaultRefactorings)
                document = await document.ApplyCodeActionAsync(refactoring, cancellationToken: context.CancellationToken);

            foreach (var codeFix in codeFixes ?? DefaultCodeFixes)
                document = await document.ApplyCodeFixAsync(codeFix, cancellationToken: context.CancellationToken);

            root = await document.GetSyntaxRootAsync(context.CancellationToken)!;
            root = new ScaffoldRewriter(await document.Project.GetCompilationAsync(context.CancellationToken)).Visit(root)!;
            document = document.WithSyntaxRoot(root);

            return document;
        }

        class CSharpConstructorsRewriter : CSharpSyntaxRewriter
        {
            readonly Compilation compilation;

            public CSharpConstructorsRewriter(Compilation? compilation) => this.compilation = compilation ?? throw new NotSupportedException();

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var semantic = compilation.GetSemanticModel(node.SyntaxTree, ignoreAccessibility: true);
                if (semantic == null)
                    return base.VisitClassDeclaration(node);

                var symbol = semantic.GetDeclaredSymbol(node);

                // TODO: warn/error?
                if (symbol == null)
                    return base.VisitClassDeclaration(node);

                var baseType = symbol.BaseType;
                if (baseType == null ||
                    baseType.TypeKind == TypeKind.Error)
                    return base.VisitClassDeclaration(node);

                var baseCtors = baseType.InstanceConstructors
                    .Where(c => c.IsAccessibleWithin(symbol))
                    .ToArray();

                foreach (var ctor in baseCtors)
                {
                    var parameters = ctor.Parameters
                        .Select(x => Parameter(x.Name).WithType(
                            IdentifierName(
                                    Identifier(
                                        TriviaList(),
                                        x.Type.ToFullName(),
                                        TriviaList(Space)))))
                        .ToImmutableArray();

                    node = node.AddMembers(
                        ConstructorDeclaration(node.Identifier)
                        .WithModifiers(TokenList(
                            Token(
                                TriviaList(Whitespace("        ")),
                                SyntaxKind.PublicKeyword,
                                TriviaList(Space))))
                        .WithParameterList(
                            ParameterList(
                                parameters.IsDefaultOrEmpty ? default :
                                SeparatedList(parameters))
                            .WithOpenParenToken(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.OpenParenToken,
                                    TriviaList()))
                            .WithCloseParenToken(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.CloseParenToken,
                                    TriviaList(Space))))
                        .WithInitializer(
                            parameters.IsDefaultOrEmpty ? default :
                            ConstructorInitializer
                            (
                                SyntaxKind.BaseConstructorInitializer,
                                ArgumentList(
                                    SeparatedList(
                                        ctor.Parameters.Select(x => Argument(IdentifierName(x.Name)))))
                                .WithCloseParenToken(
                                    Token(
                                        TriviaList(),
                                        SyntaxKind.CloseParenToken,
                                        TriviaList(Space)))
                            )
                            .WithColonToken(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.ColonToken,
                                    TriviaList(Space)))
                            .WithThisOrBaseKeyword(Token(SyntaxKind.BaseKeyword))
                        )
                        .WithBody(
                            Block()
                            .WithOpenBraceToken(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.OpenBraceToken,
                                    TriviaList(Space)))
                            .WithCloseBraceToken(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.CloseBraceToken,
                                    TriviaList(LineFeed))))
                    );
                }

                return base.VisitClassDeclaration(node)!;
            }
        }

        class ScaffoldRewriter : CSharpSyntaxRewriter
        {
            readonly Compilation compilation;

            public ScaffoldRewriter(Compilation? compilation) => this.compilation = compilation ?? throw new NotSupportedException();

            public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
                => node.Usings.Any(x => x.Name.ToString() == "System") ?
                    base.VisitCompilationUnit(node) :
                    base.VisitCompilationUnit(node.AddUsings(
                        UsingDirective(IdentifierName("System"))
                            .WithUsingKeyword(
                                Token(
                                    TriviaList(),
                                    SyntaxKind.UsingKeyword,
                                    TriviaList(Space)))
                            .WithSemicolon(CarriageReturnLineFeed)));

            public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) 
                // Add newline before namespace declaration
                => base.VisitNamespaceDeclaration(node
                    .WithNamespaceKeyword(Token(
                        TriviaList(CarriageReturnLineFeed),
                        SyntaxKind.NamespaceKeyword,
                        TriviaList(Space))));

            // Turn event field-like (override) declarations and turn them into full event
            // declarations with expression-bodied add/remove.
            public override SyntaxNode? VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
            {
                var declaration = EventDeclaration(node.AttributeLists, node.Modifiers, node.Declaration.Type,
                    null,
                    node.Declaration.Variables[0].Identifier,
                    AccessorList(List(new AccessorDeclarationSyntax[]
                    {
                        AccessorDeclaration(SyntaxKind.AddAccessorDeclaration)
                            .WithExpressionBody(ThrowNotImplementedArrow()
                                .WithArrowToken(
                                    Token(
                                        TriviaList(Space),
                                        SyntaxKind.EqualsGreaterThanToken,
                                        TriviaList())))
                            .WithSemicolon(Space),
                        AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration)
                            .WithExpressionBody(ThrowNotImplementedArrow()
                                .WithArrowToken(
                                    Token(
                                        TriviaList(Space),
                                        SyntaxKind.EqualsGreaterThanToken,
                                        TriviaList())))
                            .WithSemicolon()
                    }))
                    .WithOpenBraceToken(
                        Token(
                            TriviaList(Space),
                            SyntaxKind.OpenBraceToken,
                            TriviaList(Space)))
                    .WithCloseBraceToken(
                        Token(
                            TriviaList(Space),
                            SyntaxKind.CloseBraceToken,
                            TriviaList()))
                    )
                .WithEventKeyword(
                    Token(
                        TriviaList(),
                        SyntaxKind.EventKeyword,
                        TriviaList(Space)))
                .WithTrailingTrivia(CarriageReturnLineFeed);

                return declaration;
            }

            public override SyntaxNode? VisitEventDeclaration(EventDeclarationSyntax node)
            {
                node = node.WithAccessorList(AccessorList(List(new AccessorDeclarationSyntax[]
                {
                    AccessorDeclaration(SyntaxKind.AddAccessorDeclaration)
                        .WithExpressionBody(ThrowNotImplementedArrow())
                        .WithSemicolon(),
                    AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration)
                        .WithExpressionBody(ThrowNotImplementedArrow())
                        .WithSemicolon()
                })));

                return base.VisitEventDeclaration(node);
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Body?.Statements.Count != 1)
                    return base.VisitMethodDeclaration(node);

                // Turn single-line bodies into expression-bodied member
                var body = node.Body.Statements.Single();
                MethodDeclarationSyntax WithExpression(MethodDeclarationSyntax method, ExpressionSyntax expression)
                    => method
                        .WithParameterList(method.ParameterList.WithTrailingTrivia(Space))
                        .WithBody(null)
                        .WithExpressionBody(
                            ArrowExpressionClause(expression.WithLeadingTrivia(Space)))
                        .WithSemicolonToken(
                            Token(
                                TriviaList(),
                                SyntaxKind.SemicolonToken,
                                TriviaList(CarriageReturnLineFeed)));

                if (body.Kind() == SyntaxKind.ReturnStatement)
                    return base.VisitMethodDeclaration(WithExpression(node, ((ReturnStatementSyntax)body).Expression!));
                else if (body.Kind() == SyntaxKind.ExpressionStatement)
                    return base.VisitMethodDeclaration(WithExpression(node, ((ExpressionStatementSyntax)body).Expression));
                else if (body.Kind() == SyntaxKind.ThrowStatement)
                    return base.VisitMethodDeclaration(WithExpression(node, ThrowNotImplemented()));

                return base.VisitMethodDeclaration(node);
            }

            ArrowExpressionClauseSyntax ThrowNotImplementedArrow() => ArrowExpressionClause(ThrowNotImplemented());

            ThrowExpressionSyntax ThrowNotImplemented() => ThrowExpression(
                ObjectCreationExpression(
                    nameof(NotImplementedException))
                    .WithNewKeyword(Token(
                        TriviaList(),
                        SyntaxKind.NewKeyword,
                        TriviaList(Space))))
                .WithThrowKeyword(Token(
                    TriviaList(Space),
                    SyntaxKind.ThrowKeyword,
                    TriviaList(Space)));
        }
    }
}
