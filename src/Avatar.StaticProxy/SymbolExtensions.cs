using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    static class SymbolExtensions
    {
        public static bool IsKind(this ISymbol? symbol, SymbolKind kind)
            => symbol.MatchesKind(kind);

        public static bool MatchesKind(this ISymbol? symbol, SymbolKind kind)
            => symbol?.Kind == kind;

        public static bool IsKind<TSymbol>(this ISymbol symbol, SymbolKind kind, out TSymbol? result) where TSymbol : class, ISymbol
        {
            if (!symbol.IsKind(kind))
            {
                result = null;
                return false;
            }

            result = (TSymbol)symbol;
            return true;
        }

        public static bool IsPropertyAccessor(this MethodKind kind)
            => kind == MethodKind.PropertyGet || kind == MethodKind.PropertySet;

        public static bool IsRefOrOut(this IParameterSymbol symbol) => symbol.RefKind switch
        {
            RefKind.Ref or RefKind.Out => true,
            _ => false,
        };

        public static IEnumerable<IPropertySymbol> GetValidAnonymousTypeProperties(this ISymbol symbol)
        {
            return ((INamedTypeSymbol)symbol).GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName);
        }
    }
}
