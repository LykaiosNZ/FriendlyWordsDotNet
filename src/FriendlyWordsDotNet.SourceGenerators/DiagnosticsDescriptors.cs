namespace FriendlyWordsDotNet.SourceGenerators
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    public static class DiagnosticsDescriptors
    {
        public static readonly DiagnosticDescriptor InvalidFileName = new(id: "FWDN-codegen-001",
                                                                          title: "Invalid words file name",
                                                                          messageFormat: "Words file name contains non-alphabet characters: {0}",
                                                                          category: nameof(FriendlyWordsSourceGenerator),
                                                                          defaultSeverity: DiagnosticSeverity.Error,
                                                                          isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidWord = new(id: "FWDN-codegen-002",
                                                                      title: "Invalid word in file",
                                                                      messageFormat: "Word contains non-alphabet characters: {0}",
                                                                      category: nameof(FriendlyWordsSourceGenerator),
                                                                      defaultSeverity: DiagnosticSeverity.Error,
                                                                      isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor EmptyFile = new(id: "FWDN-codegen-003",
                                                                    title: "File is empty",
                                                                    messageFormat: "Source file was empty",
                                                                    category: nameof(FriendlyWordsSourceGenerator),
                                                                    defaultSeverity: DiagnosticSeverity.Warning,
                                                                    isEnabledByDefault: true);
    }
}
