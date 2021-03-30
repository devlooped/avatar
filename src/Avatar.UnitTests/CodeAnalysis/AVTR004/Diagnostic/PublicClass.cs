using System;

namespace Avatars.UnitTests.CodeAnalysis.AVTR004.Diagnostic
{
    public partial class MyClass
    {
        public MyClass()
        {
            var avatar = Avatar.Of<PlatformID>();

            Console.WriteLine(avatar);
        }
    }
}
