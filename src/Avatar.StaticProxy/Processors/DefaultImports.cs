﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;

namespace Avatars.Processors
{
    /// <summary>
    /// Adds a set of default imports to a document.
    /// </summary>
    public class DefaultImports : IDocumentProcessor
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
        /// Applies to both <see cref="LanguageNames.CSharp"/> and <see cref="LanguageNames.VisualBasic"/>.
        /// </summary>
        public string[] Languages { get; } = new[] { LanguageNames.CSharp, LanguageNames.VisualBasic };

        /// <summary>
        /// Runs in the first phase of code gen, <see cref="ProcessorPhase.Prepare"/>.
        /// </summary>
        public ProcessorPhase Phase => ProcessorPhase.Prepare;

        /// <summary>
        /// Adds the configured set namespaces to the document.
        /// </summary>
        public async Task<Document> ProcessAsync(Document document, CancellationToken cancellationToken = default)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var root = await document.GetSyntaxRootAsync();
            if (root == null)
                return document;

            var imports = generator.GetNamespaceImports(root).Select(generator.GetName);

            var missing = new HashSet<string>(namespaces);
            missing.ExceptWith(imports);

            if (missing.Count == 0)
                return document;

            return document.WithSyntaxRoot(generator.AddNamespaceImports(root,
                missing.Select(generator.NamespaceImportDeclaration)));
        }
    }
}
