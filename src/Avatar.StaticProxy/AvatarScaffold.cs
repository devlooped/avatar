using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avatars.CodeActions;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    /// <summary>
    /// This class encapsulates the interactions with non publicly consumable 
    /// Roslyn features (code fixers and refactorings) that we use to scaffold 
    /// a class just like the IDE would if you auto-implemented all interface 
    /// members and overrode all base class members. This allows us to be 100% 
    /// consistent with what users would expect from manually coded proxies while 
    /// allowing us to avoid having to reimplement it all, since it depends on 
    /// quite a few internal helpers that aren't easy to extract from Roslyn'
    /// source.
    /// </summary>
    class AvatarScaffold : IDisposable
    {
        static string[] refactorings { get; } =
        {
            CodeRefactorings.CSharp.GenerateDefaultConstructorsCodeActionProvider,
        };
        static string[] codeFixes { get; } =
        {
            CodeFixes.CSharp.ImplementAbstractClass,
            CodeFixes.CSharp.ImplementInterface,
            "OverrideAllMembersCodeFix",
        };

        readonly ProcessorContext context;
        readonly AdhocWorkspace workspace;
        readonly Project project;

        public AvatarScaffold(ProcessorContext context)
        {
            this.context = context;

            workspace = new AdhocWorkspace(WorkspaceServices.HostServices);
            var projectId = ProjectId.CreateNewId();
            var projectDir = Path.Combine(Path.GetTempPath(), nameof(AvatarGenerator), projectId.Id.ToString());
            var documents = context.Compilation.SyntaxTrees.Select(tree => DocumentInfo.Create(
                DocumentId.CreateNewId(projectId),
                Path.GetFileName(tree.FilePath),
                filePath: tree.FilePath,
                loader: TextLoader.From(TextAndVersion.Create(tree.GetText(), VersionStamp.Create()))))
                .ToArray();

            var projectInfo = ProjectInfo.Create(
                projectId, VersionStamp.Create(),
                nameof(AvatarGenerator),
                nameof(AvatarGenerator),
                context.Compilation.Language,
                filePath: Path.Combine(projectDir, "code.csproj"),
                compilationOptions: context.Compilation.Options,
                parseOptions: context.ParseOptions,
                metadataReferences: context.Compilation.References,
                documents: documents);

            project = workspace.AddProject(projectInfo);
        }

        public void Dispose() => workspace?.Dispose();

        public async Task<Document> ScaffoldAsync(string name, SyntaxNode syntax)
        {
            var document = project.AddDocument(nameof(AvatarScaffold),
                // NOTE: if we don't force re-parsing in the context of our own 
                // project, nested types can't be resolved, for some reason :/
                syntax.NormalizeWhitespace().ToFullString(),
                folders: context.NamingConvention.RootNamespace.Split('.'),
                filePath: name);

            foreach (var refactoring in refactorings)
                document = await document.ApplyCodeActionAsync(refactoring, cancellationToken: context.CancellationToken);

            foreach (var codeFix in codeFixes)
                document = await document.ApplyCodeFixAsync(codeFix, cancellationToken: context.CancellationToken);

            return document;
        }
    }
}
