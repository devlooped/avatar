using System.IO;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Avatars.UnitTests
{
    public class AVTR002_DuplicateBaseType : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer() => new ValidateTypesAnalyzer();

        [Theory]
        [InlineData(ThisAssembly.Constants.CodeAnalysis.AVTR002.Diagnostic.PublicClass, 9, 26)]
        public void Verify_Diagnostic(string path, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = AvatarDiagnostics.DuplicateBaseType.Id,
                Message = Resources.DuplicateBaseType_Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {
                    new DiagnosticResultLocation("Test0.cs", line, column)
                },
            };

            VerifyCSharpDiagnostic(
                new[]
                {
                    File.ReadAllText(path),
                    File.ReadAllText(@"Avatar/Avatar.cs"),
                },
                expected);
        }
    }
}
