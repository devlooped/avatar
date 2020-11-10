using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Avatars
{
    /// <summary>
    /// Encapsulates a list of <see cref="IAvatarBehavior"/>s
    /// and manages calling them in the proper order with the right inputs.
    /// </summary>
    public class BehaviorPipeline
    {
        /// <summary>
        /// Creates a new <see cref="BehaviorPipeline"/> with the given set of 
        /// <see cref="ExecuteDelegate"/> delegates.
        /// </summary>
        /// <param name="behaviors">Behaviors to add to the pipeline.</param>
        public BehaviorPipeline(params ExecuteDelegate[] behaviors)
            : this((IEnumerable<ExecuteDelegate>)behaviors)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BehaviorPipeline"/> with the given set of 
        /// <see cref="ExecuteDelegate"/> delegates.
        /// </summary>
        /// <param name="behaviors">Behaviors to add to the pipeline.</param>
        public BehaviorPipeline(IEnumerable<ExecuteDelegate> behaviors)
            : this(behaviors.Select(behavior => new AnonymousBehavior(behavior)))
        {
        }

        /// <summary>
        /// Creates a new <see cref="BehaviorPipeline"/> with the given set of <see cref="IAvatarBehavior"/>s.
        /// </summary>
        /// <param name="behaviors">Behaviors to add to the pipeline.</param>
        public BehaviorPipeline(params IAvatarBehavior[] behaviors)
            : this((IEnumerable<IAvatarBehavior>)behaviors)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BehaviorPipeline"/> with the given set of <see cref="IAvatarBehavior"/>s.
        /// </summary>
        /// <param name="behaviors">Behaviors to add to the pipeline.</param>
        public BehaviorPipeline(IEnumerable<IAvatarBehavior> behaviors)
            => Behaviors = new BehaviorsCollection(behaviors);

        /// <summary>
        /// Creates a new <see cref="BehaviorPipeline"/>.
        /// </summary>
        public BehaviorPipeline() 
            => Behaviors = new BehaviorsCollection();

        /// <summary>
        /// Gets the collection of behaviors applied to this instance.
        /// </summary>
        /// <remarks>
        /// The behaviors in the pipeline can be modified freely at 
        /// any time, but changes to the list are not visible to in-flight 
        /// invocations. Additionally, the list implements <see cref="INotifyCollectionChanged"/> 
        /// to allow components to monitor changes to the pipeline, as well as <see cref="ISupportInitialize"/> 
        /// to perform batches of changes to the behaviors and avoid excessive collection change
        /// notifications.
        /// </remarks>
        public IList<IAvatarBehavior> Behaviors { get; }

        /// <summary>
        /// Invoke the pipeline with the given input.
        /// </summary>
        /// <param name="invocation">Input to the method call.</param>
        /// <param name="target">The ultimate target of the call.</param>
        /// <param name="throwOnException">Whether to throw the <see cref="IMethodReturn.Exception"/> if it has a value after running 
        /// the behaviors.</param>
        /// <returns>Return value from the pipeline.</returns>
        public IMethodReturn Invoke(IMethodInvocation invocation, ExecuteDelegate target, bool throwOnException = false)
        {
            if (Behaviors.Count == 0)
                return target(invocation, GetNextThrows);

            // We convert to array so that the collection of behaviors can potentially 
            // be modified by behaviors themselves for a subsequent pipeline execution. 
            // The current pipeline execution, once started, cannot be modified, though.
            var behaviors = Behaviors.ToArray();

            var index = -1;
            for (var i = 0; i < behaviors.Length; i++)
            {
                if (!invocation.SkipBehaviors.Contains(behaviors[i].GetType()) && behaviors[i].AppliesTo(invocation))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
                return target(invocation, GetNextThrows);

            var result = behaviors[index].Execute(invocation, () =>
            {
                for (index++; index < behaviors.Length; index++)
                {
                    if (!invocation.SkipBehaviors.Contains(behaviors[index].GetType()) && behaviors[index].AppliesTo(invocation))
                        break;
                }

                return (index < behaviors.Length) ?
                    behaviors[index].Execute :
                    target;
            });

            if (throwOnException && result.Exception != null)
                throw result.Exception;

            return result;
        }

        ExecuteDelegate GetNextThrows() => (m, n)
            => throw new InvalidOperationException(ThisAssembly.Strings.PipelineTargetShouldNeverCallNext);
    }
}
