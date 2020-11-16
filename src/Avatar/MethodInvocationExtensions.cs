using System.ComponentModel;

namespace Avatars
{
    /// <summary>
    /// Usability overloads for working with <see cref="IMethodInvocation"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MethodInvocationExtensions
    {
        /// <summary>
        /// Creates the method invocation return that ends the 
        /// current invocation.
        /// </summary>
        /// <param name="invocation">The invocation to create the value return for.</param>
        /// <param name="returnValue">Optional return value from the method invocation. <see langword="null"/> for <see langword="void"/> methods.</param>
        /// <param name="arguments">Ordered list of all arguments to the method invocation, including ref/out arguments.</param>
        /// <returns>The <see cref="IMethodReturn"/> for the current invocation.</returns>
        public static IMethodReturn CreateValueReturn(this IMethodInvocation invocation, object? returnValue, params object?[] arguments)
            => invocation.CreateValueReturn(returnValue, new ArgumentCollection(invocation.Arguments, arguments));

        /// <summary>
        /// Adds a behavior in the pipeline that should be skipped during this invocation 
        /// to the <see cref="IMethodInvocation.SkipBehaviors"/> list.
        /// </summary>
        public static void SkipBehavior<TBehavior>(this IMethodInvocation invocation)
            => invocation.SkipBehaviors.Add(typeof(TBehavior));
    }
}
