using System.IO;
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
        [InlineData("CodeAnalysis/ST999/Diagnostic/PublicClass.cs", "CodeAnalysis/ST999/Diagnostic/PublicClass.Fix.cs", 3, 26)]
        public void Verify_Diagnostic(string path, string fix, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = "ST999",
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
