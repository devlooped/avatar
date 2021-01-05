#pragma warning disable CS0436
using Avatars;
using Xunit;

public interface IGlobalNamespaceInterface
{
    void Run();
}

public class GlobalNamespaceClass
{
    public void Run() { }
}


namespace Scenarios.GlobalNamespace
{
    /// <summary>
    /// Basic interface implementation and behaviors are correct.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            Assert.NotNull(Avatar.Of<IGlobalNamespaceInterface>());
            Assert.NotNull(Avatar.Of<GlobalNamespaceClass>());
        }
    }
}
