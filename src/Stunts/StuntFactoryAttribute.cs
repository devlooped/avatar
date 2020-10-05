using System;
using System.ComponentModel;

namespace Stunts
{
    /// <summary>
    /// Assembly-level attribute, typically emitted by nuget packages 
    /// that implement <see cref="IStuntFactory"/> for code generation 
    /// (at build time or run time), which specifies a factory to use 
    /// when creating stunts from <see cref="StuntFactory.Default"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class StuntFactoryAttribute : Attribute
    {
        /// <summary>
        /// Registers a <see cref="IStuntFactory"/> for the assembly.
        /// </summary>
        /// <param name="typeName">The type implementing <see cref="IStuntFactory"/>.</param>
        /// <param name="providerId">The identifier for the provider, usually its nuget package ID.</param>
        public StuntFactoryAttribute(string typeName, string providerId) 
            => (TypeName, ProviderId)
            = (typeName, providerId);

        /// <summary>
        /// Gets the identifier for the provider, usually its nuget package ID.
        /// </summary>
        public string ProviderId { get; }

        /// <summary>
        /// Gets the type name that implements <see cref="IStuntFactory"/>.
        /// </summary>
        public string TypeName { get; }
    }
}
