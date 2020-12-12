﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Avatars
{
    /// <summary>
    /// The context used when running <see cref="IAvatarProcessor"/> 
    /// in the generation pipeline.
    /// </summary>
    public record ProcessorContext
    {
        readonly GeneratorExecutionContext? context;
        readonly NamingConvention naming;

        /// <summary>
        /// Initializes the context from a <see cref="GeneratorExecutionContext"/> provided 
        /// to a source generator, as well as the <see cref="NamingConvention"/> to apply 
        /// to generated types.
        /// </summary>
        /// <param name="context">Context provided to a source generator</param>
        /// <param name="naming">Naming convention to apply to generated types.</param>
        public ProcessorContext(GeneratorExecutionContext context, NamingConvention naming)
    {
        this.context = context;
        this.naming = naming;
        Compilation = context.Compilation;
        ParseOptions = context.ParseOptions;
        CancellationToken = context.CancellationToken;
    }

    internal ProcessorContext(NamingConvention naming, Compilation compilation, ParseOptions parseOptions, CancellationToken cancellationToken = default)
    {
        context = null;
        this.naming = naming;
        Compilation = compilation;
        ParseOptions = parseOptions;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Get the current <see cref="Compilation"/> at the time of execution.
    /// </summary>
    /// <remarks>
    /// This compilation contains only the user supplied code; other generated code is not
    /// available. As user code can depend on the results of generation, it is possible that
    /// this compilation will contain errors.
    /// </remarks>
    public Compilation Compilation { get; init; }

    /// <summary>
    /// Get the <see cref="ParseOptions"/> that will be used to parse any added sources.
    /// </summary>
    public ParseOptions ParseOptions { get; }

    /// <summary>
    /// A set of additional non-code text files that can be used by generators.
    /// </summary>
    public ImmutableArray<AdditionalText> AdditionalFiles
        => context?.AdditionalFiles ?? throw new InvalidOperationException();

    /// <summary>
    /// Allows access to options provided by an analyzer config
    /// </summary>
    public AnalyzerConfigOptionsProvider AnalyzerConfigOptions
        => context?.AnalyzerConfigOptions ?? throw new InvalidOperationException();

    /// <summary>
    /// A <see cref="CancellationToken"/> that can be checked to see if the generation should be cancelled.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// The language being generated.
    /// </summary>
    public string Language => LanguageNames.CSharp; // TODO: VB

    /// <summary>
    /// The naming convention applied for code generation.
    /// </summary>
    public NamingConvention NamingConvention => naming;

    /// <summary>
    /// Gets access to the <see cref="ISyntaxReceiver"/>s registered via 
    /// <see cref="AvatarGenerator.WithSyntaxReceiver"/> that were instantiated and invoked 
    /// in the current generation.
    /// </summary>
    public IEnumerable<ISyntaxReceiver> SyntaxReceivers
        => ((IEnumerable)(context ?? throw new InvalidOperationException()).SyntaxReceiver!).OfType<ISyntaxReceiver>();

    ///// <summary>
    ///// The type arguments specified in the invocation to an 
    ///// <see cref="AvatarGeneratorAttribute"/>-annotated factory method that 
    ///// triggered the source generation process. (i.e. <c>Avatar.Of{T1, ..., Tn}()</c>).
    ///// </summary>
    //public ImmutableArray<INamedTypeSymbol> TypeArguments => types;

    /// <summary>
    /// Adds source code in the form of a <see cref="string"/> to the compilation.
    /// </summary>
    /// <param name="hintName">An identifier that can be used to reference this source text, 
    /// must be unique within this generator</param>
    /// <param name="source">The source code to be add to the compilation</param>
    public void AddSource(string hintName, string source)
        => (context ?? throw new InvalidOperationException()).AddSource(hintName, source);

    /// <summary>
    /// Adds a <see cref="SourceText"/> to the compilation
    /// </summary>
    /// <param name="hintName">An identifier that can be used to reference this source text, 
    /// must be unique within this generator</param>
    /// <param name="sourceText">The <see cref="SourceText"/> to add to the compilation</param>
    public void AddSource(string hintName, SourceText sourceText)
        => (context ?? throw new InvalidOperationException()).AddSource(hintName, sourceText);

    /// <summary>
    /// Adds a <see cref="Diagnostic"/> to the users compilation 
    /// </summary>
    /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
    /// <remarks>
    /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
    /// </remarks>
    public void ReportDiagnostic(Diagnostic diagnostic)
        => (context ?? throw new InvalidOperationException()).ReportDiagnostic(diagnostic);
}
}
