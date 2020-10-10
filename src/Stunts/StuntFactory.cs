using System;
using System.Reflection;

namespace Stunts
{
    /// <summary>
    /// Allows accessing the default <see cref="IStuntFactory"/> to use to 
    /// create stunts.
    /// </summary>
    public class StuntFactory : IStuntFactory
    {
        /// <summary>
        /// Gets or sets the default <see cref="IStuntFactory"/> to use 
        /// to create stunts. Defaults to the <see cref="Null"/> factory.
        /// </summary>
        public static IStuntFactory Default { get; set; } = new StuntFactory();

        /// <summary>
        /// A factory that always returns <see langword="null"/>.
        /// </summary>
        public static IStuntFactory Null { get; } = new StuntFactory();

        private StuntFactory() { }

        /// <summary>
        /// See <see cref="IStuntFactory.CreateStunt(Assembly, Type, Type[], object[])"/>
        /// </summary>
        public object CreateStunt(Assembly stuntsAssembly, Type baseType, Type[] implementedInterfaces, object?[] construtorArguments) 
            => throw new NotImplementedException(ThisAssembly.Strings.StuntFactoryNotImplemented);
    }
}
