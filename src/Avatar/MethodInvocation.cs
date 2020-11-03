using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TypeNameFormatter;

namespace Avatars
{
    /// <summary>
    /// Default implementation of <see cref="IMethodInvocation"/>.
    /// </summary>
    public class MethodInvocation : IEquatable<MethodInvocation>, IMethodInvocation
    {
        static readonly object NullArgument = new object();

        /// <summary>
        /// Initializes the <see cref="MethodInvocation"/> with the given parameters.
        /// </summary>
        /// <param name="target">The target object where the invocation is being performed.</param>
        /// <param name="method">The method being invoked, must be a <see cref="MethodInfo"/>. Provided 
        /// overload as a convenience when using <see cref="MethodBase.GetCurrentMethod"/>.</param>
        /// <param name="arguments">The optional arguments passed to the method invocation.</param>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public MethodInvocation(object target, MethodBase method, params object?[] arguments)
            : this(target, method is MethodInfo info ? info :
                  throw new ArgumentException(ThisAssembly.Strings.MethodBaseNotInfo(method.Name)), arguments)
        {
        }

        /// <summary>
        /// Initializes the <see cref="MethodInvocation"/> with the given parameters.
        /// </summary>
        /// <param name="target">The target object where the invocation is being performed.</param>
        /// <param name="method">The method being invoked.</param>
        /// <param name="arguments">The optional arguments passed to the method invocation.</param>
        public MethodInvocation(object target, MethodInfo method, params object?[] arguments)
        {
            // TODO: validate that arguments length and type match the method info?
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Method = method ?? throw new ArgumentNullException(nameof(method));
            Arguments = new ArgumentCollection(arguments, method.GetParameters());
            Context = new Dictionary<string, object>();
        }

        /// <inheritdoc />
        public IArgumentCollection Arguments { get; }

        /// <inheritdoc />
        public IDictionary<string, object> Context { get; }

        /// <inheritdoc />
        public MethodInfo Method { get; }

        /// <inheritdoc />
        public object Target { get; }

        /// <inheritdoc />
        public HashSet<Type> SkipBehaviors { get; } = new HashSet<Type>();

        /// <inheritdoc />
        public IMethodReturn CreateExceptionReturn(Exception exception)
            => new MethodReturn(this, exception);

        /// <inheritdoc />
        public IMethodReturn CreateValueReturn(object? returnValue, params object?[] allArguments)
            => new MethodReturn(this, returnValue, allArguments);

        /// <summary>
        /// Gets a friendly representation of the invocation.
        /// </summary>
        /// <devdoc>
        /// We don't want to optimize code coverage for this since it's a debugger aid only. 
        /// Annotating this method with DebuggerNonUserCode achieves that.
        /// No actual behavior depends on these strings.
        /// </devdoc>
        [ExcludeFromCodeCoverage]
        [DebuggerNonUserCode]
        public override string ToString()
        {
            var result = new StringBuilder();
            if (Method.ReturnType != typeof(void))
                result.AppendFormattedName(Method.ReturnType).Append(" ");
            else
                result.Append("void ");

            var isevent = false;

            if (Method.IsSpecialName)
            {
                if (Method.Name.Equals("get_Item", StringComparison.Ordinal) ||
                    Method.Name.Equals("set_Item", StringComparison.Ordinal))
                {
                    result.Append("this");
                }
                else if (Method.Name.StartsWith("get_", StringComparison.Ordinal))
                {
                    result.Append(Method.Name.Substring(4));
                }
                else if (Method.Name.StartsWith("set_", StringComparison.Ordinal))
                {
                    result.Append(Method.Name.Substring(4));
                    result.Append(" = ").Append(Arguments[0]?.ToString() ?? "null");
                }
                else if (Method.Name.StartsWith("add_", StringComparison.Ordinal))
                {
                    isevent = true;
                    result.Append(Method.Name.Substring(4) + " += ");
                }
                else if (Method.Name.StartsWith("remove_", StringComparison.Ordinal))
                {
                    isevent = true;
                    result.Append(Method.Name.Substring(7) + " -= ");
                }
            }
            else
            {
                result.Append(Method.Name);
            }

            if (Method.IsGenericMethod)
            {
                var generic = Method.GetGenericMethodDefinition();
                result
                    .Append("<")
                    .Append(string.Join(", ", generic.GetGenericArguments().Select(TypeName)))
                    .Append(">");
            }

            // TODO: render indexer arguments?
            if (!Method.IsSpecialName)
            {
                return result
                    .Append("(")
                    .Append(Arguments.ToString())
                    .Append(")")
                    .ToString();
            }
            else if (Method.Name == "get_Item" || Method.Name == "set_Item")
            {
                return result
                    .Append("[")
                    .Append(Arguments.ToString())
                    .Append("]")
                    .ToString();
            }
            else if (isevent)
            {
                return result
                    .Append(((Delegate)Arguments[0]!).GetMethodInfo().Name)
                    .ToString();
            }

            return result.ToString();
        }

        [DebuggerNonUserCode]
        [ExcludeFromCodeCoverage]
        string TypeName(Type type) => type.Name;

        #region Equality

        /// <summary>
        /// Tests the current invocation against another for equality, taking into account the target object 
        /// for reference equality, the object equality of both <see cref="Method"/> and the sequence and 
        /// equality for all <see cref="Arguments"/>.
        /// </summary>
        /// <param name="other">The invocation to compare against.</param>
        /// <returns><see langword="true"/> if the invocations are equal, <see langword="false"/> otherwise.</returns>
        public bool Equals(IMethodInvocation? other)
            => other != null && ReferenceEquals(Target, other.Target) && Method.Equals(other.Method) && Arguments.SequenceEqual(other.Arguments);

        /// <summary>
        /// Tests the current invocation against another for equality, taking into account the target object 
        /// for reference equality, the object equality of both <see cref="Method"/> and the sequence and 
        /// equality for all <see cref="Arguments"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the invocations are equal, <see langword="false"/> otherwise.</returns>
        public bool Equals(MethodInvocation other)
            => Equals((IMethodInvocation)other);

        /// <summary>
        /// Tests the current invocation against another for equality, taking into account the target object 
        /// for reference equality, the object equality of both <see cref="Method"/> and the sequence and 
        /// equality for all <see cref="Arguments"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the invocations are equal, <see langword="false"/> otherwise.</returns>
        public override bool Equals(object obj)
            => Equals(obj as IMethodInvocation);

        /// <summary>
        /// Gets the hash code for the current invocation, including the <see cref="Target"/>, <see cref="Method"/> 
        /// and <see cref="Arguments"/>.
        /// </summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(RuntimeHelpers.GetHashCode(Target));
            hash.Add(Method);
            foreach (var arg in Arguments)
            {
                hash.Add(arg ?? NullArgument);
            }

            return hash.ToHashCode();
        }

        #endregion
    }
}