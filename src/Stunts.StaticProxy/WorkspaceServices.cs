using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Stunts.CodeAnalysis;

namespace Stunts
{
    static class WorkspaceServices
    {
        static WorkspaceServices()
        {
            if (Environment.GetEnvironmentVariable("DEBUG_STUNTS") == "1")
                Debugger.Break();

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
