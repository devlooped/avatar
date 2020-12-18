using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;

namespace Avatars
{
    class DynamicAvatarInterceptor : IInterceptor, IAvatar // Implemented to detect breaking changes in Avatar
    {
        static readonly MethodInfo expressionFactory = typeof(DynamicAvatarInterceptor).GetMethod(nameof(CreatePipeline), BindingFlags.Static | BindingFlags.NonPublic);
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

            var input = new MethodInvocation(invocation.Proxy, invocation.Method,
                (m, i) =>
                {
                    try
                    {
                        if (notImplemented)
                            throw new NotImplementedException();

                        invocation.Proceed();
                        var returnValue = invocation.ReturnValue;
                        return m.CreateValueReturn(returnValue, invocation.Arguments);
                    }
                    catch (Exception ex)
                    {
                        return m.CreateExceptionReturn(ex);
                    }
                }, invocation.Arguments);

            var returns = pipeline.Invoke(input);
            var exception = returns.Exception;

            if (exception != null)
                throw exception;

            invocation.ReturnValue = returns.ReturnValue;
            var indexed = input.Arguments.Select((p, i) => (p.Name, i)).ToDictionary(x => x.Name, x => x.i);
            foreach (var prm in returns.Outputs)
            {
                var index = indexed[prm.Name];
                invocation.SetArgumentValue(index, returns.Outputs.GetValue(prm.Name));
            }
        }

        static Expression<Func<BehaviorPipeline>> CreatePipeline<TAvatar>() => () => BehaviorPipelineFactory.Default.CreatePipeline<TAvatar>();
    }
}
