﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars
{
    static class SyntaxExtensions
    {
        public static bool HasAttribute(this SyntaxList<AttributeListSyntax> attributes, string attribute)
            => attributes.Count > 0 &&
                attributes.Any(list => list.Attributes.Any(attr => attr.Name.ToString() == attribute));

        public static bool IsRefOut(this ArgumentSyntax argument)
            => argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword);

        public static bool IsOut(this ArgumentSyntax argument)
            => argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword);

        public static bool IsRefOut(this ParameterSyntax parameter)
            => parameter.Modifiers.Any(SyntaxKind.RefKeyword) || parameter.Modifiers.Any(SyntaxKind.OutKeyword);

        public static bool IsOut(this ParameterSyntax parameter)
            => parameter.Modifiers.Any(SyntaxKind.OutKeyword);

        public static InvocationExpressionSyntax WithArguments(this InvocationExpressionSyntax invocation, params ArgumentSyntax[] arguments)
            => WithArguments(invocation, (IEnumerable<ArgumentSyntax>)arguments);

        public static InvocationExpressionSyntax WithArguments(this InvocationExpressionSyntax invocation, IEnumerable<ArgumentSyntax> arguments)
            => invocation.WithArgumentList(ArgumentList(SeparatedList(arguments)));

        public static ElementAccessExpressionSyntax WithArgumentList(this ElementAccessExpressionSyntax indexer, params ArgumentSyntax[] arguments)
            => WithArgumentList(indexer, (IEnumerable<ArgumentSyntax>)arguments);

        public static ElementAccessExpressionSyntax WithArgumentList(this ElementAccessExpressionSyntax indexer, IEnumerable<ArgumentSyntax> arguments)
            => indexer.WithArgumentList(BracketedArgumentList(SeparatedList(arguments)));
    }
}
