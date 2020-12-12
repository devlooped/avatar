using Microsoft.CodeAnalysis;

namespace Avatars
{
    public interface IAvatarProcessor
    {
        /// <summary>
        /// Gets the language supported by the processor.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Gets the phase at which an <see cref="IAvatarProcessor"/> acts.
        /// </summary>
        ProcessorPhase Phase { get; }

        /// <summary>
        /// Processes the syntax root of the code being generated and optionally 
        /// modifies it.
        /// </summary>
        /// <param name="syntax">The syntax root of the source document.</param>
        /// <param name="context">The processor context.</param>
        SyntaxNode Process(SyntaxNode syntax, ProcessorContext context);
    }
}
