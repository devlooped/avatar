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
        static readonly IStuntFactory nullFactory = new StuntFactory();

        /// <summary>
        /// Gets or sets the default <see cref="IStuntFactory"/> to use 
        /// to create stunts. Defaults to the <see cref="NotImplemented"/> factory.
        /// </summary>
        public static IStuntFactory Default { get; set; } = nullFactory;

        /// <summary>
        /// A factory that throws <see cref="NotImplementedException"/>.
        /// </summary>
        public static IStuntFactory NotImplemented { get; } = nullFactory;

        private StuntFactory() { }

        /// <summary>
        /// See <see cref="IStuntFactory.CreateStunt(Assembly, Type, Type[], object[])"/>
        /// </summary>
        public object CreateStunt(Assembly stuntsAssembly, Type baseType, Type[] implementedInterfaces, object?[] construtorArguments) 
            => throw new NotImplementedException(ThisAssembly.Strings.StuntFactoryNotImplemented);
    }
}
