using System;

namespace Avatars.UnitTests.CodeAnalysis.AVTR003.Diagnostic
{
    public partial class MyClass
    {
        public MyClass()
        {
            var avatar = Avatar.Of<BaseType>();

            Console.WriteLine(avatar);
        }
    }

    public sealed class BaseType { }
}
