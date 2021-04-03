using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
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
        readonly ITestOutputHelper output;

        public InternalTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [MemberData(nameof(GetPackageVersions))]
        public void CanAccessRequiredInternalsViaReflection(PackageIdentity package, string targetFramework)
        {
            Assert.True(FindDotNet(out var dotnet), dotnet ?? "Could not find dotnet");
            output.WriteLine($"Located dotnet at '{dotnet}'");

            var projectFile = Path.GetFullPath(Path.Combine("InternalAccess", package.Version.Version.ToString(3), targetFramework, $"test-{package}.csproj"))!;
            var outputType = targetFramework == "net472" ? "Exe" : "Library";
            Directory.CreateDirectory(Path.GetDirectoryName(projectFile));

            File.WriteAllText(projectFile,
$@"<Project Sdk='Microsoft.NET.Sdk'>
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>{targetFramework}</TargetFramework>
        <UseAppHost>false</UseAppHost>
        <OutputPath>bin</OutputPath>
        <IntermediateOutputPath>obj</IntermediateOutputPath>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include='{package.Id}' Version='{package.Version}' />
        <PackageReference Include='System.Composition' Version='5.0.0' />
    </ItemGroup>

    <ItemGroup>
        <Compile Include='..\..\*.cs' />
    </ItemGroup>
</Project>");

            output.WriteLine($"Writing temp project to '{projectFile}'");

            var outDir = Path.Combine(Path.Combine(
                Path.GetDirectoryName(projectFile) ?? "", "pub"));

            var binlog = Path.ChangeExtension(projectFile, ".binlog");
            var process = Process.Start(new ProcessStartInfo(dotnet, $"publish {projectFile} -o {outDir} -bl:{binlog} --self-contained false")
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
                    try { Process.Start(binlog); }
                    catch { }
                }
            }
#endif

            Assert.Equal(0, process.ExitCode);

            var executable = Path.Combine(outDir, Path.ChangeExtension(Path.GetFileName(projectFile),
                    // .exe for net472, .dll otherwise
                    targetFramework == "net472" ? ".exe" : ".dll"));

            Assert.True(File.Exists(executable), "Did not find expected executable at: " + executable);

            var info = targetFramework == "net472" ?
                new ProcessStartInfo(executable, Debugger.IsAttached.ToString()) :
                new ProcessStartInfo(dotnet, executable + " " + Debugger.IsAttached.ToString());

            info.CreateNoWindow = !Debugger.IsAttached;
            info.RedirectStandardError = !Debugger.IsAttached;
            info.RedirectStandardOutput = !Debugger.IsAttached;
            info.UseShellExecute = Debugger.IsAttached;

            process = Process.Start(info) ?? throw new InvalidOperationException();

            process.WaitForExit();
            if (process.ExitCode != 0 && !Debugger.IsAttached)
            {
                output.WriteLine(process.StandardError.ReadToEnd());
                output.WriteLine(process.StandardOutput.ReadToEnd());
            }

            Assert.Equal(0, process.ExitCode);
        }

        static bool FindDotNet(out string? output)
        {
            output = null;
            var fileName = "dotnet";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                fileName += ".exe";

            var mainModule = Process.GetCurrentProcess().MainModule;
            if (!string.IsNullOrEmpty(mainModule?.FileName)
                && Path.GetFileName(mainModule.FileName).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                output = mainModule.FileName;
                return true;
            }

            // Fallback to running where/which
            output = Process.Start(new ProcessStartInfo(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which", fileName)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            })?.StandardOutput.ReadToEnd()
               .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
               .Select(line => line.Trim())
               .FirstOrDefault();

            if (!File.Exists(output))
                output = null;

            return output != null;
        }

        public static IEnumerable<object[]> GetPackageVersions()
        {
            var providers = Repository.Provider.GetCoreV3();
            var source = new PackageSource("https://api.nuget.org/v3/index.json");
            var repo = new SourceRepository(source, providers);
            var resource = repo.GetResourceAsync<PackageMetadataResource>().Result;
            var metadata = resource.GetMetadataAsync("Microsoft.CodeAnalysis", true, false, new SourceCacheContext(), new Logger(null), CancellationToken.None).Result;

            // 3.8.0 is the first version we support that introduces source generators
            return metadata
                .Select(x => x.Identity)
                .Where(x => x.Version >= new NuGetVersion("3.8.0"))
                .OrderByDescending(x => x.Version)
                .GroupBy(x => x.Version.Version)
#pragma warning disable CS0436 // Type conflicts with imported type
                .Select(v => new object[] { v.First(), ThisAssembly.Project.TargetFramework });
#pragma warning restore CS0436 // Type conflicts with imported type
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
            public void Log(LogLevel level, string data) => output?.WriteLine($"{level.ToString().ToUpperInvariant()}: {data}");
            public Task LogAsync(LogLevel level, string data)
            {
                output?.WriteLine($"{level.ToString().ToUpperInvariant()}: {data}");
                return Task.CompletedTask;
            }
            public void Log(ILogMessage message) => output?.WriteLine($"{message.Level.ToString().ToUpperInvariant()}: {message.Message}");
            public Task LogAsync(ILogMessage message)
            {
                output?.WriteLine($"{message.Level.ToString().ToUpperInvariant()}: {message.Message}");
                return Task.CompletedTask;
            }
        }
    }
}
