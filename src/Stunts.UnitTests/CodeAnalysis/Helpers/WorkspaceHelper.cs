using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Stunts;
using Stunts.CodeAnalysis;
using Xunit;

public static class WorkspaceHelper
{
    public static (AdhocWorkspace workspace, Project project) CreateWorkspaceAndProject(
        string language, string assemblyName = "Code", bool includeStuntApi = true, bool includeMockApi = false)
    {
        var workspace = new AdhocWorkspace(MefHostServices.Create(
            MefHostServices.DefaultAssemblies.Concat(new[]
            {
                typeof(IStunt).Assembly,
                typeof(NamingConvention).Assembly,
                typeof(Superpower.Parse).Assembly,
                typeof(ICodeFix).Assembly,
            })));
        var projectInfo = CreateProjectInfo(language, assemblyName, includeStuntApi, includeMockApi);
        var project = workspace.AddProject(projectInfo);

        return (workspace, project);
    }

    public static ProjectInfo CreateProjectInfo(string language, string assemblyName, bool includeStuntApi = true, bool includeMockApi = false)
    {
        var suffix = language == LanguageNames.CSharp ? "CS" : "VB";
        var options = language == LanguageNames.CSharp ?
                (CompilationOptions)new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default) :
                (CompilationOptions)new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optionStrict: OptionStrict.On, assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default);
        var parse = language == LanguageNames.CSharp ?
                (ParseOptions)new CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest) :
                (ParseOptions)new VisualBasicParseOptions(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.Latest);

        var referencePaths =
#pragma warning disable CS0436 // Type conflicts with imported type
            ThisAssembly.Project.BuildReferencePaths.Split('|')
#pragma warning restore CS0436 // Type conflicts with imported type
            .Where(path => !string.IsNullOrEmpty(path) && File.Exists(path))
            .Distinct(FileNameEqualityComparer.Default);

        var projectId = ProjectId.CreateNewId();
        var documents = new List<DocumentInfo>();

        var projectDir = Path.Combine(Path.GetTempPath(), "Test", projectId.Id.ToString());

        return ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            assemblyName + "." + suffix,
            assemblyName + "." + suffix,
            language,
            filePath: language == LanguageNames.CSharp
                ? Path.Combine(projectDir, "code.csproj")
                : Path.Combine(projectDir, "code.vbproj"),
            compilationOptions: options,
            parseOptions: parse,
            metadataReferences: referencePaths
                .Select(path => MetadataReference.CreateFromFile(path)),
            documents: documents.ToArray());
    }

    public static CancellationToken TimeoutToken(int seconds)
        => Debugger.IsAttached ?
            CancellationToken.None :
            new CancellationTokenSource(TimeSpan.FromSeconds(seconds)).Token;

    public static Document AddDocument(this AdhocWorkspace workspace, Project project, string content, string fileName = "code.cs")
        => workspace.AddDocument(DocumentInfo.Create(
            DocumentId.CreateNewId(project.Id),
            "code.cs",
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(content), VersionStamp.Create()))));

    public static Assembly Emit(this Compilation compilation)
    {
        using (var stream = new MemoryStream())
        {
            var result = compilation.Emit(stream);
            result.AssertSuccess();

            stream.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(stream.ToArray());
        }
    }

    public static void AssertSuccess(this EmitResult result)
    {
        if (!result.Success)
        {
            Assert.False(true,
                "Emit failed:\r\n" +
                Environment.NewLine +
                string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.ToString())));
        }
    }
}