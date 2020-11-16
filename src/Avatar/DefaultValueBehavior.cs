using System;
using System.Linq;
using System.Reflection;

namespace Avatars
{
    /// <summary>
    /// A <see cref="IAvatarBehavior"/> that returns default values from an 
    /// invocation, both for the method return type as well as any out/ref 
    /// parameters.
    /// </summary>
    public class DefaultValueBehavior : IAvatarBehavior
    {
        /// <summary>
        /// Initializes the behavior with a default <see cref="DefaultValueProvider"/>.
        /// </summary>
        public DefaultValueBehavior()
            : this(new DefaultValueProvider()) { }

        /// <summary>
        /// Initializes the behavior with a specific <see cref="DefaultValueProvider"/>.
        /// </summary>
        public DefaultValueBehavior(DefaultValueProvider provider) => Provider = provider;

        /// <summary>
        /// Gets or sets the provider of default values for the behavior.
        /// </summary>
        public DefaultValueProvider Provider { get; set; }

        /// <summary>
        /// Always returns <see langword="true" />
        /// </summary>
        public bool AppliesTo(IMethodInvocation invocation) => true;

        /// <summary>
        /// Fills in the ref, out and return values with the defaults determined 
        /// by the <see cref="DefaultValueProvider"/> utility class.
        /// </summary>
        IMethodReturn IAvatarBehavior.Execute(IMethodInvocation invocation, GetNextBehavior next)
        {
            var arguments = new ArgumentCollection(invocation.Arguments);
            foreach (var parameter in arguments)
            {
                // Only provide default values for out parameters. 
                // NOTE: does not touch ByRef values.
                if (parameter.IsOut)
                    arguments.SetValue(parameter.Name, Provider.GetDefault(parameter.ParameterType));
                else
                    arguments.SetValue(parameter.Name, invocation.Arguments.GetValue(parameter.Name));
            }

            var returnValue = default(object);
            if (invocation.MethodBase is MethodInfo info &&
                info.ReturnType != typeof(void))
            {
                returnValue = Provider.GetDefault(info.ReturnType);
            }

            return invocation.CreateValueReturn(returnValue, arguments);
        }
    }
}