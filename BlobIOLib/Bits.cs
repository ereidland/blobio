using System;
using System.Text;

namespace BlobIO
{
    public class Bits
    {
        private static UTF8Encoding _stringEncoding = new UTF8Encoding();
        public const int IntSizeInBits = 32;
        public const int ShortSizeInBits = 16;

        public enum SeekMode
        {
            Begin,
            Current,
            End,
        }

        public static int BitSizeToByteSize(int bitSize)
        {
            int size = bitSize >> 3;
            if (!IsByteAligned(bitSize))
                ++size;

            return size;
        }

        public static int GetUsedBits(int number, bool ignoreSign = true)
        {
            int high = 0;
            int min = ignoreSign ? 0 : -1;
            //Skipping sign bit.
            for (int i = IntSizeInBits - 1; i > min; i--)
            {
                if (Get(number, i))
                    high = IntSizeInBits - i;
            }

            return high;
        }

        public static int BitIndexToByteIndex(int index) { return index >> 3; }

        public static bool IsByteAligned(int bits) { return (bits & 7) == 0; }

        public static bool Get(int bits, int index) { return ((bits >> index) & 1) == 1; }

        public static int Set(int bits, int index, bool state)
        {
            if (state)
                bits |= (1 << index);
            else
                bits &= ~(1 << index);
            return bits;
        }

        public static int BytesToInt(byte a, byte b, byte c, byte d)
        {
            int result = d;
            result |= a << 24;
            result |= b << 16;
            result |= c << 8;
            return result;
        }

        public static void ConvertIntToBytes(int i, out byte a, out byte b, out byte c, out byte d)
        {
            a = (byte)((i >> 24) & 0xFF);
            b = (byte)((i >> 16) & 0xFF);
            c = (byte)((i >> 8) & 0xFF);
            d = (byte)(i & 0xFF);
        }

        public static short BytesToShort(byte a, byte b)
        {
            short result = b;
            result |= (short)(a << 8);
            return result;
        }

        public static void ConvertShortToBytes(short s, out byte a, out byte b)
        {
            a = (byte)(s >> 8);
            b = (byte)(s & 0xFF);
        }

        private byte[] _bytes;

        public int TopBitIndex { get; private set; }

        private int _bitIndex;
        public int BitIndex
        {
            get { return _bitIndex; }
            private set
            {
                _bitIndex = value;
                if (_bitIndex > TopBitIndex)
                    TopBitIndex = _bitIndex;
            }
        }

        public Bits EnsureBitCapacity(int howManyBits)
        {
            return EnsureByteCapacity(BitSizeToByteSize(howManyBits));
        }

        public Bits EnsureByteCapacity(int howManyBytes)
        {
            if (_bytes == null || _bytes.Length < howManyBytes)
            {
                byte[] newBytes = new byte[howManyBytes];

                if (_bytes != null)
                    _bytes.CopyTo(newBytes, 0);

                _bytes = newBytes;
            }
            return this;
        }

        public Bits Trim()
        {
            if (_bytes != null)
            {
                byte[] newBytes = new byte[BitSizeToByteSize(TopBitIndex)];
                _bytes.CopyTo(newBytes, 0);
                _bytes = newBytes;
            }
            return this;
        }

        public Bits WriteBit(bool state)
        {
            EnsureBitCapacity(BitIndex + 1);
            return WriteBitInternal(state);
        }

        private Bits WriteBitInternal(bool state)
        {
            int index = (BitIndex++) >> 3;
            _bytes[index] = (byte)Set(_bytes[index], BitIndex & 7, state);
            return this;
        }

        public Bits WriteByte(byte value)
        {
            EnsureBitCapacity(BitIndex + 8);
            return WriteByteInternal(value);
        }

        public Bits WriteBytes(byte[] values, int startIndex, int length)
        {
            EnsureBitCapacity(BitIndex + length*8);
            return WriteBytesInternal(values, startIndex, length);
        }

        public Bits WriteBytes(byte[] values, int startIndex = 0)
        {
            return WriteBytes(values, startIndex, values.Length - startIndex);
        }

        private Bits WriteBytesInternal(byte[] values, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; i++)
                WriteByteInternal(values[i]);
            return this;
        }

