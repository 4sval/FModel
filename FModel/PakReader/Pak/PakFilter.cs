using System.Collections;
using System.Collections.Generic;

namespace FModel.PakReader.Pak
{
    // Currently only supports strings that start with a value
    // I've just implemented this myself to save tons of memory so you don't have to
    // allocate FPakEntries on Fortnite emoji textures you're not going to use
    public class PakFilter : IPakFilter, IEnumerable<string>
    {
        public bool CaseSensitive { get; }
        readonly List<string> Filter;
        
        // only 1 instance
        public static readonly IPakFilter Default = new DefaultPakFilter();

        public PakFilter(IEnumerable<string> filter, bool caseSensitive = true)
        {
            Filter = new List<string>(filter);
            if (!caseSensitive)
            {
                for (int i = 0; i < Filter.Count; i++)
                {
                    Filter[i] = Filter[i].ToLowerInvariant();
                }
            }
            CaseSensitive = caseSensitive;
        }

        public bool AddFilter(string filter)
        {
            if (!CaseSensitive) filter = filter.ToLowerInvariant();
            if (!Filter.Contains(filter))
            {
                Filter.Add(filter);
                return true;
            }
            return false;
        }

        public int AddFilters(IEnumerable<string> filter)
        {
            var i = 0;
            foreach (var s in filter)
                if (AddFilter(s)) i++;
            return i;
        }

        public bool CheckFilter(string path, bool caseSensitive)
        {
            // path is case sensitive but the filter isn't
            if (caseSensitive && !CaseSensitive)
                path = path.ToLowerInvariant();
            foreach (var filter in Filter)
                if (path.StartsWith(filter))
                    return true;
            return false;
        }

        public IEnumerator<string> GetEnumerator() => Filter.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Filter.GetEnumerator();
    }
}
