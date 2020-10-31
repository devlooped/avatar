using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Avatars
{
    internal static class RoslynInternals
    {
        #region GetExports

        static readonly ConcurrentDictionary<Tuple<Type, Type, Type>, Delegate> getExportsCache = new ConcurrentDictionary<Tuple<Type, Type, Type>, Delegate>();

        /// <summary>
        /// Gets the exports and their metadata.
        /// </summary>
        /// <typeparam name="TExtension">The type of MEF extension to retrieve.</typeparam>
        /// <typeparam name="TMetadata">The metadata of the exported MEF extension.</typeparam>
        public static IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>(this HostServices services)
        {
            var getExports = getExportsCache.GetOrAdd(Tuple.Create(services.GetType(), typeof(TExtension), typeof(TMetadata)),
                _ =>
                {
                    var method = services.GetType()
                        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(m => m.Name.EndsWith("GetExports") &&
                                    m.IsGenericMethodDefinition &&
                                    m.GetGenericArguments().Length == 2)
                        .FirstOrDefault();

                    Func<IEnumerable<Lazy<TExtension, TMetadata>>> func;

                    if (method == null)
                    {
                        func = () => Enumerable.Empty<Lazy<TExtension, TMetadata>>();
                    }
                    else
                    {
                        var generic = method.MakeGenericMethod(typeof(TExtension), typeof(TMetadata));
                        func = () => (IEnumerable<Lazy<TExtension, TMetadata>>)generic.Invoke(services, null)!;
                    }

                    return func;
                });

            return ((Func<IEnumerable<Lazy<TExtension, TMetadata>>>)getExports).Invoke();
        }

        #endregion

        #region GetOverridableMembers

        internal static readonly MethodInfo? getOverridableMembers = typeof(Workspace).Assembly
            .GetType("Microsoft.CodeAnalysis.Shared.Extensions.INamedTypeSymbolExtensions", false)
            ?.GetMethod("GetOverridableMembers", BindingFlags.Public | BindingFlags.Static);

        public static ImmutableArray<ISymbol> GetOverridableMembers(INamedTypeSymbol containingType, CancellationToken cancellationToken)
            => (ImmutableArray<ISymbol>)(getOverridableMembers ?? throw NotSupported()).Invoke(
                null, new object[] { containingType, cancellationToken })!;

        #endregion

        #region OverrideAsync

        internal static readonly MethodInfo? overrideAsync = typeof(Workspace).Assembly
            .GetType("Microsoft.CodeAnalysis.Shared.Extensions.SyntaxGeneratorExtensions", false)
            ?.GetMethod("OverrideAsync", BindingFlags.Public | BindingFlags.Static);

        public static Task<ISymbol> OverrideAsync(SyntaxGenerator generator, ISymbol symbol, INamedTypeSymbol containingType, Document document, DeclarationModifiers? modifiersOpt = null, CancellationToken cancellationToken = default)
            => (Task<ISymbol>)(overrideAsync ?? throw NotSupported()).Invoke(
                null, new object?[] { generator, symbol, containingType, document, modifiersOpt, cancellationToken })!;

        #endregion

        #region AddMemberDeclarationsAsync

        internal static readonly ConstructorInfo? codeGenerationOptions = typeof(Workspace).Assembly
            .GetType("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationOptions", false)
            ?.GetConstructors().OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();

        internal static readonly MethodInfo? addMemberDeclarationsAsync = typeof(Workspace).Assembly
            .GetType("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerator", false)
            ?.GetMethod("AddMemberDeclarationsAsync", BindingFlags.Public | BindingFlags.Static);

        public static Task<Document> AddMemberDeclarationsAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CancellationToken cancellationToken = default)
            => (Task<Document>)(addMemberDeclarationsAsync ?? throw NotSupported()).Invoke(
                null, new object?[] 
                { 
                    solution, destination, members, 
                    GetOptions(solution.Workspace.Options), 
                    cancellationToken 
                })!;

        static object? GetOptions(OptionSet options)
        {
            if (codeGenerationOptions == null)
                throw NotSupported();

            var parameters = codeGenerationOptions.GetParameters()
                .Select(x => x.ParameterType == typeof(OptionSet) ? options : Type.Missing)
                .ToArray();

            return codeGenerationOptions.Invoke(parameters);
        }

        #endregion

        static Exception NotSupported() => new NotSupportedException(
            $"Version {typeof(Workspace).Assembly.GetName().Version?.ToString(3)} of the Roslyn assemblies do not support our code generation. Please report at https://github.com/kzu/avatar/issues.");
    }
}
