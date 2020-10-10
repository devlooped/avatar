using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Stunts.CodeAnalysis;
using Xunit;

namespace Stunts.UnitTests
{
    public class ST004_DuplicateBaseType : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer() => new ValidateTypesAnalyzer();

        [Theory]
        [InlineData("CodeAnalysis/ST004/Diagnostic/PublicClass.cs", 9, 25)]
        public void Verify_Diagnostic(string path, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = "ST004",
                Message = Stunts.CodeAnalysis.Resources.DuplicateBaseType_Message,
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
