namespace FriendlyWordsDotNet
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    [Generator]
    public class WordsSourceGenerator : ISourceGenerator
    {
        private static readonly Regex AlphabetRegex = new("^[A-Za-z]+$");

        public const string InvalidFileNameErrorId = "FWDN-codegen-001";
        public const string InvalidWordErrorId = "FWDN-codegen-002";

        private static readonly DiagnosticDescriptor InvalidFileNameError = new(id: InvalidFileNameErrorId,
                                                                                title: "Invalid words file name",
                                                                                messageFormat: "Words file name contains non-alphabet characters: {0}",
                                                                                category: nameof(WordsSourceGenerator),
                                                                                defaultSeverity: DiagnosticSeverity.Error,
                                                                                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor InvalidWordError = new(id: InvalidWordErrorId,
                                                                            title: "Invalid word",
                                                                            messageFormat: "Word contains non-alphabet characters: {0}, Source File: {1}",
                                                                            category: nameof(WordsSourceGenerator),
                                                                            defaultSeverity: DiagnosticSeverity.Error,
                                                                            isEnabledByDefault: true);

        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.AdditionalFiles.Any())
            {
                return;
            }

            NamespaceDeclarationSyntax @namespace = NamespaceDeclaration(ParseName("FriendlyWordsDotNet"));

            @namespace = @namespace.AddUsings(
                UsingDirective(ParseName("System.Collections.Generic")),
                UsingDirective(ParseName("System.Collections.ObjectModel"))
             );

            ClassDeclarationSyntax @class = ClassDeclaration(Identifier("FriendlyWords"))
                            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword), Token(SyntaxKind.PartialKeyword));

            foreach (AdditionalText additionalFile in context.AdditionalFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(additionalFile.Path);

                if (!AlphabetRegex.IsMatch(fileNameWithoutExtension))
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidFileNameError, Location.None, additionalFile.Path));
                    continue;
                }

                var allLines = additionalFile.GetText().Lines.Select(l => l.ToString()).ToArray();
                var arrayInitializerExpressions = new List<LiteralExpressionSyntax>();

                foreach (var line in allLines)
                {
                    if (!AlphabetRegex.IsMatch(line))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidWordError, Location.None, line, additionalFile.Path));
                        continue;
                    }

                    arrayInitializerExpressions.Add(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(line)));
                }

                ImplicitArrayCreationExpressionSyntax implicitArrayCreation = ImplicitArrayCreationExpression(
                                                InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                                                    .AddExpressions(arrayInitializerExpressions.ToArray())
                                              );

                ObjectCreationExpressionSyntax objectCreation = ObjectCreationExpression(ParseTypeName("ReadOnlyCollection<string>"))
                                                .AddArgumentListArguments(Argument(implicitArrayCreation));

                EqualsValueClauseSyntax initializer = EqualsValueClause(objectCreation);

                var propertyName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fileNameWithoutExtension);

                PropertyDeclarationSyntax property = PropertyDeclaration(ParseTypeName("IReadOnlyCollection<string>"), propertyName)
                                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                                .AddAccessorListAccessors(
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                 )
                                .WithInitializer(initializer)
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

                @class = @class.AddMembers(property);
            }

            @class = @class.NormalizeWhitespace();

            @namespace = @namespace.AddMembers(@class);

            var code = @namespace.NormalizeWhitespace()
                                    .ToFullString();

            context.AddSource("FriendlyWordsGenerated", code);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
