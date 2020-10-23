using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Stunts.CodeAnalysis;

namespace Stunts
{
    /// <summary>
    /// Analyzes source code looking for method invocations to methods annotated with 
    /// the <see cref="StuntGeneratorAttribute"/> and reports unsupported nested types for 
    /// codegen.
    /// </summary>
    // TODO: this is a stop-gap measure until we can figure out why the ImplementInterfaces codefix
    // is not returning any code actions for the CSharpScaffold (maybe VB too?) when the type to 
    // be implemented is a nested interface :(. In the IDE, the code action is properly available after 
    // generating the (non-working, since the interface isn't implemented) mock class. 
    // One possible workaround to not prevent the scenario altogether would be to leverage the 
    // IImplementInterfaceService ourselves by duplicating the (simple) behavior of the CSharpImplementInterfaceCodeFixProvider 
    // (see http://source.roslyn.io/#Microsoft.CodeAnalysis.CSharp.Features/ImplementInterface/CSharpImplementInterfaceCodeFixProvider.cs,fd395d6b5f6a7dd3)
    // and wrapping the code action in our own that would run the rewriters right after invoking the built-in one. I fear, 
    // however, that the same reason the codefix isn't showing up when we ask for it, will cause that service to return 
    // an empty list of code actions too. 
    // It may be costly to investigate, and it doesn't seem like a core scenario anyway. 
    // It can be worked around by creating a custom mock.
    // TODO: F#
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class NestedTypeAnalyzer : DiagnosticAnalyzer
    {
        Type generatorAttribute;

        /// <summary>
        /// Instantiates the analyzer for method invocations annotated with <see cref="StuntGeneratorAttribute"/>.
        /// </summary>
        public NestedTypeAnalyzer() : this(typeof(StuntGeneratorAttribute)) { }

        /// <summary>
        /// Customizes the analyzer by specifying a custom 
        /// <see cref="generatorAttribute"/> to lookup in method invocations.
        /// </summary>
        protected NestedTypeAnalyzer(Type generatorAttribute) 
            => this.generatorAttribute = generatorAttribute;

        /// <summary>
        /// Returns the single descriptor this analyzer supports.
        /// </summary>
        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics 
            => ImmutableArray.Create(StuntDiagnostics.NestedType);

        /// <summary>
        /// Registers the analyzer to take action on method invocation expressions.
        /// </summary>
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        }

        void AnalyzeOperation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;

            // Get the matching symbol for the given generator attribute from the current compilation.
            var generator = context.Compilation.GetTypeByMetadataName(generatorAttribute.FullName);
            if (generator == null)
                return;

            if (invocation.TargetMethod.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, generator)))
            {
                var args = invocation.TargetMethod.TypeArguments.OfType<INamedTypeSymbol>().Where(t => t.ContainingType != null).ToArray();
                if (args.Length != 0)
                {
                    var diagnostic = Diagnostic.Create(
                        StuntDiagnostics.NestedType,
                        invocation.Syntax.GetLocation(),
                        string.Join(", ", args.Select(t => t.ContainingType.Name + "." + t.Name)));

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
