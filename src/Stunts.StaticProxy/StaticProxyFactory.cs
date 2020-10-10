using System;
using System.Reflection;

namespace Stunts.Sdk
{
    /// <summary>
    /// Provides a <see cref="IStuntFactory"/> that creates proxies from types 
    /// generated at compile-time.
    /// </summary>
    public class StaticProxyFactory : IStuntFactory
    {
        /// <inheritdoc/>
        public object CreateStunt(Assembly stuntsAssembly, Type baseType, Type[] implementedInterfaces, object[] construtorArguments)
        {
            var name = StuntNaming.GetFullName(baseType, implementedInterfaces);
            var type = stuntsAssembly.GetType(name, true, false);

            return Activator.CreateInstance(type, construtorArguments);
        }
    }
}
