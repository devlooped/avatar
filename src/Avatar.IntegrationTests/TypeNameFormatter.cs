// Copyright (c) 2018 stakx
// License available at https://github.com/stakx/TypeNameFormatter/blob/master/LICENSE.md.

// ReSharper disable All

namespace TypeNameFormatter
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;

    /// <summary>
    ///   Contains the two extension methods
    ///   <see cref="AppendFormattedName(StringBuilder, Type, TypeNameFormatOptions)"/> and
    ///   <see cref="GetFormattedName(Type, TypeNameFormatOptions)"/>.
    /// </summary>
    [GeneratedCode("TypeNameFormatter", "1.0.1")]
    [DebuggerNonUserCode]
    [EditorBrowsable(EditorBrowsableState.Never)]
#if TYPENAMEFORMATTER_INTERNAL
    internal
#else
    public
#endif
    static class TypeName
    {
        private static Dictionary<Type, string> typeKeywords;

        static TypeName()
        {
            typeKeywords = new Dictionary<Type, string>()
            {
                [typeof(bool)] = "bool",
                [typeof(byte)] = "byte",
                [typeof(char)] = "char",
                [typeof(decimal)] = "decimal",
                [typeof(double)] = "double",
                [typeof(float)] = "float",
                [typeof(int)] = "int",
                [typeof(long)] = "long",
                [typeof(object)] = "object",
                [typeof(sbyte)] = "sbyte",
                [typeof(short)] = "short",
                [typeof(string)] = "string",
                [typeof(uint)] = "uint",
                [typeof(ulong)] = "ulong",
                [typeof(ushort)] = "ushort",
                [typeof(void)] = "void",
            };
        }

        /// <summary>
        ///   Appends a string representation of the specified type to this instance.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/> instance to which to append.</param>
        /// <param name="type">The <see cref="Type"/> of which a string representation should be appended.</param>
        /// <param name="options">Any combination of formatting options that should be applied. (Optional.)</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public static StringBuilder AppendFormattedName(this StringBuilder stringBuilder, Type type, TypeNameFormatOptions options = TypeNameFormatOptions.Default)
        {
            stringBuilder.AppendFormattedName(type, options, type);
            return stringBuilder;
        }

        /// <summary>
        ///   Gets a string representation of this instance.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of which a string representation is requested.</param>
        /// <param name="options">Any combination of formatting options that should be applied. (Optional.)</param>
        /// <returns>A string representation of this instance.</returns>
        public static string GetFormattedName(this Type type, TypeNameFormatOptions options = TypeNameFormatOptions.Default)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormattedName(type, options);
            return stringBuilder.ToString();
        }

        private static void AppendFormattedName(this StringBuilder stringBuilder, Type type, TypeNameFormatOptions options, Type typeWithGenericTypeArgs)
        {
            // PHASE 1: Rule out several special cases. (These are mostly "composite" types requiring some recursion.)

            // Types for which there is a keyword:
            if (IsSet(TypeNameFormatOptions.NoKeywords, options) == false && typeKeywords.TryGetValue(type, out string typeKeyword))
            {
                stringBuilder.Append(typeKeyword);
                return;
            }

            // Arrays, by-ref, and pointer types:
            if (type.HasElementType)
            {
                var elementType = type.GetElementType();

                if (type.IsArray)
                {
                    var ranks = new Queue<int>();
                    ranks.Enqueue(type.GetArrayRank());
                    HandleArrayElementType(elementType, ranks);

                    void HandleArrayElementType(Type et, Queue<int> r)
                    {
                        if (et.IsArray)
                        {
                            r.Enqueue(et.GetArrayRank());
                            HandleArrayElementType(et.GetElementType(), r);
                        }
                        else
                        {
                            stringBuilder.AppendFormattedName(et, options);
                            while (r.Count > 0)
                            {
                                stringBuilder.Append('[');
                                stringBuilder.Append(',', r.Dequeue() - 1);
                                stringBuilder.Append(']');
                            }
                        }
                    }
                }
                else if (type.IsByRef)
                {
                    stringBuilder.Append("ref ");
                    stringBuilder.AppendFormattedName(elementType, options);
                }
                else
                {
                    Debug.Assert(type.IsPointer, "Only array, by-ref, and pointer types have an element type.");

                    stringBuilder.AppendFormattedName(elementType, options);
                    stringBuilder.Append('*');
                }

                return;
            }

            var isConstructedGenericType = IsConstructedGenericType(typeWithGenericTypeArgs);

            if (isConstructedGenericType)
            {
                // Nullable value types (excluding the open generic Nullable<T> itself):
                if (IsSet(TypeNameFormatOptions.NoNullableQuestionMark, options) == false)
                {
                    var nullableArg = Nullable.GetUnderlyingType(type);
                    if (nullableArg != null)
                    {
                        stringBuilder.AppendFormattedName(nullableArg, options);
                        stringBuilder.Append('?');
                        return;
                    }
                }

                // Value tuple types (any type called System.ValueTuple`n):
                if (IsSet(TypeNameFormatOptions.NoTuple, options) == false && type.Name.StartsWith("ValueTuple`", StringComparison.Ordinal) && type.Namespace == "System")
                {
                    var genericTypeArgs = GetGenericTypeArguments(typeWithGenericTypeArgs);

                    stringBuilder.Append('(');
                    for (int i = 0, n = genericTypeArgs.Length; i < n; ++i)
                    {
                        if (i > 0)
                        {
                            stringBuilder.Append(", ");
                        }

                        stringBuilder.AppendFormattedName(genericTypeArgs[i], options);
                    }

                    stringBuilder.Append(')');
                    return;
                }
            }

            var name = type.Name;

            if (IsSet(TypeNameFormatOptions.NoAnonymousTypes, options) == false && name.StartsWith("<>f", StringComparison.Ordinal))
            {
                stringBuilder.Append('{');

                int i = 0;
                foreach (var property in GetDeclaredProperties(type))
                {
                    if (i > 0)
                    {
                        stringBuilder.Append(", ");
                    }

                    stringBuilder.AppendFormattedName(property.PropertyType, options)
                                 .Append(' ')
                                 .Append(property.Name);

                    ++i;
                }

                stringBuilder.Append('}');
                return;
            }

            // PHASE 2: Format a (non-"composite") type's name.

            // Possible prefix (namespace or name of enclosing type):
            if (!type.IsGenericParameter)
            {
                if (type.IsNested)
                {
                    stringBuilder.AppendFormattedName(type.DeclaringType, options, typeWithGenericTypeArgs);
                    stringBuilder.Append('.');
                }
                else if (IsSet(TypeNameFormatOptions.Namespaces, options))
                {
                    string @namespace = type.Namespace;
                    if (string.IsNullOrEmpty(@namespace) == false)
                    {
                        stringBuilder.Append(type.Namespace);
                        stringBuilder.Append('.');
                    }
                }
            }

            // Actual type name, optionally followed by a generic parameter/argument list:
            if (isConstructedGenericType || IsGenericType(type))
            {
                var backtickIndex = name.LastIndexOf('`');
                if (backtickIndex >= 0)
                {
                    stringBuilder.Append(name, 0, backtickIndex);
                }
                else
                {
                    stringBuilder.Append(name);
                }

                var ownGenericTypeParamCount = GetGenericTypeArguments(type).Length;

                int ownGenericTypeArgStartIndex = 0;
                if (type.IsNested)
                {
                    var outerTypeGenericTypeParamCount = GetGenericTypeArguments(type.DeclaringType).Length;
                    if (ownGenericTypeParamCount >= outerTypeGenericTypeParamCount)
                    {
                        ownGenericTypeArgStartIndex = outerTypeGenericTypeParamCount;
                    }
                }

                if (ownGenericTypeArgStartIndex < ownGenericTypeParamCount)
                {
                    stringBuilder.Append('<');

                    if (isConstructedGenericType || IsSet(TypeNameFormatOptions.NoGenericParameterNames, options) == false)
                    {
                        var genericTypeArgs = GetGenericTypeArguments(typeWithGenericTypeArgs);

                        for (int i = ownGenericTypeArgStartIndex, n = ownGenericTypeParamCount; i < n; ++i)
                        {
                            if (i > ownGenericTypeArgStartIndex)
                            {
                                stringBuilder.Append(", ");
                            }

                            stringBuilder.AppendFormattedName(genericTypeArgs[i], options);
                        }
                    }
                    else
                    {
                        stringBuilder.Append(',', ownGenericTypeParamCount - ownGenericTypeArgStartIndex - 1);
                    }

                    stringBuilder.Append('>');
                }
            }
            else
            {
                stringBuilder.Append(name);
            }
        }

        /// <remarks>
        ///   Replacement for <see cref="M:System.Enum.HasFlag(System.Enum)"/>
        ///   which may be slow or even unavailable on earlier target frameworks.
        /// </remarks>
        private static bool IsSet(TypeNameFormatOptions option, TypeNameFormatOptions options)
        {
            return (options & option) == option;
        }

        /// <remarks>
        ///   Allows uniform reflection across all target frameworks.
        /// </remarks>
        private static IEnumerable<PropertyInfo> GetDeclaredProperties(Type type)
        {
#if TYPENAMEFORMATTER_USE_SEMIBROKEN_REFLECTION
            return type.GetTypeInfo().DeclaredProperties;
#else
            return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
#endif
        }

        /// <remarks>
        ///   Allows uniform reflection across all target frameworks.
        /// </remarks>
        private static Type[] GetGenericTypeArguments(Type type)
        {
#if TYPENAMEFORMATTER_USE_SEMIBROKEN_REFLECTION
            return type.GenericTypeArguments;
#else
            return type.GetGenericArguments();
#endif
        }

        /// <remarks>
        ///   Allows uniform reflection across all target frameworks.
        /// </remarks>
        private static bool IsGenericType(Type type)
        {
#if TYPENAMEFORMATTER_USE_SEMIBROKEN_REFLECTION
            return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }

        /// <remarks>
        ///   Allows uniform reflection across all target frameworks.
        /// </remarks>
        private static bool IsConstructedGenericType(Type type)
        {
#if TYPENAMEFORMATTER_USE_SEMIBROKEN_REFLECTION
            return type.IsConstructedGenericType;
#else
            return type.IsGenericType && !type.IsGenericTypeDefinition;
#endif
        }
    }

    /// <summary>
    ///   An enumeration of available options when a <see cref="Type"/> name's string representation is requested.
    /// </summary>
    [Flags]
