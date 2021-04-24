namespace FriendlyWordsDotNet
{
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

                var propertyName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fileNameWithoutExtension);

                LiteralExpressionSyntax[] arrayInitializerExpressions = additionalFile.GetText()
                                                                            .Lines
                                                                            .Select(l => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(l.ToString())))
                                                                            .ToArray();

                ImplicitArrayCreationExpressionSyntax implicitArrayCreation = ImplicitArrayCreationExpression(
                                                InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                                                    .AddExpressions(arrayInitializerExpressions)
                                              );

                ObjectCreationExpressionSyntax objectCreation = ObjectCreationExpression(ParseTypeName("ReadOnlyCollection<string>"))
                                                .AddArgumentListArguments(Argument(implicitArrayCreation));

                EqualsValueClauseSyntax initializer = EqualsValueClause(objectCreation);

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
