using System;
using System.ComponentModel;

namespace Avatars
{
    /// <summary>
    /// Provides the <c>Execute</c> usability overloads.
    /// </summary>
    /// <remarks>
    /// All the <c>Execute</c> overloads are set to invoke the pipeline passing 
    /// <see langword="true" /> for the <c>throwOnException</c> to 
    /// <see cref="BehaviorPipeline.Invoke(IMethodInvocation, bool)"/> (or 
    /// <see cref="BehaviorPipeline.Invoke(IMethodInvocation, ExecuteDelegate, bool)"/>).
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class BehaviorPipelineExtensions
    {
        /// <summary>
        /// Since no <see cref="ExecuteDelegate"/> is provided as a target, this 
        /// defaults to throwing a <see cref="NotImplementedException"/> if no 
        /// behavior returns before reaching the target.
        /// </summary>
        /// <devdoc>
        /// This method exists so that the generated code can consistently call the pipeline 
        /// using `Execute` with various overloads, to streamline codegen. It makes all methods 
        /// more consistent.
        /// </devdoc>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IMethodReturn Execute(this BehaviorPipeline pipeline, IMethodInvocation invocation)
            => pipeline.Invoke(invocation, true);

        /// <summary>
        /// Since no <see cref="ExecuteDelegate"/> is provided as a target, and a value is required to 
        /// return, this defaults to throwing a <see cref="NotImplementedException"/> if no 
        /// behavior returns before reaching the target.
        /// </summary>
        public static T? Execute<T>(this BehaviorPipeline pipeline, IMethodInvocation invocation)
            => (T?)pipeline.Invoke(invocation, (input, next) => throw new NotImplementedException(), true).ReturnValue;

        /// <summary>
        /// Executes the pipeline and returns a <see cref="Ref{T}"/> to it. If the returned 
        /// value from the pipeline execution is already a <see cref="Ref{Type}"/>, it's 
        /// returned as-is, otherwise, the <typeparamref name="T"/> value is wrapped in a new
        /// <see cref="Ref{T}"/>.
        /// </summary>
        public static Ref<T> ExecuteRef<T>(this BehaviorPipeline pipeline, IMethodInvocation invocation)
            => pipeline.Invoke(invocation, (input, next) => throw new NotImplementedException(), true).AsRef<T>();

        /// <summary>
        /// Executes the pipeline and returns a <see cref="Ref{T}"/> to it. If the returned 
        /// value from the pipeline execution is already a <see cref="Ref{Type}"/>, it's 
        /// returned as-is, otherwise, the <typeparamref name="T"/> value is wrapped in a new
        /// <see cref="Ref{T}"/>.
        /// </summary>
        public static Ref<T> ExecuteRef<T>(this BehaviorPipeline pipeline, IMethodInvocation invocation, ExecuteDelegate target)
            => pipeline.Invoke(invocation, target, true).AsRef<T>();

        /// <summary>
        /// Since a value is required to return, this executes the pipeline and requests to throw on 
        /// exceptions.
        /// </summary>
        public static T? Execute<T>(this BehaviorPipeline pipeline, IMethodInvocation invocation, ExecuteDelegate target)
            => (T?)pipeline.Invoke(invocation, target, true).ReturnValue;

        /// <summary>
        /// Executes and forces an exception, for void methods.
        /// </summary>
        public static void Execute(this BehaviorPipeline pipeline, IMethodInvocation invocation, ExecuteDelegate target)
            => pipeline.Invoke(invocation, target, true);
    }
}