        private Bits WriteByteInternal(byte value, bool convert = true)
        {
            if (IsByteAligned(BitIndex))
            {
                _bytes[BitIndex >> 3] = value;
                BitIndex += 8;
            }
            else
            {
                for (int i = 0; i < 8; i++)
                    WriteBitInternal(Get(value, i));
            }
            return this;
        }
        
        public Bits WriteShort(short value)
        {
            EnsureBitCapacity(BitIndex + ShortSizeInBits);
            return WriteShortInternal(value);
        }

        private Bits WriteShortInternal(short value)
        {
            byte a, b;
            ConvertShortToBytes(value, out a, out b);
            WriteByteInternal(a);
            WriteByteInternal(b);
            return this;
        }

        public Bits WriteInt(int value)
        {
            EnsureBitCapacity(BitIndex + IntSizeInBits);
            return WriteIntInternal(value);
        }

        private Bits WriteIntInternal(int value)
        {
            byte a, b, c, d;
            ConvertIntToBytes(value, out a, out b, out c, out d);

            WriteByteInternal(a);
            WriteByteInternal(b);
            WriteByteInternal(c);
            WriteByteInternal(d);

            return this;
        }

        public Bits WriteFloat(float value)
        {
            EnsureBitCapacity(BitIndex + IntSizeInBits);
            return WriteFloatInternal(value);
        }

        private Bits WriteFloatInternal(float value)
        {
            WriteIntInternal(new TypeUnion<int, float>(value).FirstType);
            return this;
        }
        
        public Bits WritePartialNumber(int num, int bits)
        {
            if (bits > 0 && bits < IntSizeInBits)
            {
                EnsureBitCapacity(BitIndex + bits);
                WritePartialNumberInternal(num, bits);
            }
            return this;
        }

        private Bits WritePartialNumberInternal(int num, int bits)
        {
            if (bits > 0 && bits < IntSizeInBits)
            {
                for (int i = IntSizeInBits - bits; i < IntSizeInBits; i++)
                    WriteBit(Get(num, i));
            }
            return this;
        }

        public Bits WriteString(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                byte[] bytes = _stringEncoding.GetBytes(str);
                WriteShort((short)bytes.Length);
                WriteBytes(bytes);
            }
            else
            {
                WriteShort(0);
            }

