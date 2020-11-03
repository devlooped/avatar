using Avatars;

namespace Scenarios.NestedType
{
    /// <summary>
    /// Nested types are fully supported.
    /// </summary>
    public class Test : IRunnable
    {
        public void Run()
        {
            var avatar = Avatar.Of<IFoo>()
                .AddBehavior(new DefaultValueBehavior());

            avatar.Do();
        }

        public interface IFoo
        {
            void Do();
        }
    }
}
