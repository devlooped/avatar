using System;
using System.ComponentModel;
using System.Reflection;

namespace Avatars
{
    /// <summary>
    /// Provides a <see cref="IAvatarFactory"/> that creates proxies from types 
    /// generated at compile-time that are included in the received avatar 
    /// assembly in <see cref="CreateAvatar"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class StaticAvatarFactory : IAvatarFactory
    {
        /// <summary>
        /// Uses the <see cref="AvatarNaming.GetFullName(Type, Type[])"/> method to 
        /// determine the expected full type name of a compile-time generated avatar 
        /// and tries to locate it from <paramref name="stuntsAssembly"/>.
        /// </summary>
        /// <param name="stuntsAssembly">The assembly containing the compile-time generated avatars.</param>
        /// <param name="baseType">Base type of the avatar.</param>
        /// <param name="implementedInterfaces">Additional interfaces the avatar implements.</param>
        /// <param name="constructorArguments">Optional additional constructor arguments for the avatar.</param>
        public object CreateAvatar(Assembly stuntsAssembly, Type baseType, Type[] implementedInterfaces, object?[] constructorArguments)
        {
            var name = AvatarNaming.GetFullName(baseType, implementedInterfaces);
            var type = stuntsAssembly.GetType(name, true, false);

            return Activator.CreateInstance(type, constructorArguments);
        }
    }
}
