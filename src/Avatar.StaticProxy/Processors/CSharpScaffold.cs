using System.Threading;
using System.Threading.Tasks;
using Avatars.CodeActions;
using Microsoft.CodeAnalysis;

namespace Avatars.Processors
{
    /// <summary>
    /// Applies a set of code fixes that scaffold empty implementations 
    /// of all abstract class, interface and overridable members.
    /// </summary>
    public class CSharpScaffold : IDocumentProcessor
    {
        /// <summary>
        /// Default refactorings when no specific refactorings are provided. 
        /// </summary>
        public static string[] DefaultRefactorings { get; } =
        {
            CodeRefactorings.CSharp.GenerateDefaultConstructorsCodeActionProvider,
        };

        /// <summary>
        /// Default code fixes when no specific fixes are provided. 
        /// </summary>
        public static string[] DefaultCodeFixes { get; } =
        {
            CodeFixes.CSharp.ImplementAbstractClass,
            CodeFixes.CSharp.ImplementInterface,
            "OverrideAllMembersCodeFix",
        };

        readonly string[] codeFixes;
        readonly string[] codeRefactorings;

        /// <summary>
        /// Initializes the scaffold with the <see cref="DefaultCodeFixes"/> and 
        /// <see cref="DefaultRefactorings"/>.
        /// </summary>
        public CSharpScaffold() : this(DefaultCodeFixes, DefaultRefactorings) { }

        /// <summary>
        /// Initializes the scaffold with a specific set of code fixes to apply.
        /// </summary>
        public CSharpScaffold(string[]? codeFixes, string[]? codeRefactorings)
            => (this.codeFixes, this.codeRefactorings)
            = (codeFixes ?? DefaultCodeFixes, codeRefactorings ?? DefaultRefactorings);

        /// <summary>
        /// Applies to <see cref="LanguageNames.CSharp"/> only.
        /// </summary>
        public string[] Languages { get; } = new[] { LanguageNames.CSharp };

        /// <summary>
        /// Runs in the second phase of codegen, <see cref="ProcessorPhase.Scaffold"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Scaffold;

        /// <summary>
        /// Applies all existing code fixes to the document.
        /// </summary>
        public async Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default)
        {
            foreach (var refactoring in codeRefactorings)
                document = await document.ApplyCodeActionAsync(refactoring, cancellationToken: cancellationToken);

            foreach (var codeFix in codeFixes)
                document = await document.ApplyCodeFixAsync(codeFix, cancellationToken: cancellationToken);

            return document;
        }
    }
}
