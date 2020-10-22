using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Stunts.CodeAnalysis;
using Xunit;

namespace Stunts.UnitTests
{
    public class ST009_NestedType : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer() => new NestedTypeAnalyzer();

        [Theory]
        [InlineData("CodeAnalysis/ST009/Diagnostic/PublicClass.cs", 9, 25)]
        public void Verify_Diagnostic(string path, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = StuntDiagnostics.NestedType.Id,
                Message = string.Format(Stunts.CodeAnalysis.Resources.NestedType_Message, "ContainingType.INested"),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {
                    new DiagnosticResultLocation("Test0.cs", line, column)
                },
            };

            VerifyCSharpDiagnostic(
                new[]
                {
                    File.ReadAllText(path),
                    File.ReadAllText(@"Stunts/Stunt.cs"),
                },
                expected);
        }
    }
}
