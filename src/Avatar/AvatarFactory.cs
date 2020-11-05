using System;
using System.Reflection;
using System.Threading;

namespace Avatars
{
    /// <summary>
    /// Allows accessing the default <see cref="IAvatarFactory"/> to use to 
    /// create avatars.
    /// </summary>
    public class AvatarFactory : IAvatarFactory
    {
        static readonly AsyncLocal<IAvatarFactory?> localFactory = new();
        static readonly IAvatarFactory nullFactory = new AvatarFactory();
        static IAvatarFactory defaultFactory = nullFactory;

        /// <summary>
        /// Gets or sets the default <see cref="IAvatarFactory"/> to use 
        /// to create avatars. Defaults to the <see cref="NotImplemented"/> factory.
        /// </summary>
        /// <remarks>
        /// A <see cref="LocalDefault"/> can override the value of this global 
        /// default, if assigned to a non-null value.
        /// </remarks>
        public static IAvatarFactory Default 
        { 
            get => localFactory.Value ?? defaultFactory; 
            set => defaultFactory = value; 
        }

        /// <summary>
        /// Gets or sets the <see cref="IAvatarFactory"/> to use 
        /// in the current (async) flow, so it does not affect other threads/flows.
        /// </summary>
        public static IAvatarFactory? LocalDefault
        {
            get => localFactory.Value;
            set => localFactory.Value = value;
        }

        /// <summary>
        /// A factory that throws <see cref="NotImplementedException"/>.
        /// </summary>
        public static IAvatarFactory NotImplemented { get; } = nullFactory;

        AvatarFactory() { }

        /// <summary>
        /// See <see cref="IAvatarFactory.CreateAvatar(Assembly, Type, Type[], object[])"/>
        /// </summary>
        public object CreateAvatar(Assembly assembly, Type baseType, Type[] implementedInterfaces, object?[] construtorArguments) 
            => throw new NotImplementedException(ThisAssembly.Strings.AvatarFactoryNotImplemented);
    }
}
