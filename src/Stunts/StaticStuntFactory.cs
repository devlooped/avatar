using System;
using System.ComponentModel;
using System.Reflection;

namespace Stunts
{
    /// <summary>
    /// Provides a <see cref="IStuntFactory"/> that creates proxies from types 
    /// generated at compile-time that are included in the received stunt 
    /// assembly in <see cref="CreateStunt"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class StaticStuntFactory : IStuntFactory
    {
        /// <summary>
        /// Uses the <see cref="StuntNaming.GetFullName(Type, Type[])"/> method to 
        /// determine the expected full type name of a compile-time generated stunt 
        /// and tries to locate it from <paramref name="stuntsAssembly"/>.
        /// </summary>
        /// <param name="stuntsAssembly">The assembly containing the compile-time generated stunts.</param>
        /// <param name="baseType">Base type of the stunt.</param>
        /// <param name="implementedInterfaces">Additional interfaces the stunt implements.</param>
        /// <param name="construtorArguments">Optional additional constructor arguments for the stunt.</param>
        public object CreateStunt(Assembly stuntsAssembly, Type baseType, Type[] implementedInterfaces, object?[] construtorArguments)
        {
            var name = StuntNaming.GetFullName(baseType, implementedInterfaces);
            var type = stuntsAssembly.GetType(name, true, false);

            return Activator.CreateInstance(type, construtorArguments);
        }
    }
}
