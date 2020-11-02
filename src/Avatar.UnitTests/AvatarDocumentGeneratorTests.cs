using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Simplification;
using Sample;
using Avatars.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static WorkspaceHelper;

namespace Avatars.UnitTests.GeneratorTests
{
    public class AvatarDocumentGeneratorTests
    {
        readonly ITestOutputHelper output;

        public AvatarDocumentGeneratorTests(ITestOutputHelper output) => this.output = output;

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public async Task CanGenerateAvatarForInterface(string language, bool trace = false)
        {
            var generator = new AvatarDocumentGenerator();
            var compilation = await CreateAvatar(generator, language, typeof(IFoo), trace);
            var assembly = compilation.Emit();
            var type = assembly.GetType(AvatarNaming.GetFullName(typeof(IFoo)), true);

            Assert.NotNull(type);

            var instance = Activator.CreateInstance(type!);

            Assert.NotNull(instance);
            Assert.IsAssignableFrom<IFoo>(instance!);
            Assert.IsAssignableFrom<IAvatar>(instance!);

            // If no behavior is configured, invoking it throws.
            Assert.Throws<NotImplementedException>(() => ((IFoo)instance!).Do());

            // When we add at least one matching behavior, invocations succeed.
            instance!.AddBehavior(new DefaultValueBehavior());
            ((IFoo)instance!).Do();

            // The IAvatar interface is properly implemented.
            Assert.Single(((IAvatar)instance!).Behaviors);
        }

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public async Task CanGenerateAvatarForClass(string language, bool trace = false)
        {
            var generator = new AvatarDocumentGenerator();
            var compilation = await CreateAvatar(generator, language, typeof(Foo), trace);
            var assembly = compilation.Emit();
            var type = assembly.GetType(AvatarNaming.GetFullName(typeof(Foo)), true)!;

            var instance = Activator.CreateInstance(type);

            Assert.NotNull(instance);
            Assert.IsAssignableFrom<Foo>(instance!);
            Assert.IsAssignableFrom<IAvatar>(instance!);

            // If no behavior is configured, invoking it throws.
            Assert.Throws<NotImplementedException>(() => ((Foo)instance!).Do());

            // When we add at least one matching behavior, invocations succeed.
            instance!.AddBehavior(new DefaultValueBehavior());
            ((Foo)instance!).Do();

            // The IAvatar interface is properly implemented.
            Assert.Single(((IAvatar)instance!).Behaviors);
        }

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public async Task GeneratedNameContainsAdditionalInterfaceInName(string language, bool trace = false)
        {
            var compilation = await CreateAvatar(new AvatarDocumentGenerator(), language, new[] { typeof(INotifyPropertyChanged), typeof(IDisposable) }, trace);
            var assembly = compilation.Emit();
            var type = assembly.GetType(AvatarNaming.GetFullName(typeof(INotifyPropertyChanged), typeof(IDisposable)), true);
            
            Assert.NotNull(type);
            Assert.True(typeof(IDisposable).IsAssignableFrom(type));
            Assert.True(type!.FullName!.Contains(nameof(IDisposable)),
                $"Generated avatar should contain the additional type {nameof(IDisposable)} in its name.");
        }

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public async Task GeneratedInterfaceHasCompilerGeneratedAttribute(string language, bool trace = false)
        {
            var compilation = await CreateAvatar(new AvatarDocumentGenerator(), language, typeof(ICalculator), trace);
            var assembly = compilation.Emit();
            var type = assembly.GetType(AvatarNaming.GetFullName(typeof(ICalculator)), true);

            Assert.NotNull(type);
            Assert.NotNull(type!.GetCustomAttribute<CompilerGeneratedAttribute>());
        }

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public async Task GeneratedTypeOverridesVirtualObjectMembers(string language, bool trace = false)
        {
            var compilation = await CreateAvatar(new AvatarDocumentGenerator(), language, new[] { typeof(INotifyPropertyChanged), typeof(IDisposable) }, trace);
            var assembly = compilation.Emit();
            var type = assembly.GetType(AvatarNaming.GetFullName(typeof(INotifyPropertyChanged), typeof(IDisposable)), true);

            Assert.NotNull(type);

            Assert.Contains(type!.GetMethods(), m =>
                m.Name == nameof(object.GetHashCode) ||
                m.Name == nameof(object.ToString) ||
                m.Name == nameof(object.Equals));
        }

