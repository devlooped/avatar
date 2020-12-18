using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sample;
using TypeNameFormatter;
using Xunit;

namespace Avatars.UnitTests
{
    public interface ITypeGetter
    {
        Type GetType(string assembly, string name);
    }

    public class BaseClass
    {
        public virtual bool TryMixed(int x, int? y, ref string name, out int? z)
        {
            z = x + y;
            return true;
        }
    }

    [CompilerGenerated]
    class BaseClassAvatar : BaseClass, IAvatar
    {
        readonly BehaviorPipeline pipeline = BehaviorPipelineFactory.Default.CreatePipeline<BaseClassAvatar>();
        [CompilerGenerated]
        public BaseClassAvatar() => pipeline.Execute(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(this, m.Arguments)));
        [CompilerGenerated]
        IList<IAvatarBehavior> IAvatar.Behaviors => pipeline.Behaviors;
        [CompilerGenerated]
        public override bool Equals(object obj) => pipeline.Execute<bool>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.Equals(obj), obj), obj));
        [CompilerGenerated]
        public override int GetHashCode() => pipeline.Execute<int>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.GetHashCode())));
        [CompilerGenerated]
        public override string ToString() => pipeline.Execute<string>(new MethodInvocation(this, MethodBase.GetCurrentMethod(), (m, n) => m.CreateValueReturn(base.ToString())));
        [CompilerGenerated]
        public override bool TryMixed(int x, int? y, ref string name, out int? z)
        {
            var _method = MethodBase.GetCurrentMethod();
            z = default;
            var _result = pipeline.Invoke(new MethodInvocation(this, _method, (m, n) =>
            {
                var _name = m.Arguments.Get<string>("name");
                var _z = m.Arguments.Get<int?>("z");
                return m.CreateValueReturn(base.TryMixed(x, y, ref _name, out _z), new ArgumentCollection(_method.GetParameters())
                {{"x", x}, {"y", y}, {"name", _name}, {"z", _z}});
            }, x, y, name, z), true);
            x = _result.Outputs.Get<int>("x");
            y = _result.Outputs.Get<int?>("y");
            name = _result.Outputs.Get<string>("name");
            z = _result.Outputs.Get<int?>("z");
            return (bool)_result.ReturnValue!;
        }
    }

    public class AvatarGeneratorTests
    {
        // NOTE: add more representative types here if needed when fixing codegen
        [InlineData(typeof(IDisposable), typeof(IServiceProvider), typeof(IFormatProvider))]
        [InlineData(typeof(ICollection<string>), typeof(IDisposable))]
        [InlineData(typeof(IDictionary<IReadOnlyCollection<string>, IReadOnlyList<int>>), typeof(IDisposable))]
        [InlineData(typeof(IDisposable))]
        [InlineData(typeof(CalculatorBase))]
        [InlineData(typeof(Calculator))]
        [InlineData(typeof(BaseClass))]
        [InlineData(typeof(INotifyPropertyChanged))]
        [InlineData(typeof(ICustomFormatter))]
        [InlineData(typeof(ITypeGetter))]
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

            var assembly = compilation.Emit(false);

            var name = AvatarNaming.GetFullName(types.First(), types.Skip(1).ToArray());
            var type = assembly.GetType(name);

            Assert.NotNull(type);

            var avatar = Activator.CreateInstance(type!);

            foreach (var t in types)
            {
                Assert.IsAssignableFrom(t, avatar);
            }
        }

        static (ImmutableArray<Diagnostic>, Compilation) GetGeneratedOutput(string source, [CallerMemberName] string? test = null)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source, path: test + ".cs");

            var references = new List<MetadataReference>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
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

            var generator = new AvatarGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out diagnostics);

            return (diagnostics, output);
        }
    }
}
