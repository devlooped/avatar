using System;
using System.ComponentModel;
using System.Reflection;

namespace Avatars
{
    /// <summary>
    /// Usability overloads for <see cref="IArgumentCollection"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ArgumentCollectionExtensions
    {
        /// <summary>
        /// Ensures the length of both the arguments and the method or constructor 
        /// arguments matches.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void EnsureSameLength(this IArgumentCollection arguments, MethodBase member)
        {
            if (member is MethodInfo info && info.GetParameters().Length != arguments.Count)
                throw new ArgumentException(ThisAssembly.Strings.MethodArgumentsMismatch(info.Name, info.GetParameters().Length, arguments.Count));

            if (member is MethodInfo ctor && ctor.GetParameters().Length != arguments.Count)
                throw new ArgumentException(ThisAssembly.Strings.MethodArgumentsMismatch(ctor.Name, ctor.GetParameters().Length, arguments.Count));
        }

        /// <summary>
        /// Sets a typed argument on the <paramref name="arguments"/> collection 
        /// with the given <paramref name="name"/>.
        /// </summary>
        public static void Set<T>(this IArgumentCollection arguments, string name, T value)
            => arguments.SetValue(name, value);

        /// <summary>
        /// Sets a typed argument on the <paramref name="arguments"/> collection 
        /// with the given <paramref name="index"/>.
        /// </summary>
        public static void Set<T>(this IArgumentCollection arguments, int index, T value)
            => arguments.SetValue(index, value);

        /// <summary>
        /// Gets a typed argument from the <paramref name="arguments"/> collection 
        /// with the given <paramref name="name"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value in the collection was <see langword="null"/>.</exception>
        public static T Get<T>(this IArgumentCollection arguments, string name) where T : notnull
            => Get<T>("'" + name + "'", arguments.GetValue(name));

        /// <summary>
        /// Gets a typed argument from the <paramref name="arguments"/> collection 
        /// with the given <paramref name="index"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value in the collection was <see langword="null"/>.</exception>
        public static T Get<T>(this IArgumentCollection arguments, int index) where T : notnull
            => Get<T>(index.ToString(), arguments.GetValue(index));

        /// <summary>
        /// Gets an optional (nullable) typed argument from the <paramref name="arguments"/> collection 
        /// with the given <paramref name="name"/>.
        /// </summary>
        public static T? GetNullable<T>(this IArgumentCollection arguments, string name)
            => GetNullable<T>("'" + name + "'", arguments.GetValue(name));

        /// <summary>
        /// Gets an optional (nullable) typed argument from the <paramref name="arguments"/> collection 
        /// with the given <paramref name="index"/>.
        /// </summary>
        public static T? GetNullable<T>(this IArgumentCollection arguments, int index)
            => GetNullable<T>(index.ToString(), arguments.GetValue(index));

        static T Get<T>(string argument, object? value) where T : notnull
        {
            var type = typeof(T);
            if (value == null)
            {
                // Nullable<T> can handle a null return, and in that case 
                // the caller explicitly opted-in to nulls. This could only 
                // happen if the caller does not have nullable turned on.
                if (type.IsValueType &&
                    type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return default(T)!;
                else if (type.IsValueType)
                    throw new ArgumentNullException(ThisAssembly.Strings.ValueTypeIsNull(argument, type), argument);

                throw new ArgumentNullException(argument, ThisAssembly.Strings.ValueIsNull(argument));
            }

            if (type.IsAssignableFrom(value.GetType()))
                return (T)value;

            throw new ArgumentException(ThisAssembly.Strings.ValueNotCompatible(argument, value.GetType(), type));
        }

        static T? GetNullable<T>(string argument, object? value)
        {
            var type = typeof(T);
            if (value == null)
            {
                // non-value type or Nullable<T> can handle a null return.
                if (!type.IsValueType ||
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return default;

                throw new ArgumentException(ThisAssembly.Strings.ValueTypeIsNull(argument, type), argument);
            }

            if (type.IsAssignableFrom(value.GetType()))
                return (T)value;

            throw new ArgumentException(ThisAssembly.Strings.ValueNotCompatible(argument, value.GetType(), type));
        }
    }
}
