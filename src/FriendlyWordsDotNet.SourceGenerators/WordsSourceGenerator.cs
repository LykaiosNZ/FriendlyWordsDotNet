using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FriendlyWordsDotNet
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    [Generator]
    public class WordsSourceGenerator : ISourceGenerator
    {
        private static readonly Regex AlphabetRegex = new("[A-Za-z]");

        private static readonly DiagnosticDescriptor InvalidFileNameError = new(id: "FWDN-codegen-001",
                                                                                title: "Invalid words file name",
                                                                                messageFormat: "Words file name contains non-alphabet characters: {0}",
                                                                                category: nameof(WordsSourceGenerator),
                                                                                defaultSeverity: DiagnosticSeverity.Error,
                                                                                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor InvalidWordError = new(id: "FWDN-codegen-002",
                                                                            title: "Invalid word",
                                                                            messageFormat: "Word contains non-alphabet characters: {0}, Source File: {1}",
                                                                            category: nameof(WordsSourceGenerator),
                                                                            defaultSeverity: DiagnosticSeverity.Error,
                                                                            isEnabledByDefault: true);

        public void Execute(GeneratorExecutionContext context)
        {
            var sb = new StringBuilder(@"
using System;

namespace FriendlyWordsDotNet
{
    public partial class FriendlyWords
    {
            ");

            foreach (var file in context.AdditionalFiles)
            {
                sb.AppendLine(GeneratePropertyFromFile(file, context.ReportDiagnostic));
            }

            sb.Append(@"
    }
}");

            context.AddSource("FriendlyWordsGenerated.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private string GeneratePropertyFromFile(AdditionalText file, Action<Diagnostic> diagnosticCallback)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.Path);

            if (!AlphabetRegex.IsMatch(fileName))
            {
                diagnosticCallback(Diagnostic.Create(InvalidFileNameError, Location.None, fileName));
                return string.Empty;
            }

            var fieldName = "_" + fileName[0].ToString().ToLowerInvariant() + fileName.Substring(1);
            var propertyName = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fileName.ToLower());

            var sb = new StringBuilder($@"
        private static readonly string[] {fieldName} = new[] 
        {{
");

            foreach (var line in file.GetText().Lines)
            {
                if (!AlphabetRegex.IsMatch(line.ToString()))
                {
                    diagnosticCallback(Diagnostic.Create(InvalidWordError, Location.None, line, Path.GetFileName(file.Path)));
                    continue;
                }

                sb.AppendLine($"           \"{line}\",");
            }

            sb.Append($@"
        }};

        public static Words {propertyName} {{get;}} = new Words({fieldName});");

            return sb.ToString();
        } 
    }
}
