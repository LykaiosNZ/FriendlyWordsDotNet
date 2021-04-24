using System;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace FriendlyWordsDotNet.SourceGenerators
{
    internal class StringAdditionalText : AdditionalText
    {
        private readonly Lazy<SourceText> _sourceText;

        public StringAdditionalText(string path, string value)
        {
            Path = path;

            _sourceText = new Lazy<SourceText>(() => new StringSourceText(value));
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => _sourceText.Value;

        private class StringSourceText : SourceText
        {
            private readonly string _value;

            public StringSourceText(string value)
            {
                _value = value;
            }

            public override char this[int position] => _value[position];

            public override Encoding Encoding => Encoding.UTF8;

            public override int Length => _value.Length;

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => _value.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}
