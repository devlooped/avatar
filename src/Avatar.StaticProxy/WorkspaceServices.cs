using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Avatars.CodeAnalysis;

namespace Avatars
{
    static class WorkspaceServices
    {
        static WorkspaceServices()
        {
            if (Environment.GetEnvironmentVariable("DEBUG_AVATAR") == "1")
                Debugger.Break();

            HostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Concat(new[]
                {
                    // Avatar.dll
                    typeof(IAvatar).Assembly,
                    // Avatar.CodeAnalysis.dll
                    typeof(NamingConvention).Assembly,
                    // Avatar.StaticProxy.Sdk.dll
                    typeof(ICodeFix).Assembly,
                }));
        }

        public static HostServices HostServices { get; }
    }
}
