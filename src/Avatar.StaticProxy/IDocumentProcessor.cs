using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    /// <summary>
    /// Interface implemented by components that participate in avatar code generation.
    /// </summary>
    public interface IDocumentProcessor
    {
        /// <summary>
        /// Gets the languages the document processor supports.
        /// </summary>
        string[] Languages { get; }

        /// <summary>
        /// Gets the phase at which an <see cref="IDocumentProcessor"/> acts.
        /// </summary>
        ProcessorPhase Phase { get; }

        /// <summary>
        /// Processes the avatar document and optionally modifies its source code.
        /// </summary>
        Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default);
    }
}