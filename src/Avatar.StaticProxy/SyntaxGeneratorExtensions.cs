using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Avatars
{
    static class SyntaxGeneratorExtensions
    {
        /// <summary>
        /// Gets the identifier or the syntax node, which can be an argument or 
        /// a parameter or a declaration.
        /// </summary>
        public static string GetIdentifier(this SyntaxGenerator generator, SyntaxNode syntax) =>
            syntax switch
            {
                Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax arg => generator.GetIdentifier(arg.Expression),
                Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax prm => prm.Identifier.ValueText,
                // TODO: VB
                _ => generator.GetName(syntax)
            };
    }
}