            return this;
        }

        public Bits SkipPaddingBits()
        {
            BitIndex = BitIndexToByteIndex(BitIndex) << 3;
            return this;
        }

        public bool SeekBits(int howManyBits, SeekMode mode)
        {
            int newIndex = BitIndex;
            switch (mode)
            {
                case SeekMode.Begin:
                    newIndex = howManyBits;
                    break;
                case SeekMode.Current:
                    newIndex += howManyBits;
                    break;
                case SeekMode.End:
                    newIndex = TopBitIndex - howManyBits;
                break;
            }

            if (newIndex < 0 || newIndex >= _bytes.Length)
                return false;

            //Modifying __realIndex instead of _index because we don't want to set the _topIndex unless we're incrementing _index during a write.
            _bitIndex = newIndex;
            return false;
        }
        
        public bool SeekBytes(int howManyBytes, SeekMode seekMode) { return SeekBits(howManyBytes << 3, seekMode); }

        public bool TryReadBit(out bool value)
        {
            if (BitIndex < TopBitIndex)
            {
                value = Get(_bytes[BitIndex >> 3], BitIndex & 7);
                BitIndex++;
                return true;
            }
            value = false;
            return false;
        }

        public bool TryReadByte(out byte value)
        {
            if (BitIndex <= TopBitIndex - 8)
            {
                if (IsByteAligned(BitIndex))
                {
                    value = _bytes[BitIndex >> 3];
                    BitIndex += 8;
                }
                else
                {
                    value = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        value = (byte)Set(value, i, Get(_bytes[BitIndex >> 3], BitIndex & 7));
                        BitIndex++;
                    }
                }
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryReadBytes(int byteLength, out byte[] values)
        {
            if (BitIndex <= TopBitIndex - byteLength*8)
            {
                byte[] read = new byte[byteLength];
                for (int i = 0; i < read.Length; i++)
                    TryReadByte(out read[i]);

                values = read;
                return true;
            }
            values = null;
            return false;
        }

        public bool TryReadShort(out short value)
        {
            if (BitIndex <= TopBitIndex - ShortSizeInBits)
            {
                byte a, b;
                TryReadByte(out a);
                TryReadByte(out b);

                value = BytesToShort(a, b);
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryReadInt(out int value)
        {
            if (BitIndex <= TopBitIndex - IntSizeInBits)
            {
                byte a, b, c, d;
                TryReadByte(out a);
                TryReadByte(out b);
                TryReadByte(out c);
                TryReadByte(out d);

                value = BytesToInt(a, b, c, d);
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryReadFloat(out float value)
        {
            int inValue;
            if (TryReadInt(out inValue))
            {
                value = new TypeUnion<int, float>(inValue).SecondType;
                return true;
            }
            value = 0;
            return false;
        }

        public bool TryReadString(out string value)
        {
            short length;
            byte[] bytes;

            value = null;

            if (TryReadShort(out length))
            {
                if (length > 0)
                {
                    if (TryReadBytes(length, out bytes))
                    {
                        value = _stringEncoding.GetString(bytes);
                    }
                    else
                        return false;
                }

                return true;
            }
            return false;
        }

        public bool TryReadPartialNumber(int bits, out int value)
        {
            value = 0;
            if (bits > 0 && bits < IntSizeInBits && BitIndex < TopBitIndex - bits)
            {
                bool state;
                for (int i = bits - 1; i >= 0; i--)
                {
                    TryReadBit(out state);
                    value = Set(value, IntSizeInBits - i, state);
                }
                return true;
            }
            return false;
        }

        public byte ReadByte(byte def = 0)
        {
            byte value;
            if (TryReadByte(out value))
                return value;
            return def;
        }

        public byte[] ReadBytes(int byteLength)
        {
            byte[] values;
            if (TryReadBytes(byteLength, out values))
                return values;
            return null;
        }

        public short ReadShort(short def = 0)
        {
            short value;
            if (TryReadShort(out value))
                return value;
            return def;
        }

        public int ReadInt(int def = 0)
        {
            int value;
            if (TryReadInt(out value))
                return value;
            return def;
        }

        public float ReadFloat(float def = 0)
        {
            float value;
            if (TryReadFloat(out value))
                return value;
            return def;
        }

        public string ReadString()
        {
            string value;
            if (TryReadString(out value))
                return value;
            return null;
        }

        public int ReadPartialNumber(int bits, int def = 0)
        {
            int value;
            if (TryReadPartialNumber(bits, out value))
                return value;
            return def;
        }

        /// <summary>
        /// Creates a new Bits instance that uses a reference to this objects bytes array.
        /// Useful for recycling the internal array without resizing.
        /// </summary>
        /// <returns>New instance of Bits.</returns>
        public Bits ReleaseArrayAsBits()
        {
            return new Bits(ReleaseArray(), false);
        }

        /// <summary>
        /// Releases the internal byte array and returns it.
        /// </summary>
        /// <returns>The released internal byte array.</returns>
        public byte[] ReleaseArray()
        {
            byte[] existing = _bytes;
            _bytes = null;
            BitIndex = 0;
            TopBitIndex = 0;
            return existing;
        }

        public Bits Clear()
        {
            _bytes = null;
            BitIndex = 0;
            TopBitIndex = 0;
            return this;
        }

        public Bits() {}

        public Bits(Bits other)
        {
            if (other._bytes != null)
            {
                byte[] newBytes = new byte[BitIndexToByteIndex(other.TopBitIndex)];
                for (int i = 0; i < newBytes.Length; i++)
                    newBytes[i] = other._bytes[i];

                _bytes = newBytes;
                TopBitIndex = other.TopBitIndex;
            }
        }

        public Bits(byte[] existing, bool copy = true)
        {
            if (copy && existing != null)
            {
                _bytes = new byte[existing.Length];
                existing.CopyTo(_bytes, 0);
            }
            else
                _bytes = existing;
        }
    }
}