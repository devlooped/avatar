using System.Collections.Generic;
using System.Reflection;

namespace Avatars
{
    /// <summary>
    /// Represents the arguments of a method invocation.
    /// </summary>
    public interface IArgumentCollection : IReadOnlyList<ParameterInfo>
    {
        /// <summary>
        /// Gets the <see cref="ParameterInfo"/> with the given name.
        /// </summary>
        ParameterInfo this[string name] { get; }

        /// <summary>
        /// Gets the (reference or boxed) value for the argument with the 
        /// given name.
        /// </summary>
        object? GetValue(string name);

        /// <summary>
        /// Gets the (reference or boxed) value for the argument with the 
        /// given index.
        /// </summary>
        object? GetValue(int index);

        /// <summary>
        /// Sets the (reference or boxed) value for the argument with the 
        /// given name.
        /// </summary>
        public void SetValue(string name, object? value);

        /// <summary>
        /// Sets the (reference or boxed) value for the argument with the 
        /// given index.
        /// </summary>
        public void SetValue(int index, object? value);
    }
}
