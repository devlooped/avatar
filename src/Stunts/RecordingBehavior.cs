using System;
using System.Collections.Generic;
using System.Linq;

namespace Stunts
{
    /// <summary>
    /// Simple behavior that keeps track of all invocations.
    /// </summary>
    public class RecordingBehavior : IStuntBehavior
    {
        /// <summary>
        /// A list of all invocations and their result.
        /// </summary>
        public List<(IMethodInvocation Invocation, IMethodReturn Return)> Invocations { get; } = new();

        /// <summary>
        /// Invocation recording applies to all invocations, so it 
        /// always returns <see langword="true"/>.
        /// </summary>
        public bool AppliesTo(IMethodInvocation invocation) => true;

        /// <summary>
        /// Invokes the next behavior in the pipeline and records 
        /// the invocation and the result.
        /// </summary>
        public IMethodReturn Execute(IMethodInvocation invocation, GetNextBehavior next)
        {
            var result = next().Invoke(invocation, next);
            Invocations.Add((invocation, result));
            return result;
        }

        /// <summary>
        /// Returns the friendly rendering of all invocations performed.
        /// </summary>
        public override string ToString() => string.Join(Environment.NewLine, Invocations.Select(i => i.Return.ToString()));
    }
}
