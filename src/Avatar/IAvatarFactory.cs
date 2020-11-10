using System;
using System.Reflection;

namespace Avatars
{
    /// <summary>
    /// Interface implemented by avatar factories.
    /// </summary>
	public interface IAvatarFactory
	{
        /// <summary>
        /// Creates a avatar with the given parameters.
        /// </summary>
        /// <param name="assembly">Assembly where compile-time generated avatars exist.</param>
        /// <param name="baseType">The base type (or main interface) of the avatar.</param>
        /// <param name="implementedInterfaces">Additional interfaces implemented by the avatar, or an empty array.</param>
        /// <param name="constructorArguments">
        /// Constructor arguments if the <paramref name="baseType" /> is a class, rather than an interface, or an empty array.
        /// </param>
        /// <returns>A avatar that implements <see cref="IAvatar"/> in addition to the specified interfaces (if any).</returns>
		object CreateAvatar(Assembly assembly, Type baseType, Type[] implementedInterfaces, object?[] constructorArguments);
	}
}