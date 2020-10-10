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
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

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

        static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (name == null)
                return null;

            var file = Path.Combine(
                Path.GetDirectoryName(typeof(StuntSourceGenerator).Assembly.Location) ?? "",
                name + ".dll");

            if (File.Exists(file))
                return Assembly.LoadFrom(file);

            return null;
        }

        public static HostServices HostServices { get; }
    }
}
