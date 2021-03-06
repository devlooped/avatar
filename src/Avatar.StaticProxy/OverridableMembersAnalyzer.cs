﻿using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Avatars
{
#pragma warning disable RS1022 // Remove accesses to Workspace API
    /// <summary>
    /// Analyzer that flags types that have overridable members as 
    /// exposed by <see cref="RoslynInternals.GetOverridableMembers"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class OverridableMembersAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostics identifier exposed by this analyzer, <c>AVTR999</c>.
        /// </summary>
        public const string DiagnosticId = "AVTR999";
        /// <summary>
        /// The category for the diagnostics reported by this analyzer, <c>Build</c>.
        /// </summary>
        public const string Category = "Build";

        static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            nameof(OverridableMembersAnalyzer),
            nameof(OverridableMembersAnalyzer),
            Category,
            DiagnosticSeverity.Hidden, isEnabledByDefault: true);

        /// <summary>
        /// Reports the only supported rule by this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <summary>
        /// Registers the analyzer for both C# and VB class declarations.
        /// </summary>
        /// <param name="context"></param>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var overridable = RoslynInternals.GetOverridableMembers((INamedTypeSymbol)context.Symbol, context.CancellationToken);

            if (context.Compilation.Language == LanguageNames.VisualBasic)
                overridable = overridable.Where(x => x.MetadataName != WellKnownMemberNames.DestructorName)
                    // VB doesn't support overriding events (yet). See https://github.com/dotnet/vblang/issues/63
                    .Where(x => x.Kind != SymbolKind.Event)
                    .ToImmutableArray();

            if (overridable.Length != 0)
            {
                var diagnostic = Diagnostic.Create(Rule, context.Symbol.Locations.FirstOrDefault());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
