using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Stunts.CodeAnalysis;
using Stunts.Processors;

namespace Stunts
{
    /// <summary>
    /// Generates a <see cref="Document"/> in a project from a set of types and 
    /// <see cref="IDocumentProcessor"/> that modify the document to implement 
    /// the stunt.
    /// </summary>
    public class StuntDocumentGenerator
    {
        // Configured processors, by language, then phase.
        readonly Dictionary<string, Dictionary<ProcessorPhase, IDocumentProcessor[]>> processors;

        /// <summary>
        /// Instantiates the set of default <see cref="IDocumentProcessor"/> for the generator, 
        /// used for example when using the default constructor <see cref="StuntDocumentGenerator()"/>.
        /// </summary>
        public static IDocumentProcessor[] DefaultProcessors => new IDocumentProcessor[]
        {
            new DefaultImports(),
            new CSharpScaffold(),
            new CSharpRewrite(),
            new CSharpStunt(),
            new CSharpGenerated(),
            new VisualBasicScaffold(),
            new VisualBasicRewrite(),
            new VisualBasicStunt(),
            new VisualBasicParameterFixup(),
            new FixupImports(),
            new CSharpFileHeader(),
            new CSharpPragmas(),
            new VisualBasicFileHeader(),
            new VisualBasicGenerated(),
        };

        /// <summary>
        /// Default naming convention used when generating documents, unless overriden 
        /// via the corresponding constructor argument.
        /// </summary>
        public static NamingConvention DefaultNamingConvention { get; } = new NamingConvention();

        /// <summary>
        /// Default method attribute used to flag a generic method as stunt-generating,
        /// meaning invocations to that method are used to trigger source generation.
        /// </summary>
        public static Type DefaultGeneratorAttribute { get; } = typeof(StuntGeneratorAttribute);

        /// <summary>
        /// Initializes the generator with the default <see cref="Stunts.NamingConvention"/> 
        /// and the <see cref="GetDefaultProcessors"/> default processors.
        /// </summary>
        public StuntDocumentGenerator() : this(new NamingConvention(), DefaultProcessors) { }

        /// <summary>
        /// Initializes the generator with a custom <see cref="Stunts.NamingConvention"/> and 
        /// the <see cref="GetDefaultProcessors"/> default processors.
        /// </summary>
        public StuntDocumentGenerator(NamingConvention naming) : this(naming, DefaultProcessors) { }

        /// <summary>
        /// Initializes the generator with the default <see cref="Stunts.NamingConvention"/> 
        /// and the given set of <see cref="IDocumentProcessor"/>s.
        /// </summary>
        public StuntDocumentGenerator(params IDocumentProcessor[] processors) : this(new NamingConvention(), (IEnumerable<IDocumentProcessor>)processors) { }

        /// <summary>
        /// Initializes the generator with the default <see cref="Stunts.NamingConvention"/> 
        /// and the given set of <see cref="IDocumentProcessor"/>s.
        /// </summary>
        public StuntDocumentGenerator(IEnumerable<IDocumentProcessor> processors) : this(new NamingConvention(), processors) { }

        /// <summary>
        /// Initializes the generator with a custom <see cref="Stunts.NamingConvention"/> and 
        /// the given set of <see cref="IDocumentProcessor"/>s.
        /// </summary>
        public StuntDocumentGenerator(NamingConvention naming, IEnumerable<IDocumentProcessor> processors)
        {
            NamingConvention = naming;

            // Splits the processors by supported language and then by phase.
            this.processors = processors
                .SelectMany(processor => processor.Languages.Select(lang => new { Processor = processor, Language = lang }))
                .GroupBy(proclang => proclang.Language)
                .ToDictionary(
                    bylang => bylang.Key,
                    bylang => bylang
                        .GroupBy(proclang => proclang.Processor.Phase)
                        .ToDictionary(
                            byphase => byphase.Key, 
                            byphase => byphase.Select(proclang => proclang.Processor).ToArray()));
        }

        // The naming conventions to use for determining class and namespace names.
        public NamingConvention NamingConvention { get; }

        /// <summary>
        /// Attribute used to flag a generic method as stunt-generating,
        /// meaning invocations to that method are used to trigger source generation.
        /// </summary>
        public Type GeneratorAttribute { get; protected set; } = DefaultGeneratorAttribute;

        /// <summary>
        /// Generates a stunt document that implements the given types.
        /// </summary>
        /// <remarks>
        /// This aggregating method basically invokes <see cref="CreateStunt(IEnumerable{INamedTypeSymbol}, SyntaxGenerator)"/> 
        /// followed by <see cref="ApplyProcessors(Document, CancellationToken)"/>.
        /// </remarks>
        public async Task<Document> GenerateDocumentAsync(Project project, ITypeSymbol[] types, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var generator = SyntaxGenerator.GetGenerator(project);
            var (name, syntax) = CreateStunt(types.OfType<INamedTypeSymbol>(), generator);
            var code = syntax.NormalizeWhitespace().ToFullString();

            var filePath = Path.GetTempFileName();
            if (project.Language == LanguageNames.CSharp)
                filePath = Path.ChangeExtension(filePath, ".cs");
            else if (project.Language == LanguageNames.VisualBasic)
                filePath = Path.ChangeExtension(filePath, ".vb");

#if DEBUG
            // In debug builds, we persist the file so we can inspect the generated code
            // for troubleshooting.
            File.WriteAllText(filePath, code);
            Debug.WriteLine(filePath);
#endif

            Document document;

            // This special case supports tests better.
            if (project.Solution.Workspace is AdhocWorkspace workspace)
            {
                var directory = Path.Combine(Path.GetDirectoryName(project.FilePath) ?? "", 
                    Path.Combine(NamingConvention.Namespace.Split('.')));
                Directory.CreateDirectory(directory);

                document = workspace.AddDocument(DocumentInfo.Create(
                    DocumentId.CreateNewId(project.Id),
                    name,
                    folders: NamingConvention.Namespace.Split('.'),
                    filePath: filePath,
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(code), VersionStamp.Create()))));
            }
            else
            {
                document = project.AddDocument(
                    name,
                    SourceText.From(code),
                    folders: NamingConvention.Namespace.Split('.'),
                    filePath: filePath);
            }

