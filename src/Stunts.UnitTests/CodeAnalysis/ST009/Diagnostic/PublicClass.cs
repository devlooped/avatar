using System;

namespace Stunts.UnitTests.CodeAnalysis.ST009.Diagnostic
{
    public partial class MyClass
    {
        public MyClass()
        {
            var stunt = Stunt.Of<ContainingType.INested>();

            Console.WriteLine(stunt);
        }
    }

    public class ContainingType
    {
        public interface INested
        {
            void Do();
        }
    }
}
