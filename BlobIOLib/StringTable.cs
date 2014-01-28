using System.Collections.Generic;

namespace BlobIO
{
    public class StringTable
    {
        private Dictionary<string, int> _strings;

        private int _topNumber;

        public int AddString(string key)
        {
            int existing;

            if (!_strings.TryGetValue(key, out existing))
                existing = ++_topNumber;

            return existing;
        }

        public int GetString(string key, bool create = true)
        {
            if (create)
                return AddString(key);
            else
            {
                int existing;
                if (_strings.TryGetValue(key, out existing))
                    return existing;
            }

            return 0;
        }

        public void WriteString(string str, Bits bits)
        {
            if (bits != null)
            {

            }
        }

        public StringTable(bool ignoreCase = true)
        {
            _strings = new Dictionary<string, int>(ignoreCase ? System.StringComparer.InvariantCultureIgnoreCase : System.StringComparer.Ordinal);
        }
    }
}

