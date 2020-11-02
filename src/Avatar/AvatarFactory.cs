using System;
using System.Reflection;

namespace Avatars
{
    /// <summary>
    /// Allows accessing the default <see cref="IAvatarFactory"/> to use to 
    /// create avatars.
    /// </summary>
    public class AvatarFactory : IAvatarFactory
    {
        static readonly IAvatarFactory nullFactory = new AvatarFactory();

        /// <summary>
        /// Gets or sets the default <see cref="IAvatarFactory"/> to use 
        /// to create avatars. Defaults to the <see cref="NotImplemented"/> factory.
        /// </summary>
        public static IAvatarFactory Default { get; set; } = nullFactory;

        /// <summary>
        /// A factory that throws <see cref="NotImplementedException"/>.
        /// </summary>
        public static IAvatarFactory NotImplemented { get; } = nullFactory;

        private AvatarFactory() { }

        /// <summary>
        /// See <see cref="IAvatarFactory.CreateAvatar(Assembly, Type, Type[], object[])"/>
        /// </summary>
        public object CreateAvatar(Assembly assembly, Type baseType, Type[] implementedInterfaces, object?[] construtorArguments) 
            => throw new NotImplementedException(ThisAssembly.Strings.AvatarFactoryNotImplemented);
    }
}
