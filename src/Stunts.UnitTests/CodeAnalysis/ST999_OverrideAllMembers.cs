﻿using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Stunts.UnitTests
{
    public class ST999_OverrideAllMembers : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer() => new OverridableMembersAnalyzer();

        protected override CodeFixProvider? GetCSharpCodeFixProvider() => new OverrideAllMembersCodeFix();

        [Theory]
        [InlineData(
            ThisAssembly.Constants.CodeAnalysis.ST999.Diagnostic.PublicClass, 
            ThisAssembly.Constants.CodeAnalysis.ST999.Diagnostic.PublicClassFix, 4, 26)]
        public void Verify_Diagnostic(string path, string fix, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = nameof(ThisAssembly.Constants.CodeAnalysis.ST999),
                Message = nameof(OverridableMembersAnalyzer),
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] {
                    new DiagnosticResultLocation("Test0.cs", line, column)
                },
            };

            VerifyCSharpDiagnostic(
                File.ReadAllText(path),
                expected);

            VerifyCSharpFix(
                File.ReadAllText(path),
                File.ReadAllText(fix));
        }
    }
}
