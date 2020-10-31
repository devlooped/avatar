﻿namespace Avatars
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
		IMethodReturn Execute(IMethodInvocation invocation, GetNextBehavior next);
    }

    /// <summary>
    /// Method signature for getting the next behavior in a pipeline.
    /// </summary>
    /// <returns>The delegate to invoke the next behavior in a pipeline.</returns>
	public delegate ExecuteDelegate GetNextBehavior();

    /// <summary>
    /// Method signature for invoking the next behavior in a pipeline.
    /// </summary>
    /// <param name="invocation">The current method invocation.</param>
    /// <param name="next">Delegate to invoke the next behavior in the pipeline.</param>
    /// <returns>The result of the method invocation.</returns>
    public delegate IMethodReturn ExecuteDelegate(IMethodInvocation invocation, GetNextBehavior next);

    /// <summary>
    /// Method signature of <see cref="IAvatarBehavior.AppliesTo"/> for use in 
    /// anonymous behaviors.
    /// </summary>
    public delegate bool AppliesToDelegate(IMethodInvocation invocation);
}