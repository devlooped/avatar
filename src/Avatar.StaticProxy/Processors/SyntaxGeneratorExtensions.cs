using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars.Processors
{
    /// <summary>
    /// Language agnostic helper methods for code generation.
    /// </summary>
    static class SyntaxGeneratorExtensions
    {
        /// <summary>
        /// Inspects a property to determine if supports read/write.
        /// </summary>
        public static (bool canRead, bool canWrite) InspectProperty(this SyntaxGenerator generator, SyntaxNode property) 
            => (generator.GetAccessor(property, DeclarationKind.GetAccessor) != null,
                generator.GetAccessor(property, DeclarationKind.SetAccessor) != null);

        /// <summary>
        /// Replaces a method's body by invoking the behavior pipeline.
        /// </summary>
        public static SyntaxNode ImplementMethod(this SyntaxGenerator generator, SyntaxNode method, SyntaxNode? returnType)
        {
            if (returnType != null)
            {
                return generator.WithStatements(method, new[]
                {
                    generator.ReturnStatement(generator.ExecutePipeline(returnType, generator.GetParameters(method)))
                });
            }

            return generator.WithStatements(method, new[]
            {
                generator.ExecutePipeline(returnType, generator.GetParameters(method))
            });
        }

        /// <summary>
        /// Replaces the implementation of a method with ref/out parameters by invoking the behavior pipeline.
        /// </summary>
        public static SyntaxNode ImplementMethod(this SyntaxGenerator generator,
            SyntaxNode method, SyntaxNode? returnType, SyntaxNode[] outParams, SyntaxNode[] refOutParams, bool callBase, SyntaxNode[] baseArgs, SyntaxNode[] allArgs)
        {
            var statements = outParams.Select(x => generator.AssignmentStatement(
                generator.IdentifierName(generator.GetName(x)),
                generator.DefaultExpression(generator.GetType(x))))
                .ToList();

            SyntaxNode? target = default;
            if (callBase)
            {
                statements.AddRange(refOutParams.Select(x =>
                    generator.LocalDeclarationStatement(
                        generator.GetType(x),
                        "local_" + generator.GetIdentifier(x),
                        generator.IdentifierName(generator.GetIdentifier(x)))));

                if (returnType == null)
                    target = generator.ValueReturningLambdaExpression(
                        new[] { generator.IdentifierName("m"), generator.IdentifierName("n") },
                        new[]
                        {
                            generator.InvocationExpression(
                                generator.MemberAccessExpression(
                                    generator.BaseExpression(),
                                    generator.GetName(method)),
                                baseArgs),
                            generator.InvocationExpression(
                                generator.MemberAccessExpression(
                                    generator.IdentifierName("m"),
                                    "CreateValueReturn"),
                                new[] { generator.NullLiteralExpression() }.Concat(allArgs))
                        });
                else
                    target = generator.ValueReturningLambdaExpression(
                        new[] { generator.ParameterDeclaration("m"), generator.ParameterDeclaration("n") },
                        generator.InvocationExpression(
                            generator.MemberAccessExpression(
                                generator.IdentifierName("m"),
                                "CreateValueReturn"),
                            new[] 
                            { 
                                generator.InvocationExpression(
                                    generator.MemberAccessExpression(
                                        generator.BaseExpression(),
                                        generator.GetName(method)),
                                    baseArgs)
                            }.Concat(allArgs)));
            }

            statements.Add(generator.LocalDeclarationStatement(
                generator.IdentifierName(nameof(IMethodReturn)),
                "__returns",
                generator.InvokePipeline(generator.GetParameters(method), target)));

            statements.AddRange(refOutParams.Select(x =>
                generator.AssignmentStatement(
                    generator.IdentifierName(generator.GetName(x)),
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.MemberAccessExpression(
                                generator.IdentifierName("__returns"),
                                nameof(IMethodReturn.Outputs)),
                            generator.GenericName("GetNullable", generator.GetType(x))),
                        generator.LiteralExpression(generator.GetName(x))
                    )
                )
            ));

            if (returnType != null)
            {
                if (returnType.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.RefType))
                {
                    statements.Add(generator.ReturnStatement(
                        RefExpression((ExpressionSyntax)
                            generator.MemberAccessExpression(
                                generator.InvocationExpression(
                                    generator.MemberAccessExpression(
                                        generator.IdentifierName("__returns"),
                                        generator.GenericName("AsRef", ((RefTypeSyntax)returnType).Type))),
                                    "Value"))));
                }
                else
                {
                    statements.Add(generator.ReturnStatement(
                        generator.CastExpression(
                            returnType,
                            generator.MemberAccessExpression(
                                generator.IdentifierName("__returns"),
                                nameof(IMethodReturn.ReturnValue)))));
                }
            }

            return generator.WithStatements(method, statements);
        }

        /// <summary>
        /// Creates the <c>pipeline.Execute</c> method invocation.
        /// </summary>
        public static SyntaxNode ExecutePipeline(this SyntaxGenerator generator, SyntaxNode? returnType, IEnumerable<SyntaxNode> parameters, SyntaxNode? target = null)
        {
            var execute = (returnType == null) ?
                generator.IdentifierName("Execute") :
                returnType.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.RefType) ?
                generator.GenericName("ExecuteRef", ((RefTypeSyntax)returnType).Type) :
                generator.GenericName("Execute", returnType);

            var create = CreateMethodInvocation(generator, parameters);

            if (target == null)
                return generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.IdentifierName("pipeline"),
                            execute),
                        create);

            return generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.IdentifierName("pipeline"),
                        execute),
                    create, target);
        }

        /// <summary>
        /// Creates the <c>pipeline.Invoke</c> method invocation.
        /// </summary>
        public static SyntaxNode InvokePipeline(this SyntaxGenerator generator, IEnumerable<SyntaxNode> parameters, SyntaxNode? target = null)
        {
            var create = CreateMethodInvocation(generator, parameters);

            if (target == null)
                return generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.IdentifierName("pipeline"),
                            generator.IdentifierName("Invoke")),
                        create, generator.TrueLiteralExpression());

            return generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.IdentifierName("pipeline"),
                        generator.IdentifierName("Invoke")),
                    create, target, generator.TrueLiteralExpression());
        }

        /// <summary>
        /// Creates the instance of the <see cref="MethodInvocation"/> passed to the behavior pipeline.
        /// </summary>
        public static SyntaxNode CreateMethodInvocation(this SyntaxGenerator generator, IEnumerable<SyntaxNode> parameters) =>
            generator.ObjectCreationExpression(
                generator.IdentifierName(nameof(MethodInvocation)),
                new[]
                {
                    generator.ThisExpression(),
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.IdentifierName(nameof(MethodBase)),
                            nameof(MethodBase.GetCurrentMethod))),
                }
                .Concat(parameters.Select(x => generator.Argument(generator.IdentifierName(generator.GetName(x))))));

        /// <summary>
        /// Creates the <c>pipeline.Execute</c> for event add/remove methods.
        /// </summary>
        public static SyntaxNode EventExecutePipeline(this SyntaxGenerator generator, IEnumerable<SyntaxNode> parameters, string eventName, bool eventAdd, bool isVirtual)
        {
            Func<SyntaxNode, SyntaxNode, SyntaxNode> operation = eventAdd ?
                generator.AddEventHandler : generator.RemoveEventHandler;

            if (isVirtual)
                return generator.InvocationExpression(
                    generator.MemberAccessExpression(
                        generator.IdentifierName("pipeline"),
                        generator.IdentifierName("Execute")),
                    generator.CreateMethodInvocation(parameters),
                    generator.ValueReturningLambdaExpression(
                        new[]
                        {
                            generator.ParameterDeclaration("m"),
                            generator.ParameterDeclaration("n")
                        },
                        new[]
                        {
                            operation(
                                generator.MemberAccessExpression(
                                    generator.BaseExpression(),
                                    eventName),
                                IdentifierName("value")),
                            generator.ReturnStatement(
                                generator.InvocationExpression(
                                    generator.MemberAccessExpression(
                                        generator.IdentifierName("m"),
                                        "CreateValueReturn"),
                                    generator.NullLiteralExpression(),
                                    generator.IdentifierName("value")))
                        }));

            return generator.ExecutePipeline(null, parameters);
        }
    }
}
