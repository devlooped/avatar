﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Avatars
{
    /// <summary>
    /// A <see cref="ISyntaxReceiver"/> that can be registered with the <see cref="AvatarGenerator"/> 
    /// via the <see cref="AvatarGenerator.WithSyntaxReceiver(Func{ISyntaxReceiver})"/> to provide 
    /// candidates for avatar generation, where each candidate has at least one <see cref="INamedTypeSymbol"/> 
    /// (interface or base class).
    /// </summary>
    public interface IAvatarCandidatesReceiver : ISyntaxReceiver
    {
        /// <summary>
        /// The candidates for avatar generation, consisting of one <see cref="INamedTypeSymbol"/> 
        /// (interface or base class) plus any extra interfaces to implement for it. 
        /// </summary>
        /// <param name="context">The current generation context, from which the compilation and symbols can be resolved.</param>
        /// <returns>
        /// A tuple of the <see cref="SyntaxNode"/> that caused the nomination of a candidate 
        /// for avatar generation, and the array of <see cref="INamedTypeSymbol"/> symbols in 
        /// use at that location.
        /// </returns>
        IEnumerable<(SyntaxNode source, INamedTypeSymbol[] candidate)> GetCandidates(ProcessorContext context);
    }
}
