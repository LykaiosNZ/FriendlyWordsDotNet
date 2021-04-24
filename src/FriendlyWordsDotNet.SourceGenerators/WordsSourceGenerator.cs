using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace FriendlyWordsDotNet
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
    [Generator]
    public class WordsSourceGenerator : ISourceGenerator
    {
        private static readonly Regex AlphabetRegex = new("^[A-Za-z]+$");

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
            if (!context.AdditionalFiles.Any())
            {
                return;
            }

            var @namespace = NamespaceDeclaration(ParseName("FriendlyWordsDotNet"));

            @namespace = @namespace.AddUsings(
                UsingDirective(ParseName("System.Collections.Generic")),
                UsingDirective(ParseName("System.Collections.ObjectModel"))
             );

            var @class = ClassDeclaration(Identifier("FriendlyWords"))
                            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword), Token(SyntaxKind.PartialKeyword));

            foreach (var additionalFile in context.AdditionalFiles)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(additionalFile.Path);

                var propertyName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fileNameWithoutExtension);

                var arrayInitializerExpressions = additionalFile.GetText()
                                                                .Lines
                                                                .Select(l => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(l.ToString())))
                                                                .ToArray();

                var arrayCreationExpression = ImplicitArrayCreationExpression(
                                                InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                                                    .AddExpressions(arrayInitializerExpressions)
                                              );

                var objectCreationExpression = ObjectCreationExpression(ParseTypeName("ReadOnlyCollection<string>"))
                                                .AddArgumentListArguments(Argument(arrayCreationExpression));

                var initializer = EqualsValueClause(objectCreationExpression);

                var property = PropertyDeclaration(ParseTypeName("IReadOnlyCollection<string>"), propertyName)
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

            string code = @namespace.NormalizeWhitespace()
                                    .ToFullString();

            context.AddSource("FriendlyWordsGenerated", code);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
