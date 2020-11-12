using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avatars;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Extensions methods for to <see cref="Document"/>.
    /// </summary>
    public static class DocumentExtensions
    {
        static readonly Lazy<ImmutableArray<DiagnosticAnalyzer>> builtInAnalyzers = new Lazy<ImmutableArray<DiagnosticAnalyzer>>(() =>
            MefHostServices
                .DefaultAssemblies
                .SelectMany(x => x.GetTypes()
                .Where(t => !t.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(t)))
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (DiagnosticAnalyzer)(Activator.CreateInstance(t)!))
                .Where(d => d != null)
                // Add our own.
                .Concat(new[] { new OverridableMembersAnalyzer() })
                .ToImmutableArray());

        static readonly Lazy<ImmutableArray<(CodeRefactoringProvider, ExportCodeRefactoringProviderAttribute)>> builtInRefactorings = new(() =>
           MefHostServices
               .DefaultAssemblies
               .SelectMany(x => x.GetTypes()
               .Where(t => !t.IsAbstract && typeof(CodeRefactoringProvider).IsAssignableFrom(t)))
               .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
               .Select(t => ((CodeRefactoringProvider)(Activator.CreateInstance(t)!), t.GetCustomAttribute<ExportCodeRefactoringProviderAttribute>()))
               .Where(x => x.Item1 != null && x.Item2 != null)
               .ToImmutableArray());

        /// <summary>
        /// Exposes the built-in analyzers, discovered via reflection.
        /// </summary>
        // TODO: see if this should be moved elsewhere.
        public static ImmutableArray<DiagnosticAnalyzer> BuiltInAnalyzers => builtInAnalyzers.Value;

        /// <summary>
        /// Applies the given named code fix to a document.
        /// </summary>
        public static async Task<Document> ApplyCodeFixAsync(this Document document, string codeFixName, ImmutableArray<DiagnosticAnalyzer> analyzers = default, CancellationToken cancellationToken = default)
        {
            // If we request and process ALL codefixes at once, we'll get one for each 
            // diagnostics, which is one per non-implemented member of the interface/abstract 
            // base class, so we'd be applying unnecessary fixes after the first one.
            // So we re-retrieve them after each Apply, which will leave only the remaining 
            // ones.
            var codeFixes = await GetCodeFixes(document, codeFixName, analyzers, cancellationToken);
            while (codeFixes.Length != 0)
            {
                var operations = await codeFixes[0].Action.GetOperationsAsync(cancellationToken);
                ApplyChangesOperation? operation;
                if ((operation = operations.OfType<ApplyChangesOperation>().FirstOrDefault()) != null)
                {
                    // According to https://github.com/DotNetAnalyzers/StyleCopAnalyzers/pull/935 and 
                    // https://github.com/dotnet/roslyn-sdk/issues/140, Sam Harwell mentioned that we should 
                    // be forcing a re-parse of the document syntax tree at this point. 
                    var existing = operation.ChangedSolution.GetDocument(document.Id);
                    if (existing != null)
                        document = await existing.RecreateDocumentAsync(cancellationToken);

                    // Retrieve the codefixes for the updated doc again.
                    codeFixes = await GetCodeFixes(document, codeFixName, analyzers, cancellationToken);
                }
                else
                {
                    // If we got no applicable code fixes, exit the loop and move on to the next codefix.
                    break;
                }
            }

            return document;
        }

        /// <summary>
        /// Applies the given named code fix to a document.
        /// </summary>
        public static async Task<Document> ApplyCodeActionAsync(this Document document, string providerName, ImmutableArray<DiagnosticAnalyzer> analyzers = default, CancellationToken cancellationToken = default)
        {
            // If we request and process ALL codefixes at once, we'll get one for each 
            // diagnostics, which is one per non-implemented member of the interface/abstract 
            // base class, so we'd be applying unnecessary fixes after the first one.
            // So we re-retrieve them after each Apply, which will leave only the remaining 
            // ones.
            var actions = await GetCodeActions(document, providerName, cancellationToken);
            while (actions.Length != 0)
            {
                var operations = await actions[0].Action.GetOperationsAsync(cancellationToken);
                ApplyChangesOperation? operation;
                if ((operation = operations.OfType<ApplyChangesOperation>().FirstOrDefault()) != null)
                {
                    // According to https://github.com/DotNetAnalyzers/StyleCopAnalyzers/pull/935 and 
                    // https://github.com/dotnet/roslyn-sdk/issues/140, Sam Harwell mentioned that we should 
                    // be forcing a re-parse of the document syntax tree at this point. 
                    var existing = operation.ChangedSolution.GetDocument(document.Id);
                    if (existing != null)
                        document = await existing.RecreateDocumentAsync(cancellationToken);

                    // Retrieve the codefixes for the updated doc again.
                    actions = await GetCodeActions(document, providerName, cancellationToken);
                }
                else
                {
                    // If we got no applicable code fixes, exit the loop and move on to the next codefix.
                    break;
                }
            }

            return document;
        }


        /// <summary>
        /// Forces recreation of the text of a document.
        /// </summary>
        public static async Task<Document> RecreateDocumentAsync(this Document document, CancellationToken cancellationToken)
        {
            var newText = await document.GetTextAsync(cancellationToken);
            newText = newText.WithChanges(new TextChange(new TextSpan(0, 0), " "));
            newText = newText.WithChanges(new TextChange(new TextSpan(0, 1), string.Empty));
            return document.WithText(newText);
        }

        static async Task<ImmutableArray<ICodeFix>> GetCodeFixes(
            Document document, string codeFixName,
            ImmutableArray<DiagnosticAnalyzer> analyzers = default, CancellationToken cancellationToken = default)
        {
            var provider = GetComponent<CodeFixProvider>(document, codeFixName);
            if (provider == null)
                return ImmutableArray<ICodeFix>.Empty;

            var compilation = await document.Project.GetCompilationAsync(cancellationToken);
            if (compilation == null)
                return ImmutableArray<ICodeFix>.Empty;

            // TODO: should we allow extending the set of built-in analyzers being added?
            if (analyzers.IsDefaultOrEmpty)
                analyzers = builtInAnalyzers.Value;

            var supportedAnalyers = analyzers
                .Where(a => a.SupportedDiagnostics.Any(d => provider.FixableDiagnosticIds.Contains(d.Id)))
                .ToImmutableArray();

            var allDiagnostics = default(ImmutableArray<Diagnostic>);

            // This may be a compiler warning/error, not an analyzer-produced one, such as 
            // the missing abstract method implementations.
            if (supportedAnalyers.IsEmpty)
            {
                allDiagnostics = compilation.GetDiagnostics(cancellationToken);
            }
            else
            {
                var analyerCompilation = compilation.WithAnalyzers(supportedAnalyers, cancellationToken: cancellationToken);
                allDiagnostics = await analyerCompilation.GetAllDiagnosticsAsync(cancellationToken);
            }

            var diagnostics = allDiagnostics
                .Where(x => provider.FixableDiagnosticIds.Contains(x.Id))
                // Only consider the diagnostics raised by the target document.
                .Where(d =>
                    d.Location.Kind == LocationKind.SourceFile &&
                    d.Location.GetLineSpan().Path == document.FilePath);

            var actions = new List<ICodeFix>();
            foreach (var diagnostic in diagnostics)
            {
                await provider.RegisterCodeFixesAsync(
                    new CodeFixContext(document, diagnostic,
                    (action, diag) => actions.Add(new CodeFixAdapter(action, diag, codeFixName)),
                    cancellationToken));
            }

            var final = new List<ICodeFix>();

            // All code actions without equivalence keys must be applied individually.
            final.AddRange(actions.Where(x => x.Action.EquivalenceKey == null));
            // All code actions with the same equivalence key should be applied only once.
            final.AddRange(actions
                .Where(x => x.Action.EquivalenceKey != null)
                .GroupBy(x => x.Action.EquivalenceKey)
                .Select(x => x.First()));

            return final.ToImmutableArray();
        }

        static async Task<ImmutableArray<ICodeFix>> GetCodeActions(
            Document document, string providerName, CancellationToken cancellationToken = default)
        {
            // Cannot use exports because there are missing imports that come from the editor/IDE 
            // and this fails, even if it's a Lazy<T, TMeta> :(
            // var provider = GetComponent<CodeRefactoringProvider>(document, providerName);
            var provider = builtInRefactorings.Value.Where(x => x.Item2.Name == providerName)
                .Select(x => x.Item1).FirstOrDefault();
            if (provider == null)
                return ImmutableArray<ICodeFix>.Empty;

            var compilation = await document.Project.GetCompilationAsync(cancellationToken);
            if (compilation == null)
                return ImmutableArray<ICodeFix>.Empty;

            var actions = new List<ICodeFix>();
            var root = await document.GetSyntaxRootAsync();
            if (root == null)
                return ImmutableArray<ICodeFix>.Empty;

            var type = root.DescendantNodes().Where(x => x.IsKind(SyntaxKind.ClassDeclaration)).FirstOrDefault() ??
                // See https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.visualbasic.syntaxkind?view=roslyn-dotnet
                root.DescendantNodes().Where(x => x.RawKind == 53).FirstOrDefault();

            if (type == null)
                return ImmutableArray<ICodeFix>.Empty;

            await provider.ComputeRefactoringsAsync(new CodeRefactoringContext(document, new TextSpan(((ClassDeclarationSyntax)type).Identifier.Span.Start, 0),
                action => actions.Add(new CodeFixAdapter(action, ImmutableArray<Diagnostic>.Empty, providerName)),
                cancellationToken));

            var final = new List<ICodeFix>();

            // All code actions without equivalence keys must be applied individually.
            final.AddRange(actions.Where(x => x.Action.EquivalenceKey == null));
            // All code actions with the same equivalence key should be applied only once.
            final.AddRange(actions
                .Where(x => x.Action.EquivalenceKey != null)
                .GroupBy(x => x.Action.EquivalenceKey)
                .Select(x => x.First()));

            // We reverse the list since usually the "Generate All" is the last entry, i.e. in the 
            // generate default ctors.
            final.Reverse();

            return final.ToImmutableArray();
        }

        // Debug view of all available providers and their metadata
        // document.Project.Solution.Workspace.Services.HostServices.GetExports<CodeFixProvider, IDictionary<string, object>>().OrderBy(x => x.Metadata["Name"]?.ToString()).Select(x => $"{x.Metadata["Name"]}: {string.Join(", ", (string[])x.Metadata["Languages"])}"  ).ToList()
        static T? GetComponent<T>(Document document, string name) => document.Project.Solution.Workspace.Services.HostServices
            .GetExports<T, IDictionary<string, object>>()
            .Where(x =>
                x.Metadata.ContainsKey("Languages") && x.Metadata.ContainsKey("Name") &&
                x.Metadata["Languages"] is string[] languages &&
                languages.Contains(document.Project.Language) &&
                x.Metadata["Name"] is string value && value == name)
            .Select(x => x.Value)
            .FirstOrDefault();
    }
}
