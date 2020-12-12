﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Avatars.Processors
{
    /// <summary>
    /// Adds a set of default imports to a document.
    /// </summary>
    public class DefaultImports : IAvatarProcessor
    {
        // These namespaces are used by the default avatar code and are always imported.
        static readonly string[] DefaultNamespaces =
        {
            typeof(EventArgs).Namespace!,
            typeof(IList<>).Namespace!,
            typeof(MethodBase).Namespace!,
            typeof(CompilerGeneratedAttribute).Namespace!,
            typeof(IAvatar).Namespace!,
        };

        readonly string[] namespaces;

        /// <summary>
        /// Initializes the default imports with the <see cref="DefaultNamespaces"/>.
        /// </summary>
        public DefaultImports() : this(DefaultNamespaces) { }

        /// <summary>
        /// Initializes the default imports with a specific set of namespaces to add.
        /// </summary>
        public DefaultImports(params string[] namespaces) => this.namespaces = namespaces;

        /// <summary>
        /// Applies to <see cref="LanguageNames.CSharp"/>.
        /// </summary>
        public string Language { get; } = LanguageNames.CSharp;

        /// <summary>
        /// Runs in the first phase of code gen, <see cref="ProcessorPhase.Prepare"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Prepare;

        /// <summary>
        /// Adds the configured set namespaces to the document.
        /// </summary>
        public SyntaxNode Process(SyntaxNode syntax, ProcessorContext context)
        {
            if (syntax is not CompilationUnitSyntax unit)
                return syntax;

            var imports = unit.Usings.Select(x => x.Name.ToString());
            var missing = new HashSet<string>(namespaces);
            missing.ExceptWith(imports);

            if (missing.Count == 0)
                return syntax;

            return unit.WithUsings(
                List(
                    unit.Usings.Concat(
                        missing.Select(x => UsingDirective(ParseName(x)))
                    )));
        }
    }
}
