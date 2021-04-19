using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Text;

namespace FriendlyWordsDotNet
{
    [Generator]
    public class WordsSourceGenerator : ISourceGenerator
    {
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
                sb.AppendLine(GeneratePropertyFromFile(file));
            }

            sb.Append(@"
    }
}");

            context.AddSource("FriendlyWordsGenerated.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private string GeneratePropertyFromFile(AdditionalText file)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.Path);

            var fieldName = "_" + fileName.ToLowerInvariant();
            var propertyName = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fileName.ToLower());

            var lines = file.GetText().Lines;

            var sb = new StringBuilder($@"
        private static readonly string[] {fieldName} = new[] 
        {{
");

            foreach (var line in lines)
            {
                sb.AppendLine($"           \"{line}\",");
            }

            sb.Append($@"
        }};

        public static WordCollection {propertyName} {{get;}} = new WordCollection({fieldName});");

            return sb.ToString();
        }
    }
}
