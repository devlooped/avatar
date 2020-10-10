using System;

namespace Stunts.UnitTests.CodeAnalysis.ST003.Diagnostic
{
    public partial class MyClass
    {
        public MyClass()
        {
            var stunt = Stunt.Of<IDisposable, BaseType>();

            Console.WriteLine(stunt);
        }
    }

    public class BaseType { }
}
