﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Avatars.UnitTests
{
    /// <summary>
    /// Runs all the scenarios in the Scenarios folder using the source 
    /// generator to process them.
    /// </summary>
    public class Scenarios
    {
        static readonly HashSet<string> additionalSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IRunnable.cs",
            "Avatar.cs",
            "Avatar.StaticFactory.cs"
        };

        // Run particular scenarios using the TD.NET ad-hoc runner.
        public void RunScenario() => new Scenarios().Run(ThisAssembly.Constants.Scenarios.ClassBaseType);

        [Theory]
        [MemberData(nameof(GetScenarios))]
        public void Run(string path)
        {
            // Getting the generated output can be flaky sometimes, so retry
            var (diagnostics, compilation) = Retry(() => GetGeneratedOutput(
                Path.IsPathRooted(path) ? path :
                Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory, path)));

            Assert.True(diagnostics.IsEmpty,
                "Generated code produced diagnostics:\r\n" +
                Environment.NewLine +
                string.Join(Environment.NewLine, diagnostics.Where(d => d.Id != "CS0436").Select(d => d.ToString())));

            var assembly = compilation.Emit();
            var type = assembly.GetTypes().FirstOrDefault(t => t.GetInterfaces().Any(i => i.Name == nameof(IRunnable)));

            Assert.NotNull(type);

            try
            {
                var runnable = Activator.CreateInstance(type);
                type.InvokeMember("Run", BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, null, runnable, null);
            }
            catch (TargetInvocationException tie)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            }
        }

        T Retry<T>(Func<T> func, int times = 2)
        {
            for (var i = 1; i <= times; i++)
            {
                try
                {
                    return func();
                }
                catch (Exception) when (i < times)
                {
                    Thread.Sleep(500);
                    GC.Collect();
                }
            }

            return default;
        }

        public static IEnumerable<object[]> GetScenarios()
            => Directory.EnumerateFiles(Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory, "Scenarios"), "*.cs")
                .Select(file => new object[] { Path.Combine("Scenarios", Path.GetFileName(file)) });

        static Scenarios()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var requested = new AssemblyName(args.Name);
                if (requested.Name == null)
                    return null;

                var file = Path.GetFullPath(requested.Name + ".dll");
                if (File.Exists(file))
                    return Assembly.LoadFrom(file);

                return null;
            };
        }

        static (ImmutableArray<Diagnostic>, Compilation) GetGeneratedOutput(string path)
        {
            var libs = new HashSet<string>(File.ReadAllLines("lib.txt"), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => Path.GetFileName(x));

            var args = CSharpCommandLineParser.Default.Parse(
                File.ReadAllLines("csc.txt"), ThisAssembly.Project.MSBuildProjectDirectory, sdkDirectory: null);

            var syntaxTree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(path),
                options: args.ParseOptions.WithLanguageVersion(LanguageVersion.Latest),
                path: new FileInfo(path).FullName,
                encoding: Encoding.UTF8);

            var sources = new List<SyntaxTree>
            {
                syntaxTree
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
                Path.GetFileNameWithoutExtension(path),
                sources,
                args.MetadataReferences.Select(x => libs.TryGetValue(Path.GetFileName(x.Reference), out var lib) ?
                    MetadataReference.CreateFromFile(lib) :
                    MetadataReference.CreateFromFile(x.Reference)),
                args.CompilationOptions.WithCryptoKeyFile(null).WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            Predicate<Diagnostic> ignored = d =>
                d.Severity == DiagnosticSeverity.Hidden ||
                d.Severity == DiagnosticSeverity.Info;

            var diagnostics = compilation.GetDiagnostics().RemoveAll(ignored);
            if (diagnostics.Any())
                return (diagnostics, compilation);

            var driver = CSharpGeneratorDriver.Create(
                new[] { new AvatarGenerator() },
                parseOptions: args.ParseOptions.WithLanguageVersion(LanguageVersion.Latest),
                optionsProvider: EditorConfigOptionsProvider.Create(Directory.EnumerateFiles(
                    Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory, ThisAssembly.Project.IntermediateOutputPath),
                    "*.editorconfig", SearchOption.TopDirectoryOnly)));

            // Don't timeout if we're debugging.
            var token = Debugger.IsAttached ? default : new CancellationTokenSource(5000).Token;

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out diagnostics, token);
            diagnostics = diagnostics.RemoveAll(ignored);

            return (diagnostics, output);
        }
    }
}
