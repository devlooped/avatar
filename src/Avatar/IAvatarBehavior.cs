namespace Avatars
{
    /// <summary>
    /// A configured behavior for an <see cref="IAvatar"/>.
    /// </summary>
    public interface IAvatarBehavior
    {
        /// <summary>
        /// Determines whether the behavior applies to the given 
        /// <see cref="IMethodInvocation"/>.
        /// </summary>
        /// <param name="invocation">The invocation to evaluate the 
        /// behavior against.</param>
        bool AppliesTo(IMethodInvocation invocation);

        /// <summary>
        /// Executes the behavior for the given method invocation.
        /// </summary>
        /// <param name="invocation">The current method invocation.</param>
        /// <param name="next">Delegate to invoke the next behavior in the pipeline.</param>
        /// <returns>The result of the method invocation.</returns>
        IMethodReturn Execute(IMethodInvocation invocation, ExecuteHandler next);
    }

    /// <summary>
    /// Handler to invoke the <see cref="IAvatarBehavior.Execute"/> method of a behavior.
    /// </summary>
    /// <param name="invocation">The current method invocation.</param>
    /// <param name="next">Handler for executing the next behavior in the pipeline.</param>
    /// <returns>The result of the method invocation.</returns>
    public delegate IMethodReturn ExecuteHandler(IMethodInvocation invocation, ExecuteHandler next);

    /// <summary>
    /// Implements the <see cref="IAvatarBehavior.AppliesTo"/> method in anonymous behaviors.
    /// </summary>
    public delegate bool AppliesToHandler(IMethodInvocation invocation);
}