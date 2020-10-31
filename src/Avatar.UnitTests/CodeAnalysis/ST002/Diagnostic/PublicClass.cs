﻿using System;

namespace Avatars.UnitTests.CodeAnalysis.ST002.Diagnostic
{
    public partial class MyClass
    {
        public MyClass()
        {
            var avatar = Avatar.Of<BaseType, OtherBaseType>();

            Console.WriteLine(avatar);
        }
    }

    public class BaseType { }
    public class OtherBaseType { }
}
