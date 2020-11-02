using System;
using System.Collections.Generic;
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
    /// Rewrites all members so they are implemented through 
    /// the <see cref="BehaviorPipeline"/> field added to the 
    /// class by the <see cref="CSharpAvatar"/>.
    /// </summary>
    public class CSharpRewrite : IDocumentProcessor
    {
        /// <summary>
        /// Applies to <see cref="LanguageNames.CSharp"/> only.
        /// </summary>
        public string[] Languages { get; } = new[] { LanguageNames.CSharp };

        /// <summary>
        /// Runs in the third phase of codegen, <see cref="ProcessorPhase.Rewrite"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Rewrite;

        /// <summary>
        /// Rewrites all members in the document.
        /// </summary>
        public async Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default)
        {
            var syntax = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntax == null)
                return document;

            var semantic = await document.GetSemanticModelAsync(cancellationToken);
            var virtualEvents = new HashSet<string>();
            if (semantic != null)
            {
                var events = new EventVisitor();
                events.Visit(syntax);

                foreach (var symbol in events.Types.SelectMany(type => semantic
                    .LookupNamespacesAndTypes(type.Span.Start, name: type.Identifier.ValueText)
                    .OfType<INamedTypeSymbol>()).Where(x => x != null))
                {
                    var baseType = symbol.BaseType;
                    while (baseType != null)
                    {
                        foreach (var e in baseType.GetMembers().OfType<IEventSymbol>().Where(e => e.IsVirtual))
                            virtualEvents.Add(e.Name);

                        baseType = baseType.BaseType;
                    }
                }
            }

            syntax = new CSharpRewriteVisitor(document, virtualEvents).Visit(syntax);
            document = document.WithSyntaxRoot(syntax);

            return document;
        }

        class EventVisitor : CSharpSyntaxWalker
        {
            public List<ClassDeclarationSyntax> Types { get; } = new();
            public List<EventDeclarationSyntax> Events { get; } = new();

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                base.VisitClassDeclaration(node);
                Types.Add(node);
            }

            public override void VisitEventDeclaration(EventDeclarationSyntax node)
            {
                base.VisitEventDeclaration(node);
                if (node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                    Events.Add(node);
            }
        }

        class CSharpRewriteVisitor : CSharpSyntaxRewriter
        {
            readonly SyntaxGenerator generator;
            readonly HashSet<string> virtualEvents;

            public CSharpRewriteVisitor(Document document, HashSet<string> virtualEvents)
            {
                generator = SyntaxGenerator.GetGenerator(document);
                this.virtualEvents = virtualEvents;
            }

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                // Turn event fields into event declarations.
                var events = node.ChildNodes().OfType<EventFieldDeclarationSyntax>().ToArray();
                node = node.RemoveNodes(events, SyntaxRemoveOptions.KeepNoTrivia)!;

                node = node.AddMembers(events
                    .Select(x => EventDeclaration(x.Declaration.Type, x.Declaration.Variables.First().Identifier)
                        .WithModifiers(x.Modifiers))
                    .ToArray());

                return base.VisitClassDeclaration(node);
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (generator.GetAttributes(node).Any(attr => generator.GetName(attr) == "CompilerGenerated"))
                    return base.VisitMethodDeclaration(node);

                var outParams = node.ParameterList.Parameters.Where(x => x.Modifiers.Any(SyntaxKind.OutKeyword)).ToArray();
                var refOutParams = node.ParameterList.Parameters.Where(x => x.Modifiers.Any(SyntaxKind.RefKeyword) || x.Modifiers.Any(SyntaxKind.OutKeyword)).ToArray();
                var baseCall = GetBaseExpression(node.ExpressionBody?.Expression) ??
                    GetBaseExpression(node.Body?.Statements.OfType<ReturnStatementSyntax>().Select(x => x.Expression).FirstOrDefault()) ??
                    GetBaseExpression(node.Body?.Statements.OfType<ExpressionStatementSyntax>().Select(x => x.Expression).FirstOrDefault());

                if (outParams.Length != 0 || refOutParams.Length != 0)
                {
                    var localArgs = new HashSet<string>(refOutParams.Select(x => generator.GetIdentifier(x)));
                    ExpressionSyntax ToLocalIdentifier(ExpressionSyntax expression) =>
                        localArgs.Contains(generator.GetIdentifier(expression)) ?
                        IdentifierName("local_" + generator.GetIdentifier(expression)) :
                        expression;

                    // We need to rewrite all args to point to local_ and build two lists, once of arg only references, another with 
                    // the original ref/out annotations.
                    var baseArgs = baseCall == null ? Array.Empty<SyntaxNode>() :
                        ((InvocationExpressionSyntax)baseCall).ArgumentList.Arguments.Select(arg =>
                            arg.WithExpression(ToLocalIdentifier(arg.Expression))).Cast<SyntaxNode>().ToArray();

                    var allArgs = baseCall == null ? Array.Empty<SyntaxNode>() :
                        ((InvocationExpressionSyntax)baseCall).ArgumentList.Arguments.Select(arg => ToLocalIdentifier(arg.Expression)).Cast<SyntaxNode>().ToArray();

                    node = (MethodDeclarationSyntax)generator.ImplementMethod(node,
                        node.ReturnType.IsVoid() ? null : node.ReturnType,
                        outParams, refOutParams, baseCall != null, baseArgs, allArgs);
                }
                else
                {
                    if (node.Body != null)
                        node = node.RemoveNodes(new SyntaxNode[] { node.Body }, SyntaxRemoveOptions.KeepNoTrivia)!;

                    var body = ExecutePipeline(node.ReturnType, node.ParameterList.Parameters, baseCall);
                    if (node.ReturnType.IsKind(SyntaxKind.RefType))
                        body = RefExpression((ExpressionSyntax)generator.MemberAccessExpression(body, "Value"));

                    node = node.WithExpressionBody(ArrowExpressionClause(body))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                }

                return base.VisitMethodDeclaration(node);
            }

            public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (generator.GetAttributes(node).Any(attr => generator.GetName(attr) == "CompilerGenerated"))
                    return base.VisitPropertyDeclaration(node);

                (var canRead, var canWrite) = generator.InspectProperty(node);
                canRead = canRead || node.ExpressionBody != null;

                var prop = node;

                if (node.ExpressionBody != null)
                    node = node.RemoveNode(node.ExpressionBody, SyntaxRemoveOptions.KeepNoTrivia)!;

                node = node.WithAccessorList(null);

                if (canRead && !canWrite)
                {
                    node = node
                        .WithExpressionBody(ArrowExpressionClause(ExecutePipeline(
                            node.Type, Enumerable.Empty<ParameterSyntax>(), GetCallBase(prop, SyntaxKind.GetAccessorDeclaration))))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                }
                else
                {
                    if (canRead)
                    {
                        node = node.AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithExpressionBody(ArrowExpressionClause(ExecutePipeline(
                                node.Type, Enumerable.Empty<ParameterSyntax>(), GetCallBase(prop, SyntaxKind.GetAccessorDeclaration))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                    }
                    if (canWrite)
                    {
                        node = node.AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithExpressionBody(ArrowExpressionClause(
                                // NOTE: we always append the implicit "value" parameter for setters.
                                ExecutePipeline(null, new[] { Parameter(Identifier("value")).WithType(node.Type) },
                                    GetCallBase(prop, SyntaxKind.SetAccessorDeclaration))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                    }
                }

                return base.VisitPropertyDeclaration(node);
            }

            public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
            {
                if (generator.GetAttributes(node).Any(attr => generator.GetName(attr) == "CompilerGenerated"))
                    return base.VisitIndexerDeclaration(node);

                var trivia = node.GetTrailingTrivia();

                // NOTE: Most of this code could be shared with VisitPropertyDeclaration but the mutating With* 
                // and props like ExpressionBody aren't available in the shared base BasePropertyDeclarationSyntax type :(

                var canRead = (node.ExpressionBody != null || node.AccessorList?.Accessors.Any(x => x.IsKind(SyntaxKind.GetAccessorDeclaration)) == true);
                var canWrite = node.AccessorList?.Accessors.Any(x => x.IsKind(SyntaxKind.SetAccessorDeclaration)) == true;
                
                var prop = node;

                if (node.ExpressionBody != null)
                    node = node.RemoveNode(node.ExpressionBody, SyntaxRemoveOptions.KeepNoTrivia)!;

                node = node.WithAccessorList(null);

                if (canRead && !canWrite)
                {
                    node = node.WithExpressionBody(ArrowExpressionClause(ExecutePipeline(
                        node.Type, node.ParameterList.Parameters, GetCallBase(prop, SyntaxKind.GetAccessorDeclaration))))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                }
                else
                {
                    if (canRead)
                    {
                        node = node.AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithExpressionBody(ArrowExpressionClause(ExecutePipeline(
                                node.Type, node.ParameterList.Parameters, GetCallBase(prop, SyntaxKind.GetAccessorDeclaration))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                    }
                    if (canWrite)
                    {
                        node = node.AddAccessorListAccessors(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithExpressionBody(ArrowExpressionClause(
                                ExecutePipeline(null, node.ParameterList.Parameters.Concat(new[] { Parameter(Identifier("value")).WithType(node.Type) }),
                                GetCallBase(prop, SyntaxKind.SetAccessorDeclaration))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                    }
                }

                return base.VisitIndexerDeclaration(node.WithTrailingTrivia(trivia));
            }

            public override SyntaxNode? VisitEventDeclaration(EventDeclarationSyntax node)
            {
                if (generator.GetAttributes(node).Any(attr => generator.GetName(attr) == "CompilerGenerated"))
                    return base.VisitEventDeclaration(node);

                var value = Parameter(Identifier("value")).WithType(node.Type);
                var parameters = new SyntaxNode[] { value };
                
                if (virtualEvents.Contains(node.Identifier.ValueText))
                {
                    node = node.WithAccessorList(AccessorList(List(new AccessorDeclarationSyntax[]
                    {
                        AccessorDeclaration(SyntaxKind.AddAccessorDeclaration)
                            .WithExpressionBody(
                                ArrowExpressionClause((ExpressionSyntax)
                                    generator.EventExecutePipeline(parameters, generator.GetIdentifier(node), true, true)))
                            .WithSemicolon(),
                        AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration)
                            .WithExpressionBody(
                                ArrowExpressionClause((ExpressionSyntax)
                                    generator.EventExecutePipeline(parameters, generator.GetIdentifier(node), false, true)))
                            .WithSemicolon(),
                    })));
                }
                else
                {
                    node = node.WithAccessorList(AccessorList(List(new AccessorDeclarationSyntax[]
                    {
                        AccessorDeclaration(SyntaxKind.AddAccessorDeclaration)
                            .WithExpressionBody(
                                ArrowExpressionClause((ExpressionSyntax)generator.ExecutePipeline(null, parameters)))
                            .WithSemicolon(),
                        AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration)
                            .WithExpressionBody(
                                ArrowExpressionClause((ExpressionSyntax)generator.ExecutePipeline(null, parameters)))
                            .WithSemicolon()
                    })));
                }

                return base.VisitEventDeclaration(node);
            }

            ExpressionSyntax? GetCallBase(BasePropertyDeclarationSyntax node, SyntaxKind kind)
            {
                if (!node.Modifiers.Any(SyntaxKind.OverrideKeyword))
                    return null;

                ArrowExpressionClauseSyntax? expressionBody = node switch
                {
                    PropertyDeclarationSyntax prop => prop.ExpressionBody,
                    IndexerDeclarationSyntax index => index.ExpressionBody,
                    _ => throw new ArgumentException()
                };

                if (kind == SyntaxKind.GetAccessorDeclaration &&
                    expressionBody != null)
                    return GetBaseExpression(expressionBody.Expression);

                if (node.AccessorList == null)
                    return null;

                var accessor = node.AccessorList.Accessors.FirstOrDefault(d => d.IsKind(kind));
                if (accessor == null)
                    return null;

                return GetBaseExpression(accessor.ExpressionBody?.Expression) ??
                    (accessor.Body == null ? null :
                     GetBaseExpression(accessor.Body.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault()?.Expression));
            }

            ExpressionSyntax? GetBaseExpression(ExpressionSyntax? syntax) =>
                syntax == null ? null :
                syntax.DescendantNodes().OfType<BaseExpressionSyntax>().Any() ?
                syntax : null;

            ExpressionSyntax ExecutePipeline(TypeSyntax? returnType, IEnumerable<SyntaxNode> parameters, ExpressionSyntax? baseCall = null)
            {
                if (baseCall == null)
                    return (ExpressionSyntax)generator.ExecutePipeline(returnType.IsVoid() ? null : returnType, parameters, baseCall);

                if (returnType != null && !returnType.IsVoid())
                    return (ExpressionSyntax)generator.ExecutePipeline(returnType.IsVoid() ? null : returnType, parameters,
                        ParenthesizedLambdaExpression(
                            ParameterList(SeparatedList(new[] { Parameter(Identifier("m")), Parameter(Identifier("n")) })),
                            null,
                            (ExpressionSyntax)generator.InvocationExpression(
                                generator.MemberAccessExpression(
                                    generator.IdentifierName("m"), "CreateValueReturn"),
                                new[] { baseCall }.Concat(parameters
                                    .Select(x => generator.IdentifierName(generator.GetName(x))))))
                    );

                return (ExpressionSyntax)generator.ExecutePipeline(returnType.IsVoid() ? null : returnType, parameters,
                    ParenthesizedLambdaExpression(
                        ParameterList(SeparatedList(new[] { Parameter(Identifier("m")), Parameter(Identifier("n")) })),
                        Block(
                            ExpressionStatement(baseCall),
                            ReturnStatement((ExpressionSyntax)generator.InvocationExpression(
                                generator.MemberAccessExpression(
                                    generator.IdentifierName("m"), "CreateValueReturn"),
                                new[] { generator.NullLiteralExpression() }.Concat(parameters
                                        .Select(x => generator.IdentifierName(generator.GetName(x)))))
                            )
                        )));
            }
        }
    }
}