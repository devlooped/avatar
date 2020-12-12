using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Xunit;
using Xunit.Abstractions;

namespace Avatars.AcceptanceTests
{
    public class InternalTests
    {
        ITestOutputHelper output;

        public InternalTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [MemberData(nameof(GetPackageVersions))]
        public void CanAccessRequiredInternalsViaReflection(PackageIdentity package, string targetFramework)
        {
            var projectFile = Path.GetFullPath($"InternalAccess\\test-{package}-{targetFramework}.csproj")!;
            File.WriteAllText(projectFile,
$@"<Project Sdk='Microsoft.NET.Sdk'>
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>{targetFramework}</TargetFramework>
        <OutputPath>bin\{package.Version}</OutputPath>
        <IntermediateOutputPath>obj\{package.Version}</IntermediateOutputPath>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include='{package.Id}' Version='{package.Version}' />
        <PackageReference Include='System.Composition' Version='5.0.0' />
    </ItemGroup>
</Project>");

            var restore = Path.ChangeExtension(projectFile, "-restore.binlog");
            var process = Process.Start(new ProcessStartInfo("dotnet", $"msbuild {projectFile} -t:restore" + (Debugger.IsAttached ? $" -bl:{restore}" : ""))
            {
                CreateNoWindow = !Debugger.IsAttached,
                RedirectStandardError = !Debugger.IsAttached,
                RedirectStandardOutput = !Debugger.IsAttached,
                UseShellExecute = Debugger.IsAttached
            }) ?? throw new InvalidOperationException();

            process.WaitForExit();

#if DEBUG
            if (process.ExitCode != 0)
            {
                if (!Debugger.IsAttached)
                {
                    output.WriteLine(process.StandardError.ReadToEnd());
                    output.WriteLine(process.StandardOutput.ReadToEnd());
                }
                else
                {
                    try { Process.Start(restore); }
                    catch { }
                }
            }
#endif

            Assert.Equal(0, process.ExitCode);

            var build = Path.ChangeExtension(projectFile, "-build.binlog");
            process = Process.Start(new ProcessStartInfo("dotnet", $"msbuild {projectFile} -t:build" + (Debugger.IsAttached ? $" -bl:{build}" : ""))
            {
                CreateNoWindow = !Debugger.IsAttached,
                RedirectStandardError = !Debugger.IsAttached,
                RedirectStandardOutput = !Debugger.IsAttached,
                UseShellExecute = Debugger.IsAttached,
            }) ?? throw new InvalidOperationException();

            process.WaitForExit();

#if DEBUG
            if (process.ExitCode != 0)
            {
                if (!Debugger.IsAttached)
                {
                    output.WriteLine(process.StandardError.ReadToEnd());
                    output.WriteLine(process.StandardOutput.ReadToEnd());
                }
                else
                {
                    try { Process.Start(build); }
                    catch { }
                }
            }
#endif

            Assert.Equal(0, process.ExitCode);

            var info = new ProcessStartInfo(Path.Combine(
                Path.GetDirectoryName(projectFile) ?? "",
                $@"bin\{package.Version}\{targetFramework}",
                Path.ChangeExtension(Path.GetFileName(projectFile), ".exe")), Debugger.IsAttached.ToString())
            {
                CreateNoWindow = !Debugger.IsAttached,
                RedirectStandardError = !Debugger.IsAttached,
                RedirectStandardOutput = !Debugger.IsAttached,
                UseShellExecute = Debugger.IsAttached
            };

            process = Process.Start(info) ?? throw new InvalidOperationException();
            process.WaitForExit();

            if (process.ExitCode != 0 && !Debugger.IsAttached)
            {
                output.WriteLine(process.StandardError.ReadToEnd());
                output.WriteLine(process.StandardOutput.ReadToEnd());
            }

            Assert.Equal(0, process.ExitCode);
        }

        public static IEnumerable<object[]> GetPackageVersions()
        {
            var providers = Repository.Provider.GetCoreV3();
            var source = new PackageSource("https://api.nuget.org/v3/index.json");
            var repo = new SourceRepository(source, providers);
            var resource = repo.GetResourceAsync<PackageMetadataResource>().Result;
            var metadata = resource.GetMetadataAsync("Microsoft.CodeAnalysis", true, false, new Logger(null), CancellationToken.None).Result;

            // 3.8.0 is the first version we support that introduces source generators
            return metadata
                .Select(m => m.Identity)
                .Where(m => m.Version >= new NuGetVersion("3.8.0"))
                .Select(v => new object[] { v, ThisAssembly.Project.TargetFramework });
        }

        class Logger : NuGet.Common.ILogger
        {
            readonly ITestOutputHelper? output;

            public Logger(ITestOutputHelper? output) => this.output = output;

            public void LogDebug(string data) => output?.WriteLine($"DEBUG: {data}");
            public void LogVerbose(string data) => output?.WriteLine($"VERBOSE: {data}");
            public void LogInformation(string data) => output?.WriteLine($"INFORMATION: {data}");
            public void LogMinimal(string data) => output?.WriteLine($"MINIMAL: {data}");
            public void LogWarning(string data) => output?.WriteLine($"WARNING: {data}");
            public void LogError(string data) => output?.WriteLine($"ERROR: {data}");
            public void LogErrorSummary(string data) => output?.WriteLine($"ERROR: {data}");
            public void LogSummary(string data) => output?.WriteLine($"SUMMARY: {data}");
            public void LogInformationSummary(string data) => output?.WriteLine($"SUMMARY: {data}");
        }
    }
}
