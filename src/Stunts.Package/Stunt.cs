#nullable disable // So we can keep a single file regardless of the nullability of the calling project.
namespace Stunts
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Instantiates stunts for the specified types.
    /// </summary>
    [CompilerGenerated]
    [ExcludeFromCodeCoverage]
    internal partial class Stunt
    {
        private static T Create<T>(object[] constructorArgs, params Type[] interfaces) => 
            (T)StuntFactory.Default.CreateStunt(typeof(Stunt).GetTypeInfo().Assembly, typeof(T), interfaces, constructorArgs);

        /// <summary>
        /// Creates a stunt that inherits or implements the type <typeparamref name="T"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T>(params object[] constructorArgs) => Create<T>(constructorArgs);

        /// <summary>
        /// Creates a stunt that inherits or implements <typeparamref name="T"/> and 
        /// additionally implements <typeparamref name="T1"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T, T1>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1));

        /// <summary>
        /// Creates a stunt that inherits or implements <typeparamref name="T"/> and 
        /// additionally implements <typeparamref name="T1"/> and <typeparamref name="T2"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T, T1, T2>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1), typeof(T2));

        /// <summary>
        /// Creates a stunt that inherits or implements <typeparamref name="T"/> and 
        /// additionally implements <typeparamref name="T1"/>, <typeparamref name="T2"/> and 
        /// <typeparamref name="T3"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T, T1, T2, T3>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1), typeof(T2), typeof(T3));

        /// <summary>
        /// Creates a stunt that inherits or implements <typeparamref name="T"/> and 
        /// additionally implements <typeparamref name="T1"/>, <typeparamref name="T2"/>, 
        /// <typeparamref name="T3"/> and <typeparamref name="T4"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T, T1, T2, T3, T4>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1), typeof(T2), typeof(T3), typeof(T4));

        /// <summary>
        /// Creates a stunt that inherits or implements <typeparamref name="T"/> and 
        /// additionally implements <typeparamref name="T1"/>, <typeparamref name="T2"/>, 
        /// <typeparamref name="T3"/>, <typeparamref name="T4"/> and <typeparamref name="T5"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T, T1, T2, T3, T4, T5>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

        /// <summary>
        /// Creates a stunt that inherits or implements <typeparamref name="T"/> and 
        /// additionally implements <typeparamref name="T1"/>, <typeparamref name="T2"/>, 
        /// <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/> 
        /// and <typeparamref name="T6"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T, T1, T2, T3, T4, T5, T6>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

        /// <summary>
        /// Creates a stunt that inherits or implements <typeparamref name="T"/> and 
        /// additionally implements <typeparamref name="T1"/>, <typeparamref name="T2"/>, 
        /// <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/> 
        /// <typeparamref name="T6"/> and <typeparamref name="T7"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T, T1, T2, T3, T4, T5, T6, T7>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

        /// <summary>
        /// Creates a stunt that inherits or implements <typeparamref name="T"/> and 
        /// additionally implements <typeparamref name="T1"/>, <typeparamref name="T2"/>, 
        /// <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/> 
        /// <typeparamref name="T6"/>, <typeparamref name="T7"/> and <typeparamref name="T8"/>.
        /// </summary>
        [StuntGenerator]
        public static T Of<T, T1, T2, T3, T4, T5, T6, T7, T8>(params object[] constructorArgs) => Create<T>(constructorArgs, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));
    }
}