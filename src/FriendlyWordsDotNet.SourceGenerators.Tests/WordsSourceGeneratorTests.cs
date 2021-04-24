namespace FriendlyWordsDotNet.SourceGenerators.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Reflection;
    using FluentAssertions;
    using FluentAssertions.Execution;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using NUnit.Framework;

    public class Tests
    {
        [Test(Description = "Tests that, given a valid input, the expected source is generated.")]
        public void ShouldGenerateCorrectOutput()
        {
            // Arrange
            var fileName = "test.txt";
            var expectedPropertyName = "Test";
            var expectedWords = new[] { "foo", "bar" };
            var expectedTypeName = "FriendlyWordsDotNet.FriendlyWords";

            var additionalText = new StringAdditionalText(fileName, string.Join(Environment.NewLine, expectedWords));

            ExecuteGenerator(new[] { additionalText }, out Compilation outputCompilation, out GeneratorDriverRunResult runResult);

            SemanticModel model = outputCompilation.GetSemanticModel(runResult.GeneratedTrees[0]);

            TestContext.Write(runResult.GeneratedTrees[0]);

            using (new AssertionScope("Generator Output"))
            {
                runResult.Diagnostics.Should().BeEmpty("generator should not generate any diagnostic errors from a valid file");
                runResult.GeneratedTrees.Should().HaveCount(1, "generator should only generate a single syntax tree");
                outputCompilation.GetDiagnostics().Should().BeEmpty("output compilation should be valid");
            }

            // Class Assertions
            INamedTypeSymbol? @class = outputCompilation.GetTypeByMetadataName(expectedTypeName);

            @class.Should().NotBeNull("generator should create the type {0}", expectedTypeName);

            using (new AssertionScope())
            {
                @class!.TypeKind.Should().Be(TypeKind.Class, "type {0} should be a class", expectedTypeName);
                @class.DeclaredAccessibility.Should().Be(Accessibility.Public, "type {0} should be publicly accessible", expectedTypeName);
                @class.IsStatic.Should().BeFalse("type {0} should not be static", expectedTypeName);
                @class.IsSealed.Should().BeTrue("type {0} should be sealed", expectedTypeName);

                var classDeclaration = (ClassDeclarationSyntax)@class.DeclaringSyntaxReferences[0].GetSyntax();

                classDeclaration.Modifiers.Should().Contain(t => t.IsKind(SyntaxKind.PartialKeyword), "declaration of type {0} should have the partial modifier", expectedTypeName);
            }

            // Property Assertions
            IPropertySymbol? property = @class.GetProperty(expectedPropertyName);

            property.Should().NotBeNull("property {0} should be generated", expectedPropertyName);

            using (new AssertionScope())
            {
                property!.IsStatic.Should().BeTrue("property {0} should be static", expectedPropertyName);
                property.IsReadOnly.Should().BeTrue("property {0} should only have a getter", expectedPropertyName);
                property.DeclaredAccessibility.Should().Be(Accessibility.Public, "property {0} should be publicly accessible", expectedPropertyName);

                ITypeSymbol expectedPropertyType = outputCompilation.GetSymbolForType<IReadOnlyCollection<string>>();

                property.Type.Should().Be(expectedPropertyType, "property {0} should be of type {1}", expectedPropertyName, expectedPropertyType);

                property.DeclaringSyntaxReferences.Should().HaveCount(1, "property {0} should only be declared a single time", expectedPropertyName);
            }

            // Property Initializer Assertions
            using (new AssertionScope())
            {
                var propertyNode = (PropertyDeclarationSyntax)property.DeclaringSyntaxReferences[0].GetSyntax();

                propertyNode.Initializer.Should().NotBeNull("property {0} should have an initializer", expectedPropertyName);

                propertyNode.Initializer!.Value.IsKind(SyntaxKind.ObjectCreationExpression).Should().BeTrue("initializer for property {0} should be an object creation expression", expectedPropertyName);

                var objectCreation = (ObjectCreationExpressionSyntax)propertyNode.Initializer.Value;

                ISymbol objectTypeSymbol = model.GetSymbolInfo(objectCreation.Type).Symbol!;

                ITypeSymbol expectedInitializerObjectType = outputCompilation.GetSymbolForType<ReadOnlyCollection<string>>();

                objectTypeSymbol.Should().Be(expectedInitializerObjectType, "initializer for property {0} should create an instance of {1}", expectedPropertyName, expectedInitializerObjectType);

                SeparatedSyntaxList<ArgumentSyntax> args = objectCreation.ArgumentList!.Arguments;

                args.Should().HaveCount(1);

                ArgumentSyntax argument = args.First();

                var arrayCreationExpression = (ImplicitArrayCreationExpressionSyntax)argument.Expression;

                arrayCreationExpression.Initializer.Expressions.Should().AllBeOfType<LiteralExpressionSyntax>().And.OnlyContain(l => l.IsKind(SyntaxKind.StringLiteralExpression), "initialized array should only contain strings");
                arrayCreationExpression.Initializer.Expressions.OfType<LiteralExpressionSyntax>().Select(e => e.Token.ValueText).Should().BeEquivalentTo(expectedWords, "initialized array should contain all the words from the AdditionalText");
            }
        }

        [Test]
        public void ShouldReturnDiagnosticWhenFileNameContainsNonAlphabetCharacters()
        {
            // Arrange
            var fileName = "te#%$#@%9st.txt";
            var words = new[] { "foo", "bar" };

            var additionalText = new StringAdditionalText(fileName, string.Join(Environment.NewLine, words));

            // Act
            ExecuteGenerator(new[] { additionalText }, out Compilation compilation, out GeneratorDriverRunResult runResult);

            // Assert
            runResult.Diagnostics.Should().Contain(d => d.Id == WordsSourceGenerator.InvalidFileNameErrorId && d.Severity == DiagnosticSeverity.Error);
        }

        [Test]
        public void ShouldReturnDiagnosticWhenWordContainsNonAlphabetCharacters()
        {
            // Arrange
            var fileName = "test.txt";
            var words = new[] { "foo", "b@r" };

            var additionalText = new StringAdditionalText(fileName, string.Join(Environment.NewLine, words));

            // Act
            ExecuteGenerator(new[] { additionalText }, out Compilation compilation, out GeneratorDriverRunResult runResult);

            // Assert
            runResult.Diagnostics.Should().Contain(d => d.Id == WordsSourceGenerator.InvalidWordErrorId && d.Severity == DiagnosticSeverity.Error);
        }

        private static void ExecuteGenerator(IEnumerable<AdditionalText> additionalTexts, out Compilation outputCompilation, out GeneratorDriverRunResult runResult)
        {
            Compilation inputCompilation = CreateCompilation();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new WordsSourceGenerator() }, additionalTexts);

            // Act
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out outputCompilation, out ImmutableArray<Diagnostic> _);

            // Assert
            runResult = driver.GetRunResult();
        }

        private static Compilation CreateCompilation()
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(string.Empty) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
