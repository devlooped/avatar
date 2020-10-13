using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Stunts.CodeAnalysis;

namespace Stunts
{
    /// <summary>
    /// Generates stunts by inspecting the current compilation for 
    /// invocations to methods annotated with [StuntGenerator].
    /// </summary>
    /// <devdoc>
    /// This generator is not annotated with [Generator] because we 
    /// need the wrapping <see cref="DependencyResolverGenerator"/> 
    /// to hook-up the assembly resolution so dependencies resolve 
    /// from the right tools folder.
    /// </devdoc>
    //[Generator]
    public class StuntSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
            => context.RegisterForSyntaxNotifications(() => new StuntGeneratorReceiver());

        public void Execute(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.CheckDebugger(nameof(StuntSourceGenerator));

            if (context.SyntaxReceiver is not StuntGeneratorReceiver receiver)
                return;

            var generatorAttr = context.Compilation.GetTypeByMetadataName("Stunts.StuntGeneratorAttribute");
            if (generatorAttr == null)
                return;

            using var workspace = new AdhocWorkspace(WorkspaceServices.HostServices);

            var projectId = ProjectId.CreateNewId();
            var projectDir = Path.Combine(Path.GetTempPath(), nameof(StuntSourceGenerator), projectId.Id.ToString());
            var documents = context.Compilation.SyntaxTrees.Select(tree => DocumentInfo.Create(
                DocumentId.CreateNewId(projectId),
                Path.GetFileName(tree.FilePath),
                filePath: tree.FilePath,
                loader: TextLoader.From(TextAndVersion.Create(tree.GetText(), VersionStamp.Create()))))
                .ToArray();
            
            var projectInfo = ProjectInfo.Create(
                projectId, VersionStamp.Create(),
                nameof(StuntSourceGenerator),
                nameof(StuntSourceGenerator),
                context.Compilation.Language,
                filePath: Path.Combine(projectDir, "code.csproj"),
                compilationOptions: context.Compilation.Options,
                parseOptions: context.ParseOptions,
                metadataReferences: context.Compilation.References,
                documents: documents);

            var project = workspace.AddProject(projectInfo);
            var compilation = project.GetCompilationAsync(context.CancellationToken).Result;
            if (compilation == null)
                // TODO: report, should never happen
                return;

            var generator = new StuntDocumentGenerator();
            var generatedStunts = new HashSet<string>();

            foreach (var invocation in receiver.Invocations)
            {
                var semantic = context.Compilation.GetSemanticModel(invocation.Node.SyntaxTree);
                var symbol = semantic.GetSymbolInfo(invocation.Node, context.CancellationToken);
                if (symbol.Symbol is not IMethodSymbol method)
                    continue;

                if (!method.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatorAttr)))
                    continue;

                var typeArgs = invocation.Name.TypeArgumentList.Arguments
                    .Select(name => semantic.GetSymbolInfo(name, context.CancellationToken).Symbol as INamedTypeSymbol)
                    .ToList();

                // A corresponding diagnostics analyzer would flag this as a compile error.
                if (!typeArgs.All(CanGenerateFor))
                    continue;

                // Check if the type hasn't been generated already
                if (generatedStunts.Contains(generator.NamingConvention.GetName(typeArgs.OfType<ITypeSymbol>())))
                    continue;

                // Convert from real compilation symbols to ad-hoc one for codegen.
                var finalArgs = typeArgs.Select(x => x!.ToFullName())
                    .Select(compilation.GetTypeByFullName)
                    .OfType<INamedTypeSymbol>()
                    .ToList();

                // Shouldn't happen.
                if (finalArgs.Any(x => x == null))
                    continue;

                var doc = generator.GenerateDocumentAsync(project, finalArgs.ToArray(), context.CancellationToken).Result;
                var root = doc.GetSyntaxRootAsync(context.CancellationToken).Result;
                if (root == null)
                    continue;

                var code = root.NormalizeWhitespace().ToFullString();
                var name = generator.NamingConvention.GetName(finalArgs);
                if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.EmitStuntSource", out var emitSource) &&
                    bool.TryParse(emitSource, out var shouldEmit) &&
                    shouldEmit)
                {
                    var filePath = Path.Combine(Path.GetTempPath(), name + ".cs");
                    File.WriteAllText(filePath, code);
                    context.ReportDiagnostic(Diagnostic.Create("ST424242", "Compiler", $"{filePath}", 
                        DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 4, 
                        location: Location.Create(filePath, TextSpan.FromBounds(0, 0), new LinePositionSpan())));
                }

                generatedStunts.Add(name);
                context.AddSource(name, code);
            }

            bool CanGenerateFor(INamedTypeSymbol? symbol)
            {
                if (symbol == null)
                    return false;

                // Cannot generate for types using pointer types
                var usesPointers = symbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .SelectMany(method => method.Parameters)
                    .Any(parameter => parameter.Type.Kind == SymbolKind.PointerType);

                return !usesPointers;
            }
        }

        class StuntGeneratorReceiver : ISyntaxReceiver
        {
            public List<(SyntaxNode Node, GenericNameSyntax Name)> Invocations { get; } = new();

            public void OnVisitSyntaxNode(SyntaxNode node)
            {
                // TODO: C# in the future?
                if (node.IsKind(SyntaxKind.InvocationExpression) && 
                    node is InvocationExpressionSyntax invocation)
                {
                    // Both Class.Method<T, ...>()
                    if (invocation.Expression is MemberAccessExpressionSyntax member &&
                        member.Name is GenericNameSyntax memberName)
                        Invocations.Add((invocation, memberName));
                    // And Method<T, ...>()
                    else if (invocation.Expression is GenericNameSyntax methodName)
                        Invocations.Add((invocation, methodName));
                }
            }
        }
    }
}
