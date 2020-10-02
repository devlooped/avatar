using System.ComponentModel;

namespace Stunts
{
    /// <summary>
    /// Usability functions for working with <see cref="IMethodInvocation"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MethodInvocationExtensions
    {
        /// <summary>
        /// Adds a behavior in the pipeline that should be skipped during this invocation 
        /// to the <see cref="IMethodInvocation.SkipBehaviors"/> list.
        /// </summary>
        public static void SkipBehavior<TBehavior>(this IMethodInvocation invocation)
            => invocation.SkipBehaviors.Add(typeof(TBehavior));
    }
}
