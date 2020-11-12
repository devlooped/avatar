﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Sdk;
using static WorkspaceHelper;

namespace Avatars.UnitTests
{
    public class SymbolFullNameTests
    {
        [InlineData("System.IDisposable")]
        [InlineData("System.Threading.Tasks.Task<System.String>")]
        [InlineData("System.Collections.Generic.IEnumerable<System.Environment+SpecialFolder>")]
        [InlineData("System.Collections.Generic.IDictionary<System.Int32[], System.Collections.Generic.KeyValuePair<System.Environment+SpecialFolder, System.Nullable<System.Boolean>>>")]
        [Theory]
        public async Task GivenAFullName_ThenCanResolveSymbolAndRoundtrip(string fullName)
        {
            var (_, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync() ?? throw new XunitException();

            var symbol = compilation!.GetTypeByFullName(fullName);

            Assert.NotNull(symbol);

            var symbol2 = compilation!.GetTypeByFullName(symbol.ToFullName());

            Assert.NotNull(symbol2);

            Assert.Equal(symbol, symbol2);
        }

        [Fact]
        public async Task GivenASymbol_ThenCanRoundtripWithFullName()
        {
            var (workspace, project) = CreateWorkspaceAndProject(LanguageNames.CSharp);
            var compilation = await project.GetCompilationAsync() ?? throw new XunitException();

            var dictionary = compilation!.GetTypeByMetadataName(typeof(IDictionary<,>).FullName!)
                 ?? throw new XunitException();
            var list = compilation.GetTypeByMetadataName(typeof(IList<>).FullName!)
                 ?? throw new XunitException();
            var enumerable = compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName!)
                 ?? throw new XunitException();
            var intsymbol = compilation.GetTypeByMetadataName(typeof(int).FullName!)
                 ?? throw new XunitException();

            var ints = compilation.CreateArrayTypeSymbol(intsymbol, 1);
            var nullable = compilation.GetTypeByMetadataName(typeof(Nullable<>).FullName!)
                 ?? throw new XunitException();
            var special = compilation.GetTypeByMetadataName(typeof(Environment.SpecialFolder).FullName!)
                 ?? throw new XunitException();
            var pair = compilation.GetTypeByMetadataName(typeof(KeyValuePair<,>).FullName!)
                 ?? throw new XunitException();

            var pairof = pair.Construct(ints, nullable.Construct(special));
            var enumpairs = enumerable.Construct(pairof);
            var ints2 = compilation.CreateArrayTypeSymbol(intsymbol, 2);
            var listof = list.Construct(ints2);
            var dictof = dictionary.Construct(listof, enumpairs);

            var display = dictof.ToFullName();

            var resolved = compilation.GetTypeByFullName(display);

            Assert.Equal(dictof, resolved);
        }
    }
}
