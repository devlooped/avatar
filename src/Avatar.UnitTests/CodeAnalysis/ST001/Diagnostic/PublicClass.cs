using System;

namespace Avatars.UnitTests.CodeAnalysis.ST001.Diagnostic
{
    public partial class MyClass
    {
        public MyClass()
        {
            var avatar = Avatar.Of<IDisposable, BaseType>();

            Console.WriteLine(avatar);
        }
    }

    public class BaseType { }
}
