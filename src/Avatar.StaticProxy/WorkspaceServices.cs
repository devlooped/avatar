using System;
using System.Diagnostics;
using System.Linq;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

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
                    // Avatar.StaticProxy.dll
                    typeof(AvatarGenerator).Assembly,
                }));
        }

        public static HostServices HostServices { get; }
    }
}
