using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Avatars.CodeAnalysis;
using Avatars.Processors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Avatars
{
    /// <summary>
    /// Main source generator for avatars. Can also be consumed from within other source 
    /// generators that wish to customize the process for their own flavors of avatars.
    /// </summary>
    [Generator]
    public record AvatarGenerator : ISourceGenerator
    {
        ImmutableArray<Func<ISyntaxReceiver>> receivers = ImmutableArray<Func<ISyntaxReceiver>>.Empty;

    // Configured processors, by language, then phase.
    Dictionary<string, Dictionary<ProcessorPhase, IAvatarProcessor[]>>? configuredProcessors = null;

    /// <summary>
    /// Default naming convention used when generating documents, unless overridden
    /// via the corresponding constructor argument.
    /// </summary>
    public static NamingConvention DefaultNamingConvention { get; } = new NamingConvention();

    /// <summary>
    /// Default method attribute used to flag a generic method as avatar-generating,
    /// meaning invocations to that method are used to trigger source generation.
    /// </summary>
    public static Type DefaultGeneratorAttribute { get; } = typeof(AvatarGeneratorAttribute);

    /// <summary>
    /// Instantiates the set of default <see cref="IAvatarProcessor"/> for the generator.
    /// </summary>
    public static IAvatarProcessor[] DefaultProcessors => new IAvatarProcessor[]
    {
            new DefaultImports(),
            new CSharpRewrite(),
            new CSharpAvatar(),
            new CSharpGenerated(),
            new FixupImports(),
            new CSharpFileHeader(),
            new CSharpPragmas(),
    };

    /// <summary>
    /// Creates a default avatar generator, using <see cref="DefaultNamingConvention"/>, 
    /// <see cref="DefaultGeneratorAttribute"/> and <see cref="DefaultProcessors"/>.
    /// </summary>
    public AvatarGenerator() : this(DefaultNamingConvention, DefaultGeneratorAttribute, DefaultProcessors) { }

    /// <summary>
    /// Creates a new instance of the <see cref="AvatarGenerator"/>.
    /// </summary>
    /// <param name="naming">The naming convention to apply to generated code.</param>
    /// <param name="generatorAttribute">The attribute used to flag generic methods that 
    /// should trigger avatar generation. The generic type parameters passed to invocations 
    /// of those methods are used when invoking <see cref="IAvatarProcessor.Process"/> in 
    /// the <see cref="ProcessorContext.TypeArguments"/>.
    /// </param>
    /// <param name="processors">Processors to use during source generation.</param>
    public AvatarGenerator(NamingConvention naming, Type generatorAttribute, params IAvatarProcessor[] processors)
        => (NamingConvention, GeneratorAttribute, Processors)
        = (naming, generatorAttribute, processors.ToImmutableArray());

    /// <summary>
    /// Creates a new instance of the <see cref="AvatarGenerator"/>.
    /// </summary>
    /// <param name="naming">The naming convention to apply to generated code.</param>
    /// <param name="generatorAttribute">The attribute used to flag generic methods that 
    /// should trigger avatar generation. The generic type parameters passed to invocations 
    /// of those methods are used when invoking <see cref="IAvatarProcessor.Process"/> in 
    /// the <see cref="ProcessorContext.TypeArguments"/>.
    /// </param>
    /// <param name="processors">Processors to use during source generation.</param>
    public AvatarGenerator(NamingConvention naming, Type generatorAttribute, IEnumerable<IAvatarProcessor> processors)
        => (NamingConvention, GeneratorAttribute, Processors)
        = (naming, generatorAttribute, processors.ToImmutableArray());

    /// <summary>
    /// Naming convention used for generated types.
    /// </summary>
    public NamingConvention NamingConvention { get; init; }

    /// <summary>
    /// The attribute used to flag generic methods that 
    /// should trigger avatar generation.
    /// </summary>
    public Type GeneratorAttribute { get; init; }

    /// <summary>
    /// Registered <see cref="IAvatarProcessor"/> that are applied when the generator 
    /// executes.
    /// </summary>
    public ImmutableArray<IAvatarProcessor> Processors { get; init; }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Execute(GeneratorExecutionContext context)
    {
        context.AnalyzerConfigOptions.CheckDebugger(nameof(AvatarGenerator));

        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.AvatarAnalyzerDir", out var analyerDir))
            DependencyResolver.AddSearchPath(analyerDir);

        var generatorAttr = context.Compilation.GetTypeByMetadataName(GeneratorAttribute.FullName);
        if (generatorAttr == null)
        {
            // TODO: context.ReportDiagnostic(...)
            return;
        }

        OnExecute(new ProcessorContext(context, NamingConvention, generatorAttr));
    }

    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context)
        => context.RegisterForSyntaxNotifications(()
            => new AggregateSyntaxReceiver(
                new ISyntaxReceiver[] { new AvatarGeneratorReceiver() }
                .Concat(receivers.Select(x => x())).ToArray()));

    /// <summary>
    /// Replaces the <see cref="GeneratorAttribute"/> in use.
    /// </summary>
    public AvatarGenerator WithGeneratorAttribute(Type generatorAttribute)
        => this with { GeneratorAttribute = generatorAttribute };

    /// <summary>
    /// Replaces the <see cref="NamingConvention"/> in use.
    /// </summary>
    public AvatarGenerator WithNamingConvention(NamingConvention naming)
        => this with { NamingConvention = naming };

    /// <summary>
    /// Registers an additional <see cref="IAvatarProcessor"/> to use during the generation phase.
    /// </summary>
    public AvatarGenerator WithProcessor(IAvatarProcessor processor)
        => this with { Processors = Processors.Add(processor) };

    /// <summary>
    /// Replaces all previously registered processors with the given <paramref name="processors"/>.
    /// </summary>
    public AvatarGenerator WithProcessors(params IAvatarProcessor[] processors)
        => this with { Processors = processors.ToImmutableArray() };

    /// <summary>
    /// Registers an additional syntax receiver factory that can collect syntax nodes for the generation 
    /// phase. 
    /// </summary>
    /// <remarks>
    /// The instance of the registered receiver can later be retrieved from an <see cref="IAvatarProcessor"/> 
    /// via the <see cref="ProcessorContext.SyntaxReceivers"/> like:
    /// <code>
    /// var receiver = context.SyntaxReceivers.OfType{MyReceiver}();
    /// </code>
    /// </remarks>
    public AvatarGenerator WithSyntaxReceiver(Func<ISyntaxReceiver> receiverFactory)
        => this with { receivers = receivers.Add(receiverFactory) };

    void OnExecute(ProcessorContext context)
    {
        // Once configured, the dictionary is immutable.
        configuredProcessors ??= Processors
            .GroupBy(processor => processor.Language)
            .ToDictionary(
                bylang => bylang.Key,
                bylang => bylang
                    .GroupBy(proclang => proclang.Phase)
                    .ToDictionary(
                        byphase => byphase.Key,
                        byphase => byphase.Select(proclang => proclang).ToArray()));

        var factory = AvatarSyntaxFactory.CreateFactory(context.Language);
        AvatarScaffold? defaultScaffold = null;
        var avatars = new HashSet<string>();

        foreach (var candidate in context.SyntaxReceivers
            .OfType<IAvatarCandidatesReceiver>()
            .SelectMany(receiver => receiver.GetCandidates(context)).ToArray())
        {
            var name = context.NamingConvention.GetName(candidate);
            if (avatars.Contains(name))
                continue;

            var syntax = factory.CreateSyntax(context.NamingConvention, candidate);
            if (!configuredProcessors.TryGetValue(context.Language, out var supportedProcessors))
                continue;

            if (supportedProcessors.TryGetValue(ProcessorPhase.Prepare, out var prepares))
                foreach (var processor in prepares)
                    syntax = processor.Process(syntax, context);

            if (supportedProcessors.TryGetValue(ProcessorPhase.Scaffold, out var scaffolds))
            {
                foreach (var processor in scaffolds)
                    syntax = processor.Process(syntax, context);
            }
            else
            {
                // Default scaffolding we provide is based on Roslyn code actions
                var document = (defaultScaffold ??= new AvatarScaffold(context)).ScaffoldAsync(name, syntax).Result;
                syntax = document.GetSyntaxRootAsync(context.CancellationToken).Result!;
                // NOTE: if any subsequent processor needs semantic model from the syntax tree, 
                // we'd need to update the compilation from the context to the one containing it,
                // retrieved from the document's project compilation.
                context = context with { Compilation = document.Project.GetCompilationAsync(context.CancellationToken).Result! };
            }

            if (supportedProcessors.TryGetValue(ProcessorPhase.Rewrite, out var rewriters))
                foreach (var processor in rewriters)
                    syntax = processor.Process(syntax, context);

            if (supportedProcessors.TryGetValue(ProcessorPhase.Fixup, out var fixups))
                foreach (var processor in fixups)
                    syntax = processor.Process(syntax, context);

            var code = syntax.NormalizeWhitespace().ToFullString();
            avatars.Add(name);
            context.AddSource(name, SourceText.From(code, Encoding.UTF8));

#if DEBUG
            if (Debugger.IsAttached)
            {
                if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.EmitCompilerGeneratedFiles", out var emitSources) &&
                    bool.TryParse(emitSources, out var shouldEmit) &&
                    shouldEmit &&
                    context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.IntermediateOutputPath", out var intermediateDir) &&
                    context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDir))
                {
                    var targetDir = Path.Combine(projectDir, intermediateDir, "generated", nameof(AvatarGenerator));
                    Directory.CreateDirectory(targetDir);

                    var filePath = Path.Combine(targetDir, name + (context.Language == LanguageNames.CSharp ? ".cs" : ".vb"));
                    File.WriteAllText(filePath, code);
                    Debugger.Log(0, "", "Avatar Generated: " + filePath);
                }

                Debugger.Log(0, "", string.Join(
                        Environment.NewLine,
                        code.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                            .Select((line, index) => index.ToString().PadLeft(3) + " " + line)) + Environment.NewLine);
            }
#endif
        }
    }

    static bool CanGenerateFor(INamedTypeSymbol? symbol)
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

    class AggregateSyntaxReceiver : ISyntaxReceiver, IEnumerable
    {
        readonly ISyntaxReceiver[] receivers;

        public AggregateSyntaxReceiver(ISyntaxReceiver[] receivers)
            => this.receivers = receivers;

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            foreach (var receiver in receivers)
                receiver.OnVisitSyntaxNode(syntaxNode);
        }

        IEnumerator IEnumerable.GetEnumerator() => receivers.GetEnumerator();
    }

    /// <summary>
    /// A <see cref="ISyntaxReceiver"/> that collects invocations to generic methods, 
    /// which are initial candidates for lookup.
    /// </summary>
    class AvatarGeneratorReceiver : IAvatarCandidatesReceiver
    {
        readonly List<(InvocationExpressionSyntax, GenericNameSyntax)> invocations = new();

        public IEnumerable<INamedTypeSymbol[]> GetCandidates(ProcessorContext context)
        {
            foreach (var (invocation, genericName) in invocations)
            {
                var semantic = context.Compilation.GetSemanticModel(invocation.SyntaxTree);
                var symbol = semantic.GetSymbolInfo(invocation, context.CancellationToken);
                if (symbol.Symbol is not IMethodSymbol method)
                    continue;

                if (!method.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, context.GeneratorAttribute)))
                    continue;

                var typeArgs = genericName.TypeArgumentList.Arguments
                    .Select(name => semantic.GetSymbolInfo(name, context.CancellationToken).Symbol as INamedTypeSymbol)
                    .ToList();

                // A corresponding diagnostics analyzer would flag this as a compile error.
                if (!typeArgs.All(CanGenerateFor))
                    continue;

                yield return typeArgs.Cast<INamedTypeSymbol>().ToArray();
            }
        }

        public void OnVisitSyntaxNode(SyntaxNode node)
        {
            // TODO: VB in the future?
            if (node.IsKind(SyntaxKind.InvocationExpression) &&
                node is InvocationExpressionSyntax invocation)
            {
                // Both Class.Method<T, ...>()
                if (invocation.Expression is MemberAccessExpressionSyntax member &&
                    member.Name is GenericNameSyntax memberName)
                    invocations.Add((invocation, memberName));
                // And Method<T, ...>()
                else if (invocation.Expression is GenericNameSyntax methodName)
                    invocations.Add((invocation, methodName));
            }
        }
    }
}
}
