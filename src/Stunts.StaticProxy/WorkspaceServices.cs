using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Stunts.CodeAnalysis;

namespace Stunts
{
    static class WorkspaceServices
    {
        static WorkspaceServices()
        {
            HostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Concat(new[]
                {
                    // Stunts.dll
                    typeof(IStunt).Assembly,
                    // Stunts.CodeAnalysis.dll
                    typeof(NamingConvention).Assembly,
                    // Stunts.StaticProxy.Sdk.dll
                    typeof(ICodeFix).Assembly,
                }));
        }

        public static HostServices HostServices { get; }
    }
}
