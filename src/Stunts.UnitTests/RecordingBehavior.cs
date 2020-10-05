using System;
using System.Collections.Generic;
using System.Linq;
using Stunts;

public class RecordingBehavior : IStuntBehavior
{
    public List<object> Invocations { get; } = new();

    public bool AppliesTo(IMethodInvocation invocation) => true;

    public IMethodReturn Execute(IMethodInvocation invocation, GetNextBehavior next)
    {
        var result = next().Invoke(invocation, next);
        if (result != null)
            Invocations.Add(result);
        else
            Invocations.Add(invocation);

        return result!;
    }

    public override string ToString() => string.Join(Environment.NewLine, Invocations.Select(i => i.ToString()));
}