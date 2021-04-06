using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using static WorkspaceHelper;

namespace Avatars.UnitTests
{
    public class SymbolTests
    {
        [Fact]
        public async Task GivenSameSignature_ThenHasSameSignature()
        {
            var (_, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.AddDocument("Code.cs", SourceText.From(@"
using System;

public interface IFoo
{
    string Do(int value);
}

public interface IBar
{
    string Do(int value);
}
", Encoding.UTF8))
                .Project.GetCompilationAsync();

            var foo = compilation.GetTypeByMetadataName("IFoo") ?? throw new InvalidOperationException();
            var bar = compilation.GetTypeByMetadataName("IBar") ?? throw new InvalidOperationException();

            var first = foo.GetMembers().First();
            var second = bar.GetMembers().First();

            Assert.True(SignatureComparer.Default.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(first, second));
        }

        [Fact]
        public async Task GivenDifferentReturnType_TheSignatureNotSame()
        {
            var (_, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.AddDocument("Code.cs", SourceText.From(@"
using System;

public interface IFoo
{
    bool Do(int value);
}

public interface IBar
{
    string Do(int value);
}
", Encoding.UTF8))
                .Project.GetCompilationAsync();

            var foo = compilation.GetTypeByMetadataName("IFoo") ?? throw new InvalidOperationException();
            var bar = compilation.GetTypeByMetadataName("IBar") ?? throw new InvalidOperationException();

            var first = foo.GetMembers().First();
            var second = bar.GetMembers().First();

            Assert.False(SignatureComparer.Default.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(first, second));
        }

    }
}
