using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Stunts
{
    /// <summary>
    /// Wraps the actual generator which is <see cref="StuntSourceGenerator"/> so that 
    /// the dependent files are resolved from the relevant tools directory.
    /// </summary>
    [Generator]
    internal class DependencyResolverGenerator : ISourceGenerator
    {
        static readonly string logFile = Environment.ExpandEnvironmentVariables(@"%TEMP%\Stunts.txt");
        static string? resolveDir;

        ISourceGenerator? generator;

        static DependencyResolverGenerator() => AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            if (resolveDir == null)
                return null;

            var name = new AssemblyName(args.Name).Name;
            if (name == null)
                return null;

            var file = Path.GetFullPath(Path.Combine(resolveDir, name + ".dll"));

            if (File.Exists(file))
            {
#if DEBUG
                File.AppendAllText(logFile, $"Resolved {file}\r\n");
#endif
                return Assembly.LoadFrom(file);
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(GeneratorExecutionContext context)
        {
            context.AnalyzerConfigOptions.CheckDebugger(nameof(DependencyResolverGenerator));

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.StuntAnalyzerDir", out var analyerDir))
                resolveDir = analyerDir;

            (generator ??= new StuntSourceGenerator()).Execute(context);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Initialize(GeneratorInitializationContext context) 
            => (generator ??= new StuntSourceGenerator()).Initialize(context);
    }
}