using System;

namespace Avatars.UnitTests.CodeAnalysis.ST004.Diagnostic
{
    public partial class MultipleEnums
    {
        public MultipleEnums()
        {
            var avatar = Avatar.Of<PlatformID, TypeCode>();

            Console.WriteLine(avatar);
        }
    }
}
