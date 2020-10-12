﻿#if NET472
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Stunts.UnitTests
{
    internal static class MSBuildInitializer
    {
        static readonly string logFile = Environment.ExpandEnvironmentVariables(@"%TEMP%\Stunts.txt");

        [ModuleInitializer]
        internal static void Run()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

#if DEBUG
            File.AppendAllText(logFile, $"Initializing MSBuild to {ThisAssembly.Project.MSBuildBinPath}\r\n");
#endif

            var binPath = ThisAssembly.Project.MSBuildBinPath;
            Microsoft.Build.Locator.MSBuildLocator.RegisterMSBuildPath(binPath);
            // Set environment variables so SDKs can be resolved. 
            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", Path.Combine(binPath, "MSBuild.exe"), EnvironmentVariableTarget.Process);
        }

        static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            if (name == null)
                return null;

            var file = Path.Combine(ThisAssembly.Project.MSBuildBinPath, name + ".dll");

#if DEBUG
            File.AppendAllText(logFile, $"Resolving {name}\r\n");
#endif

            if (name.StartsWith("Microsoft.Build") && File.Exists(file))
            {
#if DEBUG
                File.AppendAllText(logFile, $"Found {file}\r\n");
#endif
                return Assembly.LoadFrom(file);
            }

            return null;
        }
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleInitializerAttribute : Attribute { }
}

#endif