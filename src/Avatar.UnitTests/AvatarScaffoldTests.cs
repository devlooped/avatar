﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Avatars.CodeActions;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Sample;
using Xunit;
using Xunit.Abstractions;
using static WorkspaceHelper;

namespace Avatars.UnitTests
{
    public class AvatarScaffoldTests
    {
        readonly ITestOutputHelper output;

        public AvatarScaffoldTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public async Task EnsureScaffold()
        {
            var (_, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync();
            var naming = new NamingConvention();
            var context = new ProcessorContext(compilation, project.ParseOptions);
            var scaffold = new AvatarScaffold(context);

            var types = new INamedTypeSymbol[]
            {
                compilation.GetTypeByMetadataName("System.IDisposable") ?? throw new InvalidOperationException(),
                compilation.GetTypeByMetadataName("System.IServiceProvider") ?? throw new InvalidOperationException(),
            };

            var factory = AvatarSyntaxFactory.CreateFactory(LanguageNames.CSharp);
            var document = await scaffold.ScaffoldAsync(factory.CreateSyntax(naming, types), naming);
            var syntax = await document.GetSyntaxRootAsync();
            var code = syntax.NormalizeWhitespace().ToFullString();

            if (Debugger.IsAttached)
                output.WriteLine(code);

            compilation = await project
                .AddDocument("test.cs", SourceText.From(code, Encoding.UTF8))
                .Project.GetCompilationAsync();

            var name = naming.GetName(types);
            var assembly = compilation.Emit(true);
            var type = assembly.GetType(naming.GetNamespace(types) + "." + name, true);


            Assert.True(typeof(IDisposable).IsAssignableFrom(type));
            Assert.True(typeof(IServiceProvider).IsAssignableFrom(type));
        }

        [Fact]
        public async Task EnsureAbstractScaffold()
        {
            var (_, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync();
            var naming = new NamingConvention { NameSuffix = "NotImplemented" };
            var context = new ProcessorContext(compilation, project.ParseOptions);
            var scaffold = new AvatarScaffold(context);

            var types = new INamedTypeSymbol[]
            {
                compilation.GetTypeByMetadataName("Sample.CalculatorBase") ?? throw new InvalidOperationException(),
                compilation.GetTypeByMetadataName("System.IDisposable") ?? throw new InvalidOperationException(),
            };

            var factory = AvatarSyntaxFactory.CreateFactory(LanguageNames.CSharp);
            var name = naming.GetName(types);
            var document = await scaffold.ScaffoldAsync(
                factory.CreateSyntax(naming, types),
                naming,
                new[] { CodeFixes.CSharp.ImplementAbstractClass, CodeFixes.CSharp.ImplementInterface },
                Array.Empty<string>());

            var syntax = await document.GetSyntaxRootAsync();
            var code = syntax.NormalizeWhitespace().ToFullString();

            if (Debugger.IsAttached)
                output.WriteLine(code);

            compilation = await project
                .AddDocument("test.cs", SourceText.From(code, Encoding.UTF8))
                .Project.GetCompilationAsync();

            var assembly = compilation.Emit(true);
            var type = assembly.GetType(naming.GetNamespace(types) + "." + name, true);

            Assert.True(typeof(CalculatorBase).IsAssignableFrom(type));
            Assert.True(typeof(IDisposable).IsAssignableFrom(type));

            var instance = (CalculatorBase)Activator.CreateInstance(type);

            Assert.Throws<NotImplementedException>(() => instance.TurnedOn += null);
            Assert.Throws<NotImplementedException>(() => instance.TurnOn());
            Assert.Throws<NotImplementedException>(() => instance.Mode = CalculatorMode.Scientific);
        }

        [Fact]
        public async Task EnsureInterfaceScaffold()
        {
            var (_, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync();
            var naming = new NamingConvention { NameSuffix = "NotImplemented" };
            var context = new ProcessorContext(compilation, project.ParseOptions);
            var scaffold = new AvatarScaffold(context);

            var types = new INamedTypeSymbol[]
            {
                compilation.GetTypeByMetadataName("Sample.ICalculator") ?? throw new InvalidOperationException(),
                compilation.GetTypeByMetadataName("System.IDisposable") ?? throw new InvalidOperationException(),
            };

            var factory = AvatarSyntaxFactory.CreateFactory(LanguageNames.CSharp);
            var name = naming.GetName(types);
            var document = await scaffold.ScaffoldAsync(
                factory.CreateSyntax(naming, types),
                naming,
                new[] { CodeFixes.CSharp.ImplementAbstractClass, CodeFixes.CSharp.ImplementInterface },
                Array.Empty<string>());

            var syntax = await document.GetSyntaxRootAsync();
            var code = syntax.NormalizeWhitespace().ToFullString();

            if (Debugger.IsAttached)
                output.WriteLine(code);

            compilation = await project
                .AddDocument("test.cs", SourceText.From(code, Encoding.UTF8))
                .Project.GetCompilationAsync();

            var assembly = compilation.Emit(true);
            var type = assembly.GetType(naming.GetNamespace(types) + "." + name, true);

            Assert.True(typeof(ICalculator).IsAssignableFrom(type));
            Assert.True(typeof(IDisposable).IsAssignableFrom(type));

            var instance = (ICalculator)Activator.CreateInstance(type);

            Assert.Throws<NotImplementedException>(() => instance.TurnedOn += null);
            Assert.Throws<NotImplementedException>(() => instance.TurnOn());
            Assert.Throws<NotImplementedException>(() => instance.Mode = CalculatorMode.Scientific);

            Assert.Throws<NotImplementedException>(() => ((IDisposable)instance).Dispose());
        }
    }
}
