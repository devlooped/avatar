using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    partial class SymbolEquivalence
    {
        class GetHashCodeVisitor
        {
            readonly bool _compareMethodTypeParametersByIndex;
            readonly bool _objectAndDynamicCompareEqually;
            readonly Func<int, IParameterSymbol, int> _parameterAggregator;
            readonly Func<int, ISymbol, int> _symbolAggregator;

            public GetHashCodeVisitor(
                bool compareMethodTypeParametersByIndex,
                bool objectAndDynamicCompareEqually)
            {
                _compareMethodTypeParametersByIndex = compareMethodTypeParametersByIndex;
                _objectAndDynamicCompareEqually = objectAndDynamicCompareEqually;
                _parameterAggregator = (acc, sym) => HashCode.Combine(SymbolEquivalence.ParameterSymbolEqualityComparer.Default.GetHashCode(sym), acc);
                _symbolAggregator = (acc, sym) => GetHashCode(sym, acc);
            }

            public int GetHashCode(ISymbol x, int currentHash)
            {
                if (x == null)
                    return 0;

                x = UnwrapAlias(x);

                // Special case.  If we're comparing signatures then we want to compare 'object'
                // and 'dynamic' as the same.  However, since they're different types, we don't
                // want to bail out using the above check.

                if (x.Kind == SymbolKind.DynamicType ||
                    (_objectAndDynamicCompareEqually && IsObjectType(x)))
                {
                    return HashCode.Combine(typeof(IDynamicTypeSymbol), currentHash);
                }

                return GetHashCodeWorker(x, currentHash);
            }

            int GetHashCodeWorker(ISymbol x, int currentHash)
                => x.Kind switch
                {
                    SymbolKind.ArrayType => CombineHashCodes((IArrayTypeSymbol)x, currentHash),
                    SymbolKind.Assembly => CombineHashCodes((IAssemblySymbol)x, currentHash),
                    SymbolKind.Event => CombineHashCodes((IEventSymbol)x, currentHash),
                    SymbolKind.Field => CombineHashCodes((IFieldSymbol)x, currentHash),
                    SymbolKind.Label => CombineHashCodes((ILabelSymbol)x, currentHash),
                    SymbolKind.Local => CombineHashCodes((ILocalSymbol)x, currentHash),
                    SymbolKind.Method => CombineHashCodes((IMethodSymbol)x, currentHash),
                    SymbolKind.NetModule => CombineHashCodes((IModuleSymbol)x, currentHash),
                    SymbolKind.NamedType => CombineHashCodes((INamedTypeSymbol)x, currentHash),
                    SymbolKind.Namespace => CombineHashCodes((INamespaceSymbol)x, currentHash),
                    SymbolKind.Parameter => CombineHashCodes((IParameterSymbol)x, currentHash),
                    SymbolKind.PointerType => CombineHashCodes((IPointerTypeSymbol)x, currentHash),
                    SymbolKind.Property => CombineHashCodes((IPropertySymbol)x, currentHash),
                    SymbolKind.RangeVariable => CombineHashCodes((IRangeVariableSymbol)x, currentHash),
                    SymbolKind.TypeParameter => CombineHashCodes((ITypeParameterSymbol)x, currentHash),
                    SymbolKind.Preprocessing => CombineHashCodes((IPreprocessingSymbol)x, currentHash),
                    _ => -1,
                };

            int CombineHashCodes(IArrayTypeSymbol x, int currentHash)
            {
                return
                    HashCode.Combine(x.Rank,
                    GetHashCode(x.ElementType, currentHash));
            }

            int CombineHashCodes(IAssemblySymbol x, int currentHash)
                => HashCode.Combine(AssemblyIdentityComparer.SimpleNameComparer.GetHashCode(x), currentHash);

            int CombineHashCodes(IFieldSymbol x, int currentHash)
            {
                return
                    HashCode.Combine(x.Name,
                    GetHashCode(x.ContainingSymbol, currentHash));
            }

            static int CombineHashCodes(ILabelSymbol x, int currentHash)
            {
                return
                    HashCode.Combine(x.Name,
                    HashCode.Combine(x.Locations.FirstOrDefault(), currentHash));
            }

            static int CombineHashCodes(ILocalSymbol x, int currentHash)
                => HashCode.Combine(x.Locations.FirstOrDefault(), currentHash);

            static int CombineHashCodes<T>(ImmutableArray<T> array, int currentHash, Func<int, T, int> func)
                => array.Aggregate<int, T>(currentHash, func);

            int CombineHashCodes(IMethodSymbol x, int currentHash)
            {
                currentHash = HashCode.Combine(x.MetadataName, currentHash);
                if (x.MethodKind == MethodKind.AnonymousFunction)
                {
                    return HashCode.Combine(x.Locations.FirstOrDefault(), currentHash);
                }

                currentHash =
                    HashCode.Combine(IsPartialMethodImplementationPart(x),
                    HashCode.Combine(IsPartialMethodDefinitionPart(x),
                    HashCode.Combine(x.IsDefinition,
                    HashCode.Combine(IsConstructedFromSelf(x),
                    HashCode.Combine(x.Arity,
                    HashCode.Combine(x.Parameters.Length,
                    HashCode.Combine(x.Name, currentHash)))))));

                var checkContainingType = CheckContainingType(x);
                if (checkContainingType)
                {
                    currentHash = GetHashCode(x.ContainingSymbol, currentHash);
                }

                currentHash =
                    CombineHashCodes(x.Parameters, currentHash, _parameterAggregator);

                return IsConstructedFromSelf(x)
                    ? currentHash
                    : CombineHashCodes(x.TypeArguments, currentHash, _symbolAggregator);
            }

            int CombineHashCodes(IModuleSymbol x, int currentHash)
                => CombineHashCodes(x.ContainingAssembly, HashCode.Combine(x.Name, currentHash));

            int CombineHashCodes(INamedTypeSymbol x, int currentHash)
            {
                currentHash = CombineNamedTypeHashCode(x, currentHash);

                if (x is IErrorTypeSymbol errorType)
                {
                    foreach (var candidate in errorType.CandidateSymbols)
                    {
                        if (candidate is INamedTypeSymbol candidateNamedType)
                        {
                            currentHash = CombineNamedTypeHashCode(candidateNamedType, currentHash);
                        }
                    }
                }

                return currentHash;
            }

            int CombineNamedTypeHashCode(INamedTypeSymbol x, int currentHash)
            {
                if (x.IsTupleType)
                {
                    return HashCode.Combine(currentHash, CombineValues(x.TupleElements));
                }

                // If we want object and dynamic to be the same, and this is 'object', then return
                // the same hash we do for 'dynamic'.
                currentHash =
                    HashCode.Combine(x.IsDefinition,
                    HashCode.Combine(IsConstructedFromSelf(x),
                    HashCode.Combine(x.Arity,
                    HashCode.Combine((int)GetTypeKind(x),
                    HashCode.Combine(x.Name,
                    HashCode.Combine(x.IsAnonymousType,
                    HashCode.Combine(x.IsUnboundGenericType,
                    GetHashCode(x.ContainingSymbol, currentHash))))))));

                if (x.IsAnonymousType)
                {
                    return CombineAnonymousTypeHashCode(x, currentHash);
                }

                return IsConstructedFromSelf(x) || x.IsUnboundGenericType
                    ? currentHash
                    : CombineHashCodes(x.TypeArguments, currentHash, _symbolAggregator);
            }

            int CombineAnonymousTypeHashCode(INamedTypeSymbol x, int currentHash)
            {
                if (x.TypeKind == TypeKind.Delegate)
                {
                    return GetHashCode(x.DelegateInvokeMethod!, currentHash);
                }
                else
                {
                    var xMembers = x.GetValidAnonymousTypeProperties();

                    return xMembers.Aggregate(currentHash, (a, p) =>
                    {
                        return HashCode.Combine(p.Name,
                            HashCode.Combine(p.IsReadOnly,
                            GetHashCode(p.Type, a)));
                    });
                }
            }

            int CombineHashCodes(INamespaceSymbol x, int currentHash)
            {
                return
                    HashCode.Combine(x.IsGlobalNamespace,
                    HashCode.Combine(x.Name,
                    GetHashCode(x.ContainingSymbol, currentHash)));
            }

            int CombineHashCodes(IParameterSymbol x, int currentHash)
            {
                return
                    HashCode.Combine(x.IsRefOrOut(),
                    HashCode.Combine(x.Name,
                    GetHashCode(x.Type,
                    GetHashCode(x.ContainingSymbol, currentHash))));
            }

            int CombineHashCodes(IPointerTypeSymbol x, int currentHash)
            {
                return
                    HashCode.Combine(typeof(IPointerTypeSymbol).GetHashCode(),
                    GetHashCode(x.PointedAtType, currentHash));
            }

            int CombineHashCodes(IPropertySymbol x, int currentHash)
            {
                currentHash =
                    HashCode.Combine(x.IsIndexer,
                    HashCode.Combine(x.Name,
                    HashCode.Combine(x.Parameters.Length,
                    GetHashCode(x.ContainingSymbol, currentHash))));

                return CombineHashCodes(x.Parameters, currentHash, _parameterAggregator);
            }

            int CombineHashCodes(IEventSymbol x, int currentHash)
            {
                return
                    HashCode.Combine(x.Name,
                    GetHashCode(x.ContainingSymbol, currentHash));
            }

            public int CombineHashCodes(ITypeParameterSymbol x, int currentHash)
            {
                Debug.Assert(
                    (x.TypeParameterKind == TypeParameterKind.Method && IsConstructedFromSelf(x.DeclaringMethod!)) ||
                    (x.TypeParameterKind == TypeParameterKind.Type && IsConstructedFromSelf(x.ContainingType)) ||
                    x.TypeParameterKind == TypeParameterKind.Cref);

                currentHash =
                    HashCode.Combine(x.Ordinal,
                    HashCode.Combine((int)x.TypeParameterKind, currentHash));

                if (x.TypeParameterKind == TypeParameterKind.Method && _compareMethodTypeParametersByIndex)
                {
                    return currentHash;
                }

                if (x.TypeParameterKind == TypeParameterKind.Type && x.ContainingType.IsAnonymousType)
                {
                    // Anonymous type type parameters compare by index as well to prevent
                    // recursion.
                    return currentHash;
                }

                if (x.TypeParameterKind == TypeParameterKind.Cref)
                {
                    return currentHash;
                }

                return
                    GetHashCode(x.ContainingSymbol, currentHash);
            }

            static int CombineHashCodes(IRangeVariableSymbol x, int currentHash)
                => HashCode.Combine(x.Locations.FirstOrDefault(), currentHash);

            static int CombineHashCodes(IPreprocessingSymbol x, int currentHash)
                => HashCode.Combine(x.GetHashCode(), currentHash);
        }
    }
}
