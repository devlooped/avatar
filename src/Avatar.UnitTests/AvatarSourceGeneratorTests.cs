using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TypeNameFormatter;
using Xunit;

namespace Avatars.UnitTests
{
    public class AvatarSourceGeneratorTests
    {
        // NOTE: add more representative types here if needed when fixing codegen
        [InlineData(typeof(IDisposable), typeof(IServiceProvider), typeof(IFormatProvider))]
        [InlineData(typeof(ICollection<string>), typeof(IDisposable))]
        [InlineData(typeof(IDictionary<IReadOnlyCollection<string>, IReadOnlyList<int>>), typeof(IDisposable))]
        [InlineData(typeof(IDisposable))]
        [Theory]
        public void GenerateCode(params Type[] types)
        {
            var code = @"
using System;
using Avatars;

namespace UnitTests
{
    public class Test
    {
        public void Do()
        {
            var avatar = Avatar.Of<$$>();
            Console.WriteLine(avatar.ToString());
        }
    }
}".Replace("$$", string.Join(", ", types.Select(t =>
                     t.GetFormattedName(TypeNameFormatOptions.Namespaces))));

            var (diagnostics, compilation) = GetGeneratedOutput(code);

            Assert.Empty(diagnostics);

            var assembly = compilation.Emit();

            var stuntName = AvatarNaming.GetFullName(types.First(), types.Skip(1).ToArray());
            var stuntType = assembly.GetType(stuntName);

            Assert.NotNull(stuntType);

            var avatar = Activator.CreateInstance(stuntType!);

            foreach (var type in types)
            {
                Assert.IsAssignableFrom(type, avatar);
            }
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
                    CSharpSyntaxTree.ParseText(File.ReadAllText("Avatar/Avatar.cs"), path: "Avatar.cs"),
                    CSharpSyntaxTree.ParseText(File.ReadAllText("Avatar/Avatar.StaticFactory.cs"), path: "Avatar.StaticFactory.cs"),
                }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = compilation.GetDiagnostics().RemoveAll(d => d.Severity == DiagnosticSeverity.Hidden || d.Severity == DiagnosticSeverity.Info);
            if (diagnostics.Any())
                return (diagnostics, compilation);

            ISourceGenerator generator = new AvatarSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out diagnostics);

            return (diagnostics, output);
        }
    }
}
