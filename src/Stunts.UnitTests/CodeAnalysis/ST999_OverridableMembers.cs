using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Stunts.UnitTests
{
    public class ST999_OverridableMembers : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer() => new OverridableMembersAnalyzer();

        [Theory]
        [InlineData("CodeAnalysis/ST999/Diagnostic/PublicClass.cs", 4, 26)]
        public void Verify_Diagnostic(string path, int line, int column)
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
        }
    }
}
