using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FriendlyWordsDotNet
{
    public class Words : IReadOnlyCollection<string>
    {
        private readonly ILookup<int, string> _wordsByLength;

        public Words(IEnumerable<string> words)
        {
            _wordsByLength = words.ToLookup(w => w.Length);
        }

        public int Count => _wordsByLength.Count;

        public IEnumerator<string> GetEnumerator() => _wordsByLength.SelectMany(w => w).GetEnumerator();

        public IEnumerable<string> OfLength(int length) => _wordsByLength[length];

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
