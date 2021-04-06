using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    partial class SymbolEquivalence
    {
        internal class ParameterSymbolEqualityComparer : IEqualityComparer<IParameterSymbol>
        {
            ParameterSymbolEqualityComparer() { }

            public static ParameterSymbolEqualityComparer Default { get; } = new ParameterSymbolEqualityComparer();

            public bool Equals(
                IParameterSymbol x,
                IParameterSymbol y,
                Dictionary<INamedTypeSymbol, INamedTypeSymbol>? equivalentTypesWithDifferingAssemblies,
                bool compareParameterName,
                bool isCaseSensitive = true)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (x == null || y == null)
                    return false;

                var nameComparisonCheck = true;
                if (compareParameterName)
                {
                    nameComparisonCheck = isCaseSensitive ?
                        x.Name == y.Name
                        : string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                }

                // See the comment in the outer type.  If we're comparing two parameters for
                // equality, then we want to consider method type parameters by index only.

                return
                    AreRefKindsEquivalent(x.RefKind, y.RefKind, false) &&
                    nameComparisonCheck &&
                    GetEquivalenceVisitor().AreEquivalent(x.CustomModifiers, y.CustomModifiers, equivalentTypesWithDifferingAssemblies) &&
                    TypeSymbolEquivalenceComparer.Default.Equals(x.Type, y.Type, equivalentTypesWithDifferingAssemblies);
            }

            public bool Equals(IParameterSymbol x, IParameterSymbol y)
                => Equals(x, y, null, false, false);

            public bool Equals(IParameterSymbol x, IParameterSymbol y, bool compareParameterName, bool isCaseSensitive = true)
                => Equals(x, y, null, compareParameterName, isCaseSensitive);

            public int GetHashCode(IParameterSymbol x)
            {
                if (x == null)
                {
                    return 0;
                }

                return
                    HashCode.Combine(x.IsRefOrOut(),
                    TypeSymbolEquivalenceComparer.Default.GetHashCode(x.Type));
            }
        }

    }
}