            document = await ApplyProcessors(document, cancellationToken);

#if DEBUG
            // Update the persisted temp file in debug builds.
            File.WriteAllText(filePath, (await document.GetSyntaxRootAsync(cancellationToken))!
                .NormalizeWhitespace()
                .GetText()
                .ToString());
            if (Debugger.IsAttached)
                Process.Start(filePath);
#endif

            return document;
        }

        /// <summary>
        /// Generates the empty stunt code as a <see cref="SyntaxNode"/>, also returning 
        /// the resulting class name. It contains just the main compilation unit, namespace 
        /// imports, namespace declaration and class declaration with the given base type and 
        /// interfaces, with no class members at all.
        /// </summary>
        public (string name, SyntaxNode syntax) CreateStunt(IEnumerable<INamedTypeSymbol> symbols, SyntaxGenerator generator)
        {
            var name = NamingConvention.GetName(symbols);
            var imports = new HashSet<string>();
            var (baseType, implementedInterfaces) = symbols.ValidateGeneratorTypes();

            if (baseType != null)
                AddImports(imports, baseType);

            foreach (var iface in implementedInterfaces)
            {
                AddImports(imports, iface);
            }

            var syntax = generator.CompilationUnit(imports
                .Select(generator.NamespaceImportDeclaration)
                .Concat(new[]
                {
                    generator.NamespaceDeclaration(NamingConvention.Namespace,
                        generator.AddAttributes(
                            generator.ClassDeclaration(name,
                                modifiers: DeclarationModifiers.Partial,
                                baseType: baseType == null ? null : AsSyntaxNode(generator, baseType),
                                interfaceTypes: implementedInterfaces
                                    .Select(x => AsSyntaxNode(generator, x))
                            )
                        )
                    )
                }));

            return (name, syntax);
        }

        void AddImports(HashSet<string> imports, ITypeSymbol symbol)
        {
            if (symbol != null && symbol.ContainingNamespace != null && symbol.ContainingNamespace.CanBeReferencedByName)
                imports.Add(symbol.ContainingNamespace.ToDisplayString());

            if (symbol is INamedTypeSymbol named && named.IsGenericType)
            {
                foreach (var typeArgument in named.TypeArguments)
                {
                    AddImports(imports, typeArgument);
                }
            }
        }

        SyntaxNode AsSyntaxNode(SyntaxGenerator generator, ITypeSymbol symbol)
        {
            var prefix = symbol.ContainingType == null ? "" : symbol.ContainingType.Name + ".";
            if (symbol is INamedTypeSymbol named && named.IsGenericType)
                return generator.GenericName(prefix + symbol.Name, named.TypeArguments.Select(arg => AsSyntaxNode(generator, arg)));

            return generator.IdentifierName(prefix = symbol.Name);
        }

        /// <summary>
        /// Applies all received <see cref="IDocumentProcessor"/>s received in the generator constructor.
        /// </summary>
        public async Task<Document> ApplyProcessors(Document document, CancellationToken cancellationToken)
        {
#if DEBUG
            // While debugging the generation itself, don't let the cancellation timeouts
            // from tests cause this to fail.
            if (Debugger.IsAttached)
                cancellationToken = CancellationToken.None;
#endif

            var language = document.Project.Language;
            if (!processors.TryGetValue(language, out var supportedProcessors))
                return document;

            if (supportedProcessors.TryGetValue(ProcessorPhase.Prepare, out var prepares))
            {
                foreach (var prepare in prepares)
                {
                    document = await prepare.ProcessAsync(document, cancellationToken);
                }
            }

            if (supportedProcessors.TryGetValue(ProcessorPhase.Scaffold, out var scaffolds))
            {
                foreach (var scaffold in scaffolds)
                {
                    document = await scaffold.ProcessAsync(document, cancellationToken);
                }
            }

            if (supportedProcessors.TryGetValue(ProcessorPhase.Rewrite, out var rewriters))
            {
                foreach (var rewriter in rewriters)
                {
                    document = await rewriter.ProcessAsync(document, cancellationToken);
                }
            }

            if (supportedProcessors.TryGetValue(ProcessorPhase.Fixup, out var fixups))
            {
                foreach (var fixup in fixups)
                {
                    document = await fixup.ProcessAsync(document, cancellationToken);
                }
            }

            return document;
        }
    }
}
