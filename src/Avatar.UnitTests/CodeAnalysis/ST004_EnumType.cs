using System.IO;
using Avatars.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Avatars.UnitTests
{
    public class ST004_EnumType : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer() => new ValidateTypesAnalyzer();

        [Fact]
        public void VerifyMultipleEnums()
        {
            VerifyCSharpDiagnostic(
                new[]
                {
                    File.ReadAllText(ThisAssembly.Constants.CodeAnalysis.ST004.Diagnostic.MultipleEnums),
                    File.ReadAllText(@"Avatar/Avatar.cs"),
                },
                new[]
                {
                    new DiagnosticResult
                    {
                        Id = AvatarDiagnostics.EnumType.Id,
                        Message = string.Format(Resources.EnumType_Message, "PlatformID"),
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 26)
                        },
                    },
                    new DiagnosticResult
                    {
                        Id = AvatarDiagnostics.EnumType.Id,
                        Message = string.Format(Resources.EnumType_Message, "TypeCode"),
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 26)
                        },
                    }
                });
        }

        [Theory]
        [InlineData(ThisAssembly.Constants.CodeAnalysis.ST004.Diagnostic.PublicClass, 9, 26)]
        public void Verify_Diagnostic(string path, int line, int column)
        {
            var expected = new DiagnosticResult
            {
                Id = AvatarDiagnostics.EnumType.Id,
                Message = string.Format(Resources.EnumType_Message, "PlatformID"),
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
