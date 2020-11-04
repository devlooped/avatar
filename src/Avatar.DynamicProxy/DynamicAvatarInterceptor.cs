using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;

namespace Avatars
{
    internal class DynamicAvatarInterceptor : IInterceptor, IAvatar // Implemented to detect breaking changes in Avatar
    {
        static readonly MethodInfo expressionFactory = typeof(DynamicAvatarInterceptor).GetMethod("CreatePipeline", BindingFlags.Static | BindingFlags.NonPublic);
        static readonly ConcurrentDictionary<Type, Func<BehaviorPipeline>> createPipelineFactories = new();

        readonly bool notImplemented;
        BehaviorPipeline? pipeline;

        internal DynamicAvatarInterceptor(bool notImplemented) => this.notImplemented = notImplemented;

        public IList<IAvatarBehavior> Behaviors => pipeline!.Behaviors;

        public virtual void Intercept(IInvocation invocation)
        {
            if (pipeline == null)
                pipeline = createPipelineFactories.GetOrAdd(invocation.Proxy.GetType(), type =>
                {
                    var expression = (Expression<Func<BehaviorPipeline>>)expressionFactory
                        .MakeGenericMethod(type)
                        .Invoke(null, null);

                    return expression.Compile();
                }).Invoke();

            if (invocation.Method.DeclaringType == typeof(IAvatar))
            {
                invocation.ReturnValue = Behaviors;
                return;
            }

            var input = new MethodInvocation(invocation.Proxy, invocation.Method, invocation.Arguments);
            var returns = pipeline.Invoke(input, (i, next) =>
            {
                try
                {
                    if (notImplemented)
                        throw new NotImplementedException();

                    invocation.Proceed();
                    var returnValue = invocation.ReturnValue;
                    return input.CreateValueReturn(returnValue, invocation.Arguments);
                }
                catch (Exception ex)
                {
                    return input.CreateExceptionReturn(ex);
                }
            });

            var exception = returns.Exception;
            if (exception != null)
                throw exception;

            invocation.ReturnValue = returns.ReturnValue;
            for (var i = 0; i < returns.Outputs.Count; i++)
            {
                var name = returns.Outputs.GetInfo(i).Name;
                var index = input.Arguments.IndexOf(name);
                invocation.SetArgumentValue(index, returns.Outputs[i]);
            }
        }

        static Expression<Func<BehaviorPipeline>> CreatePipeline<TAvatar>() => () => BehaviorPipelineFactory.Default.CreatePipeline<TAvatar>();
    }
}
