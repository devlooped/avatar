using System.Collections.Generic;
using System.Linq;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    static class SymbolExtensions
    {
        public static bool IsKind(this ISymbol? symbol, SymbolKind kind) => symbol?.Kind == kind;

        public static bool IsAbstractClass(this ITypeSymbol? symbol) => symbol?.TypeKind == TypeKind.Class && symbol.IsAbstract;

        public static bool IsSameAssemblyOrHasFriendAccessTo(this IAssemblySymbol assembly, IAssemblySymbol toAssembly)
        {
            return
                SymbolEqualityComparer.Default.Equals(assembly, toAssembly) ||
                (assembly.IsInteractive && toAssembly.IsInteractive) ||
                toAssembly.GivesAccessTo(assembly);
        }

        // Determine if "type" inherits from or implements "baseType", ignoring constructed types, and dealing
        // only with original types.
        public static bool InheritsFromOrImplementsOrEqualsIgnoringConstruction(
            this ITypeSymbol type, ITypeSymbol baseType)
        {
            var originalBaseType = baseType.OriginalDefinition;
            type = type.OriginalDefinition;

            if (SymbolEqualityComparer.Default.Equals(type, originalBaseType))
            {
                return true;
            }

            var baseTypes = (baseType.TypeKind == TypeKind.Interface) ? type.AllInterfaces : type.GetBaseTypes();

            return baseTypes.Any(t =>
                SymbolEqualityComparer.Default.Equals(t.OriginalDefinition.ContainingAssembly, originalBaseType.ContainingAssembly) &&
                t.OriginalDefinition.ToFullName() == originalBaseType.ToFullName());
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }
    }
}