        [InlineData(LanguageNames.CSharp, typeof(INotifyPropertyChanged))]
        [InlineData(LanguageNames.CSharp, typeof(ITypeGetter))]
        [InlineData(LanguageNames.CSharp, typeof(ICustomFormatter))]
        //[InlineData(LanguageNames.VisualBasic, typeof(INotifyPropertyChanged))]
        //[InlineData(LanguageNames.VisualBasic, typeof(ITypeGetter))]
        //[InlineData(LanguageNames.VisualBasic, typeof(ICustomFormatter))]
        [Theory]
        public Task SpecificTypes(string language, Type type)
            => CreateAvatar(new AvatarDocumentGenerator(), language, type);

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public Task WhenTypeHasGlobalNamespaceThenItWorks(string language)
            => CreateAvatar(new AvatarDocumentGenerator(), language, typeof(IGlobal));

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public Task WhenTypeIsInterface(string language, bool trace = false)
            => CreateAvatar(new AvatarDocumentGenerator(), language, typeof(ICalculator), trace);

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public Task WhenTypeIsAbstract(string language)
            => CreateAvatar(new AvatarDocumentGenerator(), language, typeof(CalculatorBase));

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public Task WhenTypeHasVirtualMembers(string language)
            => CreateAvatar(new AvatarDocumentGenerator(), language, typeof(Calculator));

        [InlineData(LanguageNames.CSharp)]
        //[InlineData(LanguageNames.VisualBasic)]
        [Theory]
        public async Task CanGenerateProxyWithMultipleInterfaces(string language)
        {
            var (workspace, project) = CreateWorkspaceAndProject(language);

            var compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();

            Assert.False(compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error),
                string.Join(Environment.NewLine, compilation.GetDiagnostics().Select(d => d.GetMessage())));

            var document = await new AvatarDocumentGenerator().GenerateDocumentAsync(project, new[]
            {
                compilation.GetTypeByMetadataName(typeof(INotifyPropertyChanging).FullName!) ?? throw new XunitException(),
                compilation.GetTypeByMetadataName(typeof(INotifyPropertyChanged).FullName!) ?? throw new XunitException(),
                compilation.GetTypeByMetadataName(typeof(ICalculator).FullName!) ?? throw new XunitException(),
            }, TimeoutToken(5));

            var syntax = await document.GetSyntaxRootAsync() ?? throw new XunitException();

            document = project.AddDocument("proxy." + (language == LanguageNames.CSharp ? "cs" : "vb"), syntax,
                filePath: Path.GetTempFileName());

            await AssertCode.NoErrorsAsync(document);
        }

