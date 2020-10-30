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

namespace Stunts.UnitTests
{
    public class StuntSourceGeneratorTests
    {
        [Fact]
        public void GenerateStuntNestedType()
        {
            var code = @"
using System;
using Stunts;

namespace UnitTests
{
    public class Test
    {
        public void Do()
        {
            var stunt = Stunt.Of<IFoo>();
        }
        
        public interface IFoo 
        {
            void Do();
        }
    }
}";

            var (diagnostics, compilation) = GetGeneratedOutput(code);

            Assert.Empty(diagnostics);

            var assembly = compilation.Emit();

            Assert.NotNull(assembly.GetTypes().FirstOrDefault(t => t.Name == "IFooStunt"));
        }

        [Fact]
        public void GenerateStuntRefReturns()
        {
            var code = @"
using System;
using Stunts;

namespace UnitTests
{
    public interface IMemory
    {
        ref int Get();
    }

    public class Test
    {
        public void Do()
        {
            var stunt = Stunt.Of<IMemory>();
            stunt.AddBehavior(new DefaultValueBehavior());
            ref int value = ref stunt.Get();
        }
    }
}";

            var (diagnostics, compilation) = GetGeneratedOutput(code);

            Assert.Empty(diagnostics);

            var assembly = compilation.Emit();

            Assert.NotNull(assembly.GetTypes().FirstOrDefault(t => t.Name == "IMemoryStunt"));

            var test = Activator.CreateInstance(assembly.GetType("UnitTests.Test"));
            test.GetType().InvokeMember("Do", BindingFlags.InvokeMethod, null, test, null);
        }

        [Fact]
        public void GenerateStuntRefReturnsRefOut()
        {
            var code = @"
using System;
using Stunts;

namespace UnitTests
{
    public interface IMemory
    {
        ref int Get(ref string name, out int count);
    }

    public class Test
    {
        public void Do()
        {
            var stunt = Stunt.Of<IMemory>();
            stunt.AddBehavior(new DefaultValueBehavior());

            var name = ""foo"";
            ref int value = ref stunt.Get(ref name, out var count);
        }
    }
}";

            var (diagnostics, compilation) = GetGeneratedOutput(code);

            Assert.Empty(diagnostics);

            var assembly = compilation.Emit();

            Assert.NotNull(assembly.GetTypes().FirstOrDefault(t => t.Name == "IMemoryStunt"));

            var test = Activator.CreateInstance(assembly.GetType("UnitTests.Test"));
            test.GetType().InvokeMember("Do", BindingFlags.InvokeMethod, null, test, null);
        }

        [Fact]
        public void GeneratesOneStuntPerType()
        {
            var code = @"
using System;

namespace Stunts.UnitTests
{
    public class Test
    {
        public void Do()
        {
            var stunt = Stunt.Of<IDisposable>();
            var services = Stunt.Of<IServiceProvider>();

            Console.WriteLine(stunt.ToString());
        }
        
        public void DoToo()
        {
            var other = Stunt.Of<IDisposable>();
            var sp = Stunt.Of<IServiceProvider>();
        }
    }
}";

            var (diagnostics, compilation) = GetGeneratedOutput(code);

            Assert.Empty(diagnostics);
            
            var assembly = compilation.Emit();

            Assert.NotNull(assembly.GetType(StuntNaming.GetFullName(typeof(IDisposable))));
            Assert.NotNull(assembly.GetType(StuntNaming.GetFullName(typeof(IServiceProvider))));
        }

        [InlineData(typeof(IDisposable), typeof(IServiceProvider), typeof(IFormatProvider))]
        [InlineData(typeof(ICollection<string>), typeof(IDisposable))]
        [InlineData(typeof(IDictionary<IReadOnlyCollection<string>, IReadOnlyList<int>>), typeof(IDisposable))]
        [InlineData(typeof(IDisposable))]
        [Theory]
        public void GenerateCode(params Type[] types)
        {
            var code = @"
using System;
using Stunts;

namespace UnitTests
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
                    CSharpSyntaxTree.ParseText(File.ReadAllText("Stunts/Stunt.cs"), path: "Stunt.cs"),
                    CSharpSyntaxTree.ParseText(File.ReadAllText("Stunts/Stunt.StaticFactory.cs"), path: "Stunt.StaticFactory.cs"),
                }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var diagnostics = compilation.GetDiagnostics().RemoveAll(d => d.Severity == DiagnosticSeverity.Hidden || d.Severity == DiagnosticSeverity.Info);
            if (diagnostics.Any())
                return (diagnostics, compilation);

            ISourceGenerator generator = new StuntSourceGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out diagnostics);

            return (diagnostics, output);
        }
    }
}
