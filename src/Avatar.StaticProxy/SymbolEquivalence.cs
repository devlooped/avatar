using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    /// <summary>
    /// Provides a way to test two symbols for equivalence.  While there are ways to ask for
    /// different sorts of equivalence, the following must hold for two symbols to be considered
    /// equivalent.
    /// <list type="number">
    /// <item>The kinds of the two symbols must match.</item>
    /// <item>The names of the two symbols must match.</item>
    /// <item>The arity of the two symbols must match.</item>
    /// <item>If the symbols are methods or parameterized properties, then the signatures of the two
    /// symbols must match.</item>
    /// <item>Both symbols must be definitions or must be instantiations.  If they are instantiations,
    /// then they must be instantiated in the same manner.</item>
    /// <item>The containing symbols of the two symbols must be equivalent.</item>
    /// <item>Nullability of symbols is not involved in the comparison.</item>
    /// </list>
    /// Note: equivalence does not concern itself with whole symbols.  Two types are considered
    /// equivalent if the above hold, even if one type has different members than the other.  Note:
    /// type parameters, and signature parameters are not considered 'children' when comparing
    /// symbols.
    /// 
    /// Options are provided to tweak the above slightly.  For example, by default, symbols are
    /// equivalent only if they come from the same assembly or different assemblies of the same simple name.
    /// However, one can ask if two symbols are equivalent even if their assemblies differ.
    /// </summary>
    partial class SymbolEquivalence
    {
        static readonly ImmutableArray<EquivalenceVisitor> _equivalenceVisitors;
        static readonly ImmutableArray<GetHashCodeVisitor> _getHashCodeVisitors;

        static SymbolEquivalence()
        {
            // There are only so many EquivalenceVisitors and GetHashCodeVisitors we can have.
            // Create them all up front.
            var equivalenceVisitorsBuilder = ImmutableArray.CreateBuilder<EquivalenceVisitor>();
            equivalenceVisitorsBuilder.Add(new EquivalenceVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true));
            equivalenceVisitorsBuilder.Add(new EquivalenceVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: false));
            equivalenceVisitorsBuilder.Add(new EquivalenceVisitor(compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: true));
            equivalenceVisitorsBuilder.Add(new EquivalenceVisitor(compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: false));
            _equivalenceVisitors = equivalenceVisitorsBuilder.ToImmutable();

            var getHashCodeVisitorsBuilder = ImmutableArray.CreateBuilder<GetHashCodeVisitor>();
            getHashCodeVisitorsBuilder.Add(new GetHashCodeVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: true));
            getHashCodeVisitorsBuilder.Add(new GetHashCodeVisitor(compareMethodTypeParametersByIndex: true, objectAndDynamicCompareEqually: false));
            getHashCodeVisitorsBuilder.Add(new GetHashCodeVisitor(compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: true));
            getHashCodeVisitorsBuilder.Add(new GetHashCodeVisitor(compareMethodTypeParametersByIndex: false, objectAndDynamicCompareEqually: false));
            _getHashCodeVisitors = getHashCodeVisitorsBuilder.ToImmutable();
        }

        // Very subtle logic here.  When checking if two parameters are the same, we can end up with
        // a tricky infinite loop.  Specifically, consider the case if the parameter refers to a
        // method type parameter.  i.e. "void Goo<T>(IList<T> arg)".  If we compare two method type
        // parameters for equality, then we'll end up asking if their methods are the same.  And that
        // will cause us to check if their parameters are the same.  And then we'll be right back
        // here.  So, instead, when asking if parameters are equal, we pass an appropriate flag so
        // that method type parameters are just compared by index and nothing else.
        static EquivalenceVisitor GetEquivalenceVisitor(
            bool compareMethodTypeParametersByIndex = false, bool objectAndDynamicCompareEqually = false)
        {
            var visitorIndex = GetVisitorIndex(compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually);
            return _equivalenceVisitors[visitorIndex];
        }

        static GetHashCodeVisitor GetGetHashCodeVisitor(
            bool compareMethodTypeParametersByIndex, bool objectAndDynamicCompareEqually)
        {
            var visitorIndex = GetVisitorIndex(compareMethodTypeParametersByIndex, objectAndDynamicCompareEqually);
            return _getHashCodeVisitors[visitorIndex];
        }

        static int GetVisitorIndex(
            bool compareMethodTypeParametersByIndex, bool objectAndDynamicCompareEqually)
        {
            if (compareMethodTypeParametersByIndex)
            {
                if (objectAndDynamicCompareEqually)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                if (objectAndDynamicCompareEqually)
                {
                    return 2;
                }
                else
                {
                    return 3;
                }
            }
        }

        static bool AreRefKindsEquivalent(RefKind rk1, RefKind rk2, bool distinguishRefFromOut)
        {
            return distinguishRefFromOut
                ? rk1 == rk2
                : (rk1 == RefKind.None) == (rk2 == RefKind.None);
        }

        static SymbolKind GetKindAndUnwrapAlias(ref ISymbol symbol)
        {
            var k = symbol.Kind;
            if (k == SymbolKind.Alias)
            {
                symbol = ((IAliasSymbol)symbol).Target;
                k = symbol.Kind;
            }

            return k;
        }

        static TypeKind GetTypeKind(INamedTypeSymbol x)
        {
            // Treat static classes as modules.
            var k = x.TypeKind;
            return k == TypeKind.Module ? TypeKind.Class : k;
        }

        static bool IsConstructedFromSelf(INamedTypeSymbol symbol)
            => SymbolEqualityComparer.Default.Equals(symbol, symbol.ConstructedFrom);

        static bool IsConstructedFromSelf(IMethodSymbol symbol)
            => SymbolEqualityComparer.Default.Equals(symbol.ConstructedFrom);

        static bool IsObjectType(ISymbol symbol)
            => symbol.IsKind(SymbolKind.NamedType, out ITypeSymbol? typeSymbol) && typeSymbol?.SpecialType == SpecialType.System_Object;

        static bool IsPartialMethodDefinitionPart(IMethodSymbol symbol)
            => symbol.PartialImplementationPart != null;

        static bool IsPartialMethodImplementationPart(IMethodSymbol symbol)
            => symbol.PartialDefinitionPart != null;

        static bool CheckContainingType(IMethodSymbol x)
        {
            if (x.MethodKind == MethodKind.DelegateInvoke &&
                x.ContainingType != null &&
                x.ContainingType.IsAnonymousType)
            {
                return false;
            }
            else if (x.MethodKind == MethodKind.FunctionPointerSignature)
            {
                // We use the signature of a function pointer type to determine equivalence, but
                // function pointer types do not have containing types.
                return false;
            }

            return true;
        }

        static int CombineValues<T>(ImmutableArray<T> values, int maxItemsToHash = int.MaxValue)
        {
            if (values.IsDefaultOrEmpty)
            {
                return 0;
            }

            var hashCode = 0;
            var count = 0;
            foreach (var value in values)
            {
                if (count++ >= maxItemsToHash)
                {
                    break;
                }

                // Should end up with a constrained virtual call to object.GetHashCode (i.e. avoid boxing where possible).
                if (value != null)
                {
                    hashCode = HashCode.Combine(value.GetHashCode(), hashCode);
                }
            }

            return hashCode;
        }

        static IEnumerable<INamedTypeSymbol> Unwrap(INamedTypeSymbol namedType)
        {
            yield return namedType;

            if (namedType is IErrorTypeSymbol errorType)
            {
                foreach (var type in errorType.CandidateSymbols.OfType<INamedTypeSymbol>())
                {
                    yield return type;
                }
            }
        }

        static ISymbol UnwrapAlias(ISymbol symbol)
            => symbol.IsKind(SymbolKind.Alias, out IAliasSymbol? alias) && alias != null ? alias.Target : symbol;
    }
}