        [Fact]
        public async Task WhenClassSymbolIsNotFirstThenThrows()
        {
            var (workspace, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();
            var types = new[]
            {
                compilation.GetTypeByMetadataName(typeof(ICalculator).FullName!) ?? throw new XunitException(),
                compilation.GetTypeByMetadataName(typeof(Calculator).FullName!) ?? throw new XunitException(),
            };

            await Assert.ThrowsAsync<ArgumentException>(() => new AvatarDocumentGenerator()
                .GenerateDocumentAsync(project, types, TimeoutToken(5)));
        }

        [Fact]
        public async Task WhenMultipleClassSymbolsThenThrows()
        {
            var (workspace, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();
            var types = new[]
            {
                compilation.GetTypeByMetadataName(typeof(object).FullName!) ?? throw new XunitException(),
                compilation.GetTypeByMetadataName(typeof(Calculator).FullName!) ?? throw new XunitException(),
            };

            await Assert.ThrowsAsync<ArgumentException>(() => new AvatarDocumentGenerator()
                .GenerateDocumentAsync(project, types, TimeoutToken(5)));
        }

        [Fact]
        public async Task WhenEnumSymbolIsSpecifiedThenThrows()
        {
            var (workspace, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();
            var types = new[]
            {
                compilation.GetTypeByMetadataName(typeof(PlatformID).FullName!) ?? throw new XunitException(),
            };

            await Assert.ThrowsAsync<ArgumentException>(() => new AvatarDocumentGenerator()
                .GenerateDocumentAsync(project, types, TimeoutToken(5)));
        }

        [Fact]
        public async Task WhenAdditionalGeneratorSpecifiedThenAddsAnnotation()
        {
            var (workspace, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();
            var types = new[]
            {
                compilation.GetTypeByMetadataName(typeof(IDisposable).FullName!) ?? throw new XunitException(),
            };

            var doc = await new TestGenerator().GenerateDocumentAsync(project, types, TimeoutToken(5));
            var syntax = await doc.GetSyntaxRootAsync() ?? throw new XunitException();
            var decl = syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var trivia = decl.GetLeadingTrivia();

            Assert.True(trivia.Any(SyntaxKind.SingleLineCommentTrivia));
        }

        [InlineData(LanguageNames.CSharp, @"public class Foo { }")]
//        [InlineData(LanguageNames.VisualBasic, @"Public Class Foo 
//End Class")]
        [Theory]
        public async Task CanCreateInstance(string language, string code, bool trace = false)
        {
            var compilation = await Compile(language, code, trace);
            var assembly = compilation.Emit();
            var type = assembly.GetExportedTypes().FirstOrDefault();

            Assert.NotNull(type);
            Assert.Equal("Foo", type!.FullName);

            var instance = Activator.CreateInstance(type!);

            Assert.NotNull(instance);
        }

        Task<Compilation> CreateAvatar(AvatarDocumentGenerator generator, string language, Type type, bool trace = false)
            => CreateAvatar(generator, language, new[] { type }, trace);

        async Task<Compilation> CreateAvatar(AvatarDocumentGenerator generator, string language, Type[] types, bool trace = false)
        {
            var (workspace, project) = CreateWorkspaceAndProject(language);
            project = project.AddAnalyzerReference(new AnalyzerFileReference(typeof(OverridableMembersAnalyzer).Assembly.Location, new AssemblyLoader()));

            var compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();

            Assert.False(compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error),
                string.Join(Environment.NewLine, compilation.GetDiagnostics().Select(d => d.GetMessage())));

            var symbols = types.Select(t => compilation.GetTypeByMetadataName(t.FullName!) 
                ?? throw new XunitException($"Could not get type {t.FullName} from compilation.")).ToArray();
            var document = await generator.GenerateDocumentAsync(project, symbols, TimeoutToken(5));

            var syntax = await document.GetSyntaxRootAsync() ?? throw new XunitException();
            document = project.AddDocument("code." + (language == LanguageNames.CSharp ? "cs" : "vb"), syntax, filePath: document.FilePath);

            await AssertCode.NoErrorsAsync(document);

            if (trace)
            {
                document = await Simplifier.ReduceAsync(document);
                var root = await document.GetSyntaxRootAsync() ?? throw new XunitException();
                output.WriteLine(root.NormalizeWhitespace().ToFullString());
            }

            return await document.Project.GetCompilationAsync() ?? throw new XunitException();
        }

        async Task<Compilation> Compile(string language, string code, bool trace = false)
        {
            var (workspace, project) = CreateWorkspaceAndProject(language);
            var compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();

            Assert.False(compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error),
                string.Join(Environment.NewLine, compilation.GetDiagnostics().Select(d => d.GetMessage())));

            var document = project.AddDocument("code." + (language == LanguageNames.CSharp ? "cs" : "vb"), code);

            await AssertCode.NoErrorsAsync(document);

            if (trace)
            {
                document = await Simplifier.ReduceAsync(document);
                var root = await document.GetSyntaxRootAsync() ?? throw new XunitException();
                output.WriteLine(root.NormalizeWhitespace().ToFullString());
            }

            return await document.Project.GetCompilationAsync() ?? throw new XunitException();
        }

        class AssemblyLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath) { }

            public Assembly LoadFromPath(string fullPath) => Assembly.Load(AssemblyName.GetAssemblyName(fullPath));
        }
    }

    public class TestGenerator : AvatarDocumentGenerator
    {
        public TestGenerator()
            : base(new NamingConvention(), DefaultProcessors.Concat(new[] { new TestProcessor() }).ToArray()) { }

        class TestProcessor : CSharpSyntaxRewriter, IDocumentProcessor
        {
            public string[] Languages => new[] { LanguageNames.CSharp };

            public ProcessorPhase Phase => ProcessorPhase.Scaffold;

            public async Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default)
            {
                var syntax = await document.GetSyntaxRootAsync(cancellationToken) ?? throw new XunitException();
                syntax = Visit(syntax);

                return document.WithSyntaxRoot(syntax);
            }

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
                => base.VisitClassDeclaration(node.WithLeadingTrivia(SyntaxFactory.Comment("Test")));
        }
    }

    public interface IFoo
    {
        void Do();
    }

    public abstract class Foo : IFoo
    {
        public abstract void Do();
    }

    public interface ITypeGetter
    {
        Type GetType(string assembly, string name);
    }
}

public interface IGlobal
{
    void Do();
}