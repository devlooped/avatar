using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    partial class SymbolEquivalence
    {
        internal class TypeSymbolEquivalenceComparer : IEqualityComparer<ITypeSymbol>
        {
            public static TypeSymbolEquivalenceComparer Default { get; } = new TypeSymbolEquivalenceComparer();

            TypeSymbolEquivalenceComparer() { }

            public bool Equals(ITypeSymbol x, ITypeSymbol y)
                => this.Equals(x, y, null);

            public bool Equals(ITypeSymbol x, ITypeSymbol y, Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies)
                => GetEquivalenceVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true).AreEquivalent(x, y, equivalentTypesWithDifferingAssemblies);

            public int GetHashCode(ITypeSymbol x)
                => GetGetHashCodeVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true).GetHashCode(x, currentHash: 0);
        }
    }
}
