﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Avatars
{
    /// <summary>
    /// Default implementation of <see cref="IMethodReturn"/>.
    /// </summary>
    class MethodReturn : IMethodReturn
    {
        readonly IMethodInvocation invocation;
        readonly object?[] allArguments;

        public MethodReturn(IMethodInvocation invocation, object? returnValue, object?[] allArguments)
        {
            this.invocation = invocation;
            this.allArguments = allArguments;

            ReturnValue = returnValue;

            var outputArgs = new List<object?>();
            var outputInfos = new List<ParameterInfo>();
            var allInfos = invocation.MethodBase.GetParameters();

            for (var i = 0; i < allInfos.Length; i++)
            {
                var info = allInfos[i];
                if (info.ParameterType.IsByRef)
                {
                    outputArgs.Add(allArguments[i]);
                    outputInfos.Add(info);
                }
            }

            Outputs = new ArgumentCollection(outputArgs, outputInfos);
        }

        public MethodReturn(IMethodInvocation invocation, Exception exception)
        {
            this.invocation = invocation;
            allArguments = Array.Empty<object>();
            Outputs = new ArgumentCollection(new object[0], new ParameterInfo[0]);
            Exception = exception;
        }

        /// <summary>
        /// The collection of output parameters. If the method has no output
        /// parameters, this is a zero-length list (never null).
        /// </summary>
        public IArgumentCollection Outputs { get; }

        public object? ReturnValue { get; }

        public Exception? Exception { get; }

        public IDictionary<string, object> Context { get => invocation.Context; }

        /// <summary>
        /// Gets a friendly representation of the object.
        /// </summary>
        /// <devdoc>
        /// We don't want to optimize code coverage for this since it's a debugger aid only. 
        /// Annotating this method with DebuggerNonUserCode achieves that.
        /// No actual behavior depends on these strings.
        /// </devdoc>
        [DebuggerNonUserCode]
        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append(invocation.ToString());

            if (Exception != null)
            {
                result.Append($" => throw new {Exception.GetType().Name}(\"{Exception.Message}\")");
            }
            else if (invocation.MethodBase is MethodInfo r && r.ReturnType != typeof(void))
            {
                result.Append(" => ")
                    .Append(
                        ReturnValue == null ? "null" :
                        r.ReturnType == typeof(string) ? $"\"{ReturnValue}\"" :
                        r.ReturnType == typeof(bool) ? ReturnValue.ToString().ToLowerInvariant() :
                        ReturnValue);
            }

            return result.ToString();
        }
    }
}
