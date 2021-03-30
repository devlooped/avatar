using System;

namespace Avatars.UnitTests.CodeAnalysis.AVTR005.Diagnostic
{
    public partial class MyClass
    {
        public MyClass()
        {
            var avatar = Avatar.Of<IPointers>();

            Console.WriteLine(avatar);
        }
    }

    public interface IPointers
    {
        unsafe void Do(int* value);
    }
}
