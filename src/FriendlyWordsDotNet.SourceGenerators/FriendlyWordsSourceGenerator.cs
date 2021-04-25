namespace FriendlyWordsDotNet.SourceGenerators
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    /// <summary>
    /// A source generator that generates a class containing collections of words from multiple source files.
    /// </summary>
    [Generator]
    public class FriendlyWordsSourceGenerator : ISourceGenerator
    {
        private const string Namespace = "FriendlyWordsDotNet";
        private const string ClassName = "FriendlyWords";
        private const string PropertyType = "IReadOnlyCollection<string>";
        private const string PropertyInitializerType = "ReadOnlyCollection<string>";
        private const string SourceName = ClassName + "Generated";

        private static readonly Regex AlphabetRegex = new("^[A-Za-z]+$");

        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.AdditionalFiles.Any())
            {
                return;
            }

            PropertyDeclarationSyntax[] properties = BuildProperties(context).ToArray();

            ClassDeclarationSyntax @class = ClassDeclaration(Identifier(ClassName))
                                               .AddModifiers(
                                                  Token(SyntaxKind.PublicKeyword),
                                                  Token(SyntaxKind.SealedKeyword),
                                                  Token(SyntaxKind.PartialKeyword)
                                                )
                                               .AddMembers(properties);

            NamespaceDeclarationSyntax @namespace = NamespaceDeclaration(ParseName(Namespace))
                                                       .AddUsings(
                                                           UsingDirective(ParseName("System.Collections.Generic")),
                                                           UsingDirective(ParseName("System.Collections.ObjectModel"))
                                                        )
                                                       .AddMembers(@class)
                                                       .NormalizeWhitespace();

            context.AddSource(SourceName, @namespace.ToFullString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private static IEnumerable<PropertyDeclarationSyntax> BuildProperties(GeneratorExecutionContext context)
        {
            foreach (AdditionalText file in context.AdditionalFiles)
            {
                PropertyDeclarationSyntax? property = BuildProperty(context, file);

                if (property == null)
                {
                    continue;
                }

                yield return property;
            }
        }

        private static PropertyDeclarationSyntax? BuildProperty(GeneratorExecutionContext context, AdditionalText file)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Path);

            if (!AlphabetRegex.IsMatch(fileNameWithoutExtension))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticsDescriptors.InvalidFileName, Location.Create(file.Path, new TextSpan(0, 0), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero)), fileNameWithoutExtension));
                return null;
            }

            EqualsValueClauseSyntax? initializer = BuildInitializer(context, file);

            if (initializer == null)
            {
                return null;
            }

            var propertyName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fileNameWithoutExtension);

            return PropertyDeclaration(ParseTypeName(PropertyType), propertyName)
                      .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                      .AddAccessorListAccessors(
                          AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                             .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                       )
                      .WithInitializer(initializer)
                      .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private static EqualsValueClauseSyntax? BuildInitializer(GeneratorExecutionContext context, AdditionalText file)
        {
            LiteralExpressionSyntax[] arrayInitializerExpressions = BuildArrayInitializerExpressions(context, file).ToArray();

            if (!arrayInitializerExpressions.Any())
            {
                return null;
            }

            ImplicitArrayCreationExpressionSyntax implicitArrayCreation = ImplicitArrayCreationExpression(
                                                                             InitializerExpression(SyntaxKind.ArrayInitializerExpression)
                                                                                .AddExpressions(arrayInitializerExpressions)
                                                                          );

            ObjectCreationExpressionSyntax objectCreation = ObjectCreationExpression(ParseTypeName(PropertyInitializerType))
                                                               .AddArgumentListArguments(Argument(implicitArrayCreation));

            return EqualsValueClause(objectCreation);
        }

        private static IEnumerable<LiteralExpressionSyntax> BuildArrayInitializerExpressions(GeneratorExecutionContext context, AdditionalText file)
        {
            SourceText? text = file.GetText();

            if (string.IsNullOrWhiteSpace(text?.ToString()))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticsDescriptors.EmptyFile, Location.Create(file.Path, new TextSpan(0, 0), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero))));
                yield break;
            }

            foreach (TextLine line in text!.Lines)
            {
                var word = line.ToString();

                if (!AlphabetRegex.IsMatch(word))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticsDescriptors.InvalidWord, Location.Create(file.Path, line.Span, text.Lines.GetLinePositionSpan(line.Span)), word));
                    continue;
                }

                yield return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(word));
            }
        }
    }
}
