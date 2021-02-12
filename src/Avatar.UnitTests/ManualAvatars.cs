using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sample;

namespace Avatars.UnitTests
{
    class ManualAvatars
    {
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1822 // Intended to be run by the ad-hoc TD.NET runner
        public void UpdateAvatars()
        {
            if (!Debugger.IsAttached)
                throw new InvalidOperationException("This is intended to be run with the debugger attached.");

            var code = @"
using System;
using Avatars;
using Sample;

namespace UnitTests
{
    public class Test
    {
        public void Do()
        {
             _ = Avatar.Of<ICalculator>();
             _ = Avatar.Of<ICalculator, IDisposable>();
             _ = Avatar.Of<Calculator>();
             _ = Avatar.Of<CalculatorBase>();
             _ = Avatar.Of<ICalculatorMemory>();
             _ = Avatar.Of<CalculatorMemory>();
             _ = Avatar.Of<CalculatorMemoryBase>();
        }
    }
}";

            var libs = new HashSet<string>(File.ReadAllLines("lib.txt"), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => Path.GetFileName(x));

            var args = CSharpCommandLineParser.Default.Parse(
                File.ReadAllLines("csc.txt"), ThisAssembly.Project.MSBuildProjectDirectory, sdkDirectory: null);

            var syntaxTree = CSharpSyntaxTree.ParseText(
                code,
                options: args.ParseOptions.WithLanguageVersion(LanguageVersion.Latest),
                path: Path.GetTempFileName(),
                encoding: Encoding.UTF8);

            var sources = new List<SyntaxTree>
            {
                syntaxTree
            };

            var additionalSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Avatar.cs",
                "Avatar.StaticFactory.cs"
            };

            foreach (var source in args.SourceFiles.Where(x => additionalSources.Contains(Path.GetFileName(x.Path))))
            {
                var filePath = source.Path;
                var fileName = filePath.StartsWith(ThisAssembly.Project.MSBuildProjectDirectory) ?
                    filePath.Substring(ThisAssembly.Project.MSBuildProjectDirectory.Length).TrimStart(Path.DirectorySeparatorChar) :
                    filePath;

                sources.Add(CSharpSyntaxTree.ParseText(
                    File.ReadAllText(filePath),
                    options: args.ParseOptions.WithLanguageVersion(LanguageVersion.Latest),
                    path: filePath,
                    encoding: Encoding.UTF8));
            }

            foreach (var thisAssemblyFile in Directory.EnumerateFiles(
                Path.Combine(
                    ThisAssembly.Project.MSBuildProjectDirectory,
                    ThisAssembly.Project.IntermediateOutputPath,
                    "generated"),
                "ThisAssembly.*.cs",
                SearchOption.AllDirectories))
            {
                sources.Add(CSharpSyntaxTree.ParseText(
                    File.ReadAllText(thisAssemblyFile),
                    options: args.ParseOptions.WithLanguageVersion(LanguageVersion.Latest),
                    path: thisAssemblyFile,
                    encoding: Encoding.UTF8));
            }

            Compilation compilation = CSharpCompilation.Create(
                "ManualAvatars",
                sources,
                args.MetadataReferences.Select(x => libs.TryGetValue(Path.GetFileName(x.Reference), out var lib) ?
                    MetadataReference.CreateFromFile(lib) :
                    MetadataReference.CreateFromFile(x.Reference)),
                args.CompilationOptions.WithCryptoKeyFile(null).WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            AssertCode.NoErrors(compilation);

            Predicate<Diagnostic> ignored = d =>
                d.Severity == DiagnosticSeverity.Hidden ||
                d.Severity == DiagnosticSeverity.Info;

            var diagnostics = compilation.GetDiagnostics().RemoveAll(ignored);
            var options = EditorConfigOptionsProvider.Create(Directory.EnumerateFiles(
                    Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory, ThisAssembly.Project.IntermediateOutputPath),
                    "*.editorconfig", SearchOption.TopDirectoryOnly));

            var driver = CSharpGeneratorDriver.Create(
                new[] { new AvatarGenerator() },
                parseOptions: args.ParseOptions.WithLanguageVersion(LanguageVersion.Latest),
                optionsProvider: options);

            // Don't timeout if we're debugging.
            var token = Debugger.IsAttached ? default : new CancellationTokenSource(5000).Token;

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out diagnostics, token);
            diagnostics = diagnostics.RemoveAll(ignored);

            AssertCode.NoErrors(compilation);

            // Copy from intermediate output to manual avatars folder.
            var generatedDir = Path.Combine(
                ThisAssembly.Project.MSBuildProjectDirectory,
                ThisAssembly.Project.IntermediateOutputPath,
                "generated",
                nameof(AvatarGenerator));

            var names = new[]
            {
                AvatarNaming.GetName(typeof(ICalculator)) + ".cs",
                AvatarNaming.GetName(typeof(ICalculator), typeof(IDisposable)) + ".cs",
                AvatarNaming.GetName(typeof(Calculator)) + ".cs",
                AvatarNaming.GetName(typeof(CalculatorBase)) + ".cs",
                AvatarNaming.GetName(typeof(ICalculatorMemory)) + ".cs",
                AvatarNaming.GetName(typeof(CalculatorMemory)) + ".cs",
                AvatarNaming.GetName(typeof(CalculatorMemoryBase)) + ".cs",
            };

            foreach (var name in names)
            {
                File.Copy(
                    Path.Combine(generatedDir, name),
                    Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory, @"..\ManualAvatars\Avatars", name),
                    true);
            }
        }
    }
}
