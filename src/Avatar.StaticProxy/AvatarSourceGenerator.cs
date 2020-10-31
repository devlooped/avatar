using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Avatars
{
    /// <summary>
    /// Generates avatars by inspecting the current compilation for 
    /// invocations to methods annotated with [AvatarGenerator].
    /// </summary>
    [Generator]
    public class AvatarSourceGenerator : ISourceGenerator
    {
        static HashSet<string> resolveDirs = new();

        static AvatarSourceGenerator() => AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            if (resolveDirs.Count == 0)
                return null;

            var name = new AssemblyName(args.Name).Name;
            if (name == null)
                return null;

            foreach (var dir in resolveDirs)
            {
                var file = Path.GetFullPath(Path.Combine(dir, name + ".dll"));
                if (File.Exists(file))
                    return Assembly.LoadFrom(file);
            }

            return null;
        }

        /// <summary>
        /// Adds a directory to the <see cref="AppDomain.AssemblyResolve"/> probing paths when 
        /// loading the generator.
        /// </summary>
        /// <returns>Whether the directory was added or it was already registered.</returns>
        public static bool AddResolveDirectory(string resolveDirectory) => resolveDirs.Add(resolveDirectory);

        public virtual void Initialize(GeneratorInitializationContext context)
            => context.RegisterForSyntaxNotifications(() => new AggregateSyntaxReceiver(CreateSyntaxReceivers()));

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void Execute(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.CheckDebugger(nameof(AvatarSourceGenerator));

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AvatarAnalyzerDir", out var analyerDir))
                AddResolveDirectory(analyerDir);

            OnExecute(context, DocumentGenerator);
        }

        protected virtual AvatarDocumentGenerator DocumentGenerator => new AvatarDocumentGenerator();

        protected virtual IEnumerable<ISyntaxReceiver> CreateSyntaxReceivers() => new[] { new AvatarGeneratorReceiver() };

        void OnExecute(GeneratorExecutionContext context, AvatarDocumentGenerator generator)
        {
            if (context.SyntaxReceiver is not IEnumerable enumerable ||
                enumerable.OfType<AvatarGeneratorReceiver>().FirstOrDefault() is not AvatarGeneratorReceiver receiver)
                return;

            if (generator.GeneratorAttribute.FullName == null)
                return;

            var generatorAttr = context.Compilation.GetTypeByMetadataName(generator.GeneratorAttribute.FullName);
            if (generatorAttr == null)
                return;

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

            IEnumerable<INamedTypeSymbol[]> GetCandidates()
            {
                foreach (var invocation in receiver!.Invocations)
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

                    yield return typeArgs.Cast<INamedTypeSymbol>().ToArray();
                }
            };

            OnExecute(context, generator, GetCandidates());
        }

        protected virtual void OnExecute(GeneratorExecutionContext context, AvatarDocumentGenerator generator, IEnumerable<INamedTypeSymbol[]> candidates)
        {
            using var workspace = new AdhocWorkspace(WorkspaceServices.HostServices);

            var projectId = ProjectId.CreateNewId();
            var projectDir = Path.Combine(Path.GetTempPath(), nameof(AvatarSourceGenerator), projectId.Id.ToString());
            var documents = context.Compilation.SyntaxTrees.Select(tree => DocumentInfo.Create(
                DocumentId.CreateNewId(projectId),
                Path.GetFileName(tree.FilePath),
                filePath: tree.FilePath,
                loader: TextLoader.From(TextAndVersion.Create(tree.GetText(), VersionStamp.Create()))))
                .ToArray();

            var projectInfo = ProjectInfo.Create(
                projectId, VersionStamp.Create(),
                nameof(AvatarSourceGenerator),
                nameof(AvatarSourceGenerator),
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

            var generatedAvatars = new HashSet<string>();

            foreach (var args in candidates)
            {
                var name = generator.NamingConvention.GetName(args);
                if (generatedAvatars.Contains(name))
                    continue;

                var doc = generator.GenerateDocumentAsync(project, args.ToArray(), context.CancellationToken).Result;
                var root = doc.GetSyntaxRootAsync(context.CancellationToken).Result;
                if (root == null)
                    continue;

                var code = root.NormalizeWhitespace().ToFullString();
                if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.EmitAvatarSource", out var emitSource) &&
                    bool.TryParse(emitSource, out var shouldEmit) &&
                    shouldEmit)
                {
                    var filePath = Path.Combine(Path.GetTempPath(), name + ".cs");
                    File.WriteAllText(filePath, code);
                    context.ReportDiagnostic(Diagnostic.Create("ST424242", "Compiler", filePath,
                        DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 4,
                        location: Location.Create(filePath, TextSpan.FromBounds(0, 0), new LinePositionSpan())));
                }

                generatedAvatars.Add(name);
                context.AddSource(name, code);
            }
        }

        class AggregateSyntaxReceiver : ISyntaxReceiver, IEnumerable
        {
            public AggregateSyntaxReceiver(IEnumerable<ISyntaxReceiver> receivers) => SyntaxReceivers = receivers.ToArray();

            public ISyntaxReceiver[] SyntaxReceivers { get; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                foreach (var receiver in SyntaxReceivers)
                {
                    receiver.OnVisitSyntaxNode(syntaxNode);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => SyntaxReceivers.GetEnumerator();
        }

        class AvatarGeneratorReceiver : ISyntaxReceiver
        {
            public List<(InvocationExpressionSyntax Node, GenericNameSyntax Name)> Invocations { get; } = new();

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
