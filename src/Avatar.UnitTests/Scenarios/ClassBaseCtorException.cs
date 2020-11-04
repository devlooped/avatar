#pragma warning disable CS0436
using System;
using Xunit;

namespace Avatars.Scenarios.ClassBaseCtorException
{
    public class ClassBaseCtor
    {
        public ClassBaseCtor() => throw new IndexOutOfRangeException();
    }

    public class Test : IRunnable
    {
        public void Run() 
            => Assert.Throws<IndexOutOfRangeException>(() => Avatar.Of<ClassBaseCtor>());
    }
}
