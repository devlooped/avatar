using System.Runtime.CompilerServices;

namespace Avatars
{
    /// <summary>
    /// A <see cref="IAvatarBehavior"/> that implements an avatar's 
    /// <c>GetHashCode</c> and <c>Equals</c> using <see cref="object.GetHashCode"/> 
    /// and <see cref="object.ReferenceEquals(object, object)"/> respectively.
    /// </summary>
    public class DefaultEqualityBehavior : IAvatarBehavior
    {
        /// <summary>
        /// Always returns <see langword="true" />
        /// </summary>
        public bool AppliesTo(IMethodInvocation invocation)
            => invocation.MethodBase.Name == nameof(GetHashCode) || invocation.MethodBase.Name == nameof(Equals);

        /// <summary>
        /// Implements the <see cref="object.GetHashCode"/> and <see cref="object.Equals(object)"/> 
        /// methods.
        /// </summary>
        public IMethodReturn Execute(IMethodInvocation invocation, GetNextBehavior next)
        {
            if (invocation.MethodBase.Name == nameof(GetHashCode))
                return invocation.CreateValueReturn(RuntimeHelpers.GetHashCode(invocation.Target));
            if (invocation.MethodBase.Name == nameof(Equals))
                return invocation.CreateValueReturn(ReferenceEquals(invocation.Target, invocation.Arguments[0]));

            return next().Invoke(invocation, next);
        }
    }
}