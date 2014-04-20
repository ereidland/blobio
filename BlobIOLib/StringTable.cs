using System.Collections.Generic;

namespace BlobIO
{
    public class StringTable
    {
        private Dictionary<string, ushort> _stringsByName;
        private Dictionary<ushort, string> _stringsByID;

        private bool _isHost;

        private ushort _topNumber;

        private void AddString(string str, ushort id)
        {
            _stringsByName[str] = id;
            _stringsByID[id] = str;
        }

        private ushort AddString(string str)
        {
            ushort existing;
            if (!_stringsByName.TryGetValue(str, out existing))
            {
                existing = ++_topNumber;
                AddString(str, existing);
            }
            return existing;
        }

        public void WriteString(string str, Bits bits, bool createIfMissing = true)
        {
            if (bits != null)
            {
                ushort existing;
                bool alreadyDefined = true;
                bool definesEntry = false;
                if (!_stringsByName.TryGetValue(str, out existing))
                {
                    alreadyDefined = false;
                    definesEntry = _isHost;
                    if (_isHost)
                        existing = AddString(str);
                }

                bits.WriteBit(alreadyDefined);

                if (alreadyDefined)
                    bits.WriteUShort(existing);
                else
                {
                    bits.WriteBit(definesEntry);

                    if (definesEntry)
                        bits.WriteUShort(existing);

                    bits.WriteString(str);
                }
            }
        }

        public string ReadString(Bits bits)
        {
            if (bits != null)
            {
                bool alreadyDefined = bits.ReadBit();
                ushort id = 0;
                if (alreadyDefined)
                {
                    id = bits.ReadUShort();
                    return GetString(id);
                }
                else
                {
                    bool definesEntry = bits.ReadBit();
                    if (definesEntry)
                    {
                        if (!_isHost) //Clients cannot send the host new entries.
                        {
                            id = bits.ReadUShort();
                            string str = bits.ReadString();
                            AddString(str, id);

                            return str;
                        }
                    }
                    else
                        return bits.ReadString();
                }
            }
            return null;
        }

        public ushort GetID(string str)
        {
            ushort id = 0;
            _stringsByName.TryGetValue(str, out id);
            return id;
        }

        public string GetString(ushort id)
        {
            string str;
            _stringsByID.TryGetValue(id, out str);
            return str;
        }

        public StringTable(bool isHost, bool ignoreCase = false)
        {
            _isHost = isHost;
            _stringsByName = new Dictionary<string, ushort>(ignoreCase ? System.StringComparer.InvariantCultureIgnoreCase : System.StringComparer.Ordinal);
            _stringsByID = new Dictionary<ushort, string>();
        }
    }
}