#if TYPENAMEFORMATTER_INTERNAL
    internal
#else
    public
#endif
    enum TypeNameFormatOptions
    {
        /// <summary>
        ///   The default type name formatting options.
        /// </summary>
        Default = 0,

        /// <summary>
        ///   Specifies that a type's namespace should be included.
        ///   <example>
        ///     For example, the type <see cref="Action"/> is formatted as <c>"Action"</c> by default.
        ///     When this flag is specified, it will be formatted as <c>"System.Action"</c>.
        ///   </example>
        /// </summary>
        Namespaces = 1,

        /// <summary>
        ///   Specifies that anonymous types should not be transformed to C#-like syntax.
        ///   <example>
        ///   For example, the anonymous type of <c>"new { Name = "Blob", Count = 17 }"</c> is formatted as
        ///   <c>"{string Name, int Count}"</c> by default. When this flag is specified, it will be formatted as
        ///   the raw "display class" name, which looks something like <c>"&lt;&gt;f__AnonymousType5&lt;string, int&gt;"</c>.
        ///   </example>
        /// </summary>
        NoAnonymousTypes = 2,

        /// <summary>
        ///   Specifies that an open generic type's parameter names should be omitted.
        ///   <example>
        ///     For example, the open generic type <see cref="IEquatable{T}"/> is formatted as <c>"IEquatable&lt;T&gt;"</c> by default.
        ///     When this flag is specified, it will be formatted as <c>"IEquatable&lt;&gt;"</c>.
        ///   </example>
        /// </summary>
        NoGenericParameterNames = 4,

        /// <summary>
        ///   Specifies that primitive types should not be mapped to their corresponding C# language keywords.
        ///   <example>
        ///     For example, the type <see cref="Int32"/> is formatted as <c>"int"</c> by default.
        ///     When this flag is specified, it will be formatted as <c>"Int32"</c>.
        ///   </example>
        /// </summary>
        NoKeywords = 8,

        /// <summary>
        ///   Specifies that nullable types should not be simplified to C# question mark syntax.
        ///   <example>
        ///   For example, the type <see cref="Nullable{T}"/> of <see cref="Int32"/> is formatted as <c>"int?"</c> by default.
        ///   When this flag is specified, it will be formatted as <c>"Nullable&lt;int&gt;"</c>.
        ///   </example>
        /// </summary>
        NoNullableQuestionMark = 16,

        /// <summary>
        ///   Specifies that value tuple types should not be transformed to C# tuple syntax.
        ///   <example>
        ///   For example, the type <see cref="T:System.ValueTuple`2"/> of <see cref="Boolean"/>, <see cref="Int32"/>
        ///   is formatted as <c>"(bool, int)"</c> by default. When this flag is specified,
        ///   it will be formatted as <c>"ValueTuple&lt;bool, int&gt;"</c>.
        ///   </example>
        /// </summary>
        NoTuple = 32,
    }
}
