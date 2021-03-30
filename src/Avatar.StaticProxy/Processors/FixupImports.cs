using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars.Processors
{
    /// <summary>
    /// Sorts imports.
    /// </summary>
    public class FixupImports : ISyntaxProcessor
    {
        /// <summary>
        /// Applies to <see cref="LanguageNames.CSharp"/> only.
        /// </summary>
        public string Language { get; } = LanguageNames.CSharp;

        /// <summary>
        /// Runs in the last phase of code gen, <see cref="ProcessorPhase.Fixup"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Fixup;

        /// <summary>
        /// Removes and sorts namespaces.
        /// </summary>
        public SyntaxNode Process(SyntaxNode syntax, ProcessorContext context)
        {
            // This codefix is available for both C# and VB
            //document = await document.ApplyCodeFixAsync(CodeFixNames.All.RemoveUnnecessaryImports);

            // TODO: remove unused ones?

            if (syntax is not CompilationUnitSyntax unit)
                return syntax;

            return unit.WithUsings(
                List(
                    unit.Usings
                        .Distinct(UsingEqualityComparer.Default)
                        .OrderBy(x => x.Name.ToString())));
        }

        class UsingEqualityComparer : IEqualityComparer<UsingDirectiveSyntax>
        {
            public static IEqualityComparer<UsingDirectiveSyntax> Default { get; } = new UsingEqualityComparer();

            UsingEqualityComparer() { }

            public bool Equals(UsingDirectiveSyntax? x, UsingDirectiveSyntax? y) => x?.Name == y?.Name;

            public int GetHashCode(UsingDirectiveSyntax obj) => obj.Name.GetHashCode();
        }
    }
}
