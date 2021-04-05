using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Avatars.Processors
{
    class RoslynInternalScaffold : ISyntaxProcessor
    {
        readonly AvatarScaffold scaffold;
        readonly NamingConvention naming;

        public RoslynInternalScaffold(ProcessorContext context, NamingConvention naming)
            => (scaffold, this.naming)
            = (new AvatarScaffold(context), naming);

        public string Language => LanguageNames.CSharp;

        public ProcessorPhase Phase => ProcessorPhase.Scaffold;

        public SyntaxNode Process(SyntaxNode syntax, ProcessorContext context)
        {
            var document = scaffold.ScaffoldAsync(syntax, naming).Result;
            if (document == null)
                return syntax;

            var root = document.GetSyntaxRootAsync(context.CancellationToken).Result;

            return root ?? syntax;
        }
    }
}
