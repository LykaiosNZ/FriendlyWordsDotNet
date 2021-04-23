using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace FriendlyWordsDotNet.SourceGenerators.Tests
{
    public class Tests
    {
        [Test]
        public void SimpleGeneratorTest()
        {
            // Arrange
            Compilation inputCompilation = CreateCompilation(@"
namespace Test.Namespace
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
}
");

            WordsSourceGenerator generator = new WordsSourceGenerator();

            var fileName = "test.txt";
            var expectedPropertyName = "Test";
            var expectedWords = new[] { "foo", "bar" };

            var additionalText = new StringAdditionalText(fileName, string.Join(Environment.NewLine, expectedWords));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator }, additionalTexts: new[] { additionalText });

            // Act
            // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            // Assert
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            runResult.Diagnostics.Should().BeEmpty();
            runResult.GeneratedTrees.Should().HaveCount(1);

            // Check the generated class
            INamedTypeSymbol? type = outputCompilation.GetTypeByMetadataName("FriendlyWordsDotNet.FriendlyWords");

            type.Should().NotBeNull();
            type!.TypeKind.Should().Be(TypeKind.Class);
            type.DeclaredAccessibility.Should().Be(Accessibility.Public);
            type.IsStatic.Should().BeFalse();

            // Check the generated property
            var members = type.GetMembers(expectedPropertyName);

            members.Should().HaveCount(1);
            members[0].Kind.Should().Be(SymbolKind.Property);

            var property = (IPropertySymbol)members[0];

            property.Kind.Should().Be(SymbolKind.Property);
            property.IsStatic.Should().BeTrue();
            property.IsReadOnly.Should().BeTrue();
            property.DeclaredAccessibility.Should().Be(Accessibility.Public);

            var iReadOnlyCollection = outputCompilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1")!;
            var iReadOnlyCollectionString = iReadOnlyCollection.Construct(outputCompilation.GetSpecialType(SpecialType.System_String));

            property.Type.Should().Be(iReadOnlyCollectionString);

            // Jumping over to the syntax tree to inspect the getter
            property.Locations.Should().HaveCount(1);
            var location = property.Locations[0];
            var propertyNode = location.SourceTree!
                .GetRoot()
                .FindNode(location.SourceSpan);

            var literals = propertyNode.DescendantNodes().OfType<LiteralExpressionSyntax>().Select(l => l.Token.ValueText);

            literals.Should().BeEquivalentTo(expectedWords);
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}