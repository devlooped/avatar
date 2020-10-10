using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Stunts.CodeAnalysis;
using TypeNameFormatter;
using Xunit;
using Xunit.Sdk;
using static WorkspaceHelper;

namespace Stunts.UnitTests
{
    public class StuntSourceGeneratorTests
    {
        [InlineData(typeof(IDisposable))]
        [Theory]
        public void GenerateCode(params Type[] types)
        {
            var code = @"
using System;

namespace Stunts.UnitTests
{
    public class Test
    {
        public void Do()
        {
            var stunt = Stunt.Of<$$>();
            Console.WriteLine(stunt.ToString());
        }
    }
}".Replace("$$", string.Join(", ", types.Select(t =>
                     t.GetFormattedName(TypeNameFormatOptions.Namespaces))));

            var (diagnostics, compilation) = GetGeneratedOutput(code);

            Assert.Empty(diagnostics);

            var assembly = compilation.Emit();

            var stuntName = StuntNaming.GetFullName(types.First(), types.Skip(1).ToArray());
            var stuntType = assembly.GetType(stuntName);

            Assert.NotNull(stuntType);

            var stunt = Activator.CreateInstance(stuntType!);

            foreach (var type in types)
            {
                Assert.IsAssignableFrom(type, stunt);
            }
        }

        [InlineData(LanguageNames.CSharp, typeof(IDisposable))]
        [Theory]
        public async Task CanGenerateStuntForInterface(string language, params Type[] types)
        {
            var compilation = await Compile(language, types);

            // compilation.AddSyntaxTrees()

            var assembly = compilation.Emit();
            var type = assembly.GetType(StuntNaming.GetFullName(types[0], types[1..]), true);

            Assert.NotNull(type);
        }

        async Task<Compilation> Compile(string language, Type[] types, bool trace = false)
        {
            var (workspace, project) = CreateWorkspaceAndProject(language);
            var compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();

            var analyzer = new AnalyzerFileReference(typeof(StuntSourceGenerator).Assembly.Location, AssemblyLoader.Instance);
            var generators = analyzer.GetGenerators();

            var code = @"
using System;
using System.Collections.Generic;

namespace Stunts.UnitTests
{
    public class Test
    {
        public void Do()
        {
            var stunt = Stunt.Of<$$>();
            Console.WriteLine(stunt.ToString());

            var other = Create<$$>();
            Console.WriteLine(other);
        }

        [StuntGenerator]
        public static T Create<T>() => default;
    }
}

namespace Stunts
{
    public static class Stunt
    {
        [StuntGenerator]
        public static T Of<T>() => default;
        [StuntGenerator]
        public static T Of<T, T1>() => default;
    }
}".Replace("$$", string.Join(", ", types.Select(t =>
                       compilation.GetTypeByMetadataName(t.FullName!)?.ToFullName())));

            var syntax = CSharpSyntaxTree.ParseText(code);

            var temp = Path.GetTempFileName();
            File.WriteAllText(temp, code);

            project = project
                .AddDocument("test.cs", code, null, temp)
                .Project
                .AddAnalyzerReference(new TestAnalyzerReference(new StuntSourceGenerator()));

            compilation = await project.GetCompilationAsync(TimeoutToken(5)) ?? throw new XunitException();

            Assert.False(compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error),
                string.Join(Environment.NewLine, compilation.GetDiagnostics().Select(d => d.GetMessage())));

            return compilation;
        }

        class TestAnalyzerReference : AnalyzerReference
        {
            private readonly ISourceGenerator generator;

            public TestAnalyzerReference(ISourceGenerator generator) => this.generator = generator;

            public override string? FullPath => null;
            public override object Id => this;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => ImmutableArray<DiagnosticAnalyzer>.Empty;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => ImmutableArray<DiagnosticAnalyzer>.Empty;

            public override ImmutableArray<ISourceGenerator> GetGenerators() => ImmutableArray.Create(generator);
        }

        class AssemblyLoader : IAnalyzerAssemblyLoader
        {
            public static AssemblyLoader Instance = new AssemblyLoader();

            public void AddDependencyLocation(string fullPath) { }

            public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
        }

        static (ImmutableArray<Diagnostic>, Compilation) GetGeneratedOutput(string source, [CallerMemberName] string? test = null)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: test + ".cs");

            var references = new List<MetadataReference>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }

            var compilation = CSharpCompilation.Create(test,
                new SyntaxTree[]
                {
                    syntaxTree,
                    CSharpSyntaxTree.ParseText(File.ReadAllText(@"Stunts\Stunt.cs"), path: "Stunt.cs"),
                }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = compilation.GetDiagnostics();
            if (diagnostics.Any())
                return (diagnostics, compilation);

            ISourceGenerator generator = new StuntSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out diagnostics);

            return (diagnostics, output);
        }
    }
}
