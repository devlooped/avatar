﻿using System;

namespace Avatars
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (RoslynInternals.addMemberDeclarationsAsync == null)
            {
                Console.WriteLine($"Null {nameof(RoslynInternals)}.{nameof(RoslynInternals.addMemberDeclarationsAsync)}");
                Console.Error.WriteLine($"Null {nameof(RoslynInternals)}.{nameof(RoslynInternals.addMemberDeclarationsAsync)}");
                return -1;
            }
            if (RoslynInternals.getOverridableMembers == null)
            {
                Console.WriteLine($"Null {nameof(RoslynInternals)}.{nameof(RoslynInternals.getOverridableMembers)}");
                Console.Error.WriteLine($"Null {nameof(RoslynInternals)}.{nameof(RoslynInternals.getOverridableMembers)}");
                return -2;
            }
            if (RoslynInternals.overrideAsync == null)
            {
                Console.WriteLine($"Null {nameof(RoslynInternals)}.{nameof(RoslynInternals.overrideAsync)}");
                Console.Error.WriteLine($"Null {nameof(RoslynInternals)}.{nameof(RoslynInternals.overrideAsync)}");
                return -3;
            }
            if (RoslynInternals.codeGenerationOptions == null)
            {
                Console.WriteLine($"Null {nameof(RoslynInternals)}.{nameof(RoslynInternals.codeGenerationOptions)}");
                Console.Error.WriteLine($"Null {nameof(RoslynInternals)}.{nameof(RoslynInternals.codeGenerationOptions)}");
                return -4;
            }

            return 0;
        }
    }
}
