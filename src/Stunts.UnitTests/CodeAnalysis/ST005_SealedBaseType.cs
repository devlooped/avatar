using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Stunts.CodeAnalysis;
using Xunit;

namespace Stunts.UnitTests
{
    public class ST005_SealedBaseType : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer() => new ValidateTypesAnalyzer();

        [Theory]
        [InlineData("CodeAnalysis/ST005/Diagnostic/PublicClass.cs", 9, 25)]
        public void Verify_Diagnostic(string path, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = "ST005",
                Message = string.Format(Stunts.CodeAnalysis.Resources.SealedBaseType_Message, "BaseType"),
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
