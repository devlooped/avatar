using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    /// <summary>
    /// A syntax processor driver that applies the provided set of 
    /// <see cref="ISyntaxProcessor"/> to a <see cref="SyntaxNode"/>.
    /// </summary>
    public class SyntaxProcessorDriver
    {
        // Configured processors, by language, then phase.
        readonly Dictionary<string, Dictionary<ProcessorPhase, ISyntaxProcessor[]>> configuredProcessors;

        public SyntaxProcessorDriver(params ISyntaxProcessor[] processors)
            : this((IEnumerable<ISyntaxProcessor>)processors) { }

        public SyntaxProcessorDriver(IEnumerable<ISyntaxProcessor> processors)
        {
            configuredProcessors = processors
                .GroupBy(processor => processor.Language)
                .ToDictionary(
                    bylang => bylang.Key,
                    bylang => bylang
                        .GroupBy(proclang => proclang.Phase)
                        .ToDictionary(
                            byphase => byphase.Key,
                            byphase => byphase.Select(proclang => proclang).ToArray()));
        }

        /// <summary>
        /// Applies configured syntax processors to the given syntax node.
        /// </summary>
        /// <param name="syntax">The syntax root to apply processors to.</param>
        /// <param name="context">The processor context.</param>
        public SyntaxNode Process(SyntaxNode syntax, ProcessorContext context)
        {
            if (!configuredProcessors.TryGetValue(context.Language, out var supportedProcessors))
                return syntax;

            // For each processor, we pass in an updated context with the received syntax tree added 
            // to the compilation each time. This allows us to have an up-to-date compilation with the 
            // changes from each processor, should semantic information be needed for anything in the 
            // updated syntax trees at any point.

            if (supportedProcessors.TryGetValue(ProcessorPhase.Prepare, out var prepares))
                foreach (var processor in prepares)
                    syntax = processor.Process(syntax, context with { Compilation = context.Compilation.AddSyntaxTrees(syntax.SyntaxTree) });

            if (supportedProcessors.TryGetValue(ProcessorPhase.Scaffold, out var scaffolds))
                foreach (var processor in scaffolds)
                    syntax = processor.Process(syntax, context with { Compilation = context.Compilation.AddSyntaxTrees(syntax.SyntaxTree) });

            if (supportedProcessors.TryGetValue(ProcessorPhase.Rewrite, out var rewriters))
                foreach (var processor in rewriters)
                    syntax = processor.Process(syntax, context with { Compilation = context.Compilation.AddSyntaxTrees(syntax.SyntaxTree) });

            if (supportedProcessors.TryGetValue(ProcessorPhase.Fixup, out var fixups))
                foreach (var processor in fixups)
                    syntax = processor.Process(syntax, context with { Compilation = context.Compilation.AddSyntaxTrees(syntax.SyntaxTree) });

            return syntax;
        }
    }
}
