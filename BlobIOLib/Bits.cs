using System;

namespace BlobIO
{
    public class Bits
    {
        public const int FloatRounding = 100;
        public const int IntSizeInBits = 32;

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

        public static float BytesToRoundedFloat(byte a, byte b, byte c, byte d) { return BytesToInt(a, b, c, d)/(float)FloatRounding; }
        public static void ConvertRoundedFloatToBytes(float f, out byte a, out byte b, out byte c, out byte d) { ConvertIntToBytes((int)Math.Round(f*FloatRounding), out a, out b, out c, out d); }

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
        private int _topIndex;

        private int __realIndex;
        private int _index
        {
            get { return __realIndex; }
            set
            {
                __realIndex = value;
                if (__realIndex > _topIndex)
                    _topIndex = __realIndex;
            }
        }

        public void EnsureBitCapacity(int howManyBits)
        {
            EnsureByteCapacity(BitSizeToByteSize(howManyBits));
        }

        public void EnsureByteCapacity(int howManyBytes)
        {
            if (_bytes == null || howManyBytes < _bytes.Length)
            {
                byte[] newBytes = new byte[howManyBytes];

                if (_bytes != null)
                    _bytes.CopyTo(newBytes, 0);

                _bytes = newBytes;
            }
        }

        public Bits Trim()
        {
            if (_bytes != null)
            {
                byte[] newBytes = new byte[BitSizeToByteSize(_topIndex)];
                _bytes.CopyTo(newBytes, 0);
                _bytes = newBytes;
            }
            return this;
        }

        public Bits WriteBit(bool state)
        {
            EnsureBitCapacity(_index + 1);
            return WriteBitInternal(state);
        }

        private Bits WriteBitInternal(bool state)
        {
            int index = (_index++) >> 3;
            _bytes[index] = (byte)Set(_bytes[index], _index & 7, state);
            return this;
        }

        public Bits WriteByte(byte value)
        {
            EnsureBitCapacity(_index + 8);
            return WriteByteInternal(value);
        }

        private Bits WriteByteInternal(byte value)
        {
            if (IsByteAligned(_index))
            {
                _bytes[_index >> 3] = value;
                _index += 8;
            }
            else
            {
                for (int i = 0; i < 8; i++)
                    WriteBitInternal(Get(value, i));
            }
            return this;
        }

        public Bits WriteInt(int value)
        {
            EnsureBitCapacity(_index + IntSizeInBits);
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
            EnsureBitCapacity(_index + IntSizeInBits);
            return WriteFloatInternal(value);
        }

        private Bits WriteFloatInternal(float value)
        {
            WriteIntInternal((int)Math.Round(value*FloatRounding));
            return this;
        }

        public Bits WriteLong(long value)
        {
            EnsureBitCapacity(_index + IntSizeInBits*2);
            return WriteLongInternal(value);
        }

        private Bits WriteLongInternal(long value)
        {
            WriteIntInternal((int)(value >> 32));
            WriteIntInternal((int)(value & 0xFFFF));
            return this;
        }

        
        public Bits WritePartialNumber(int num, int bits)
        {
            if (bits > 0 && bits < IntSizeInBits)
            {
                EnsureBitCapacity(_index + bits);
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

        public Bits WriteCompressedInt(int num)
        {
            int abs = Math.Abs(num);

        }

        public Bits SkipPaddingBits()
        {
            _index = BitIndexToByteIndex(_index) << 3;
            return this;
        }

        public bool SeekBits(int howManyBits, SeekMode mode)
        {
            int newIndex = _index;
            switch (mode)
            {
                case SeekMode.Begin:
                    newIndex = howManyBits;
                    break;
                case SeekMode.Current:
                    newIndex += howManyBits;
                    break;
                case SeekMode.End:
                    newIndex = _topIndex - howManyBits;
                break;
            }

            if (newIndex < 0 || newIndex >= _bytes.Length)
                return false;

            //Modifying __realIndex instead of _index because we don't want to set the _topIndex unless we're incrementing _index during a write.
            __realIndex = newIndex;
            return false;
        }
        
        public bool SeekBytes(int howManyBytes, SeekMode seekMode) { return SeekBits(howManyBytes << 3, seekMode); }

        public bool ReadBit(out bool value)
        {
            if (_index < _topIndex)
            {
                value = Get(_bytes[_index >> 3], _index & 7);
                _index++;
                return true;
            }
            value = false;
            return false;
        }

        public bool ReadByte(out byte value)
        {
            if (_index <= _topIndex - 8)
            {
                if (IsByteAligned(_index))
                    value = _bytes[_index >> 3];
                else
                {
                    value = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        value = (byte)Set(value, i, Get(_bytes[_index >> 3], _index & 7));
                        _index++;
                    }
                }
                return true;
            }
            value = 0;
            return false;
        }

        public bool ReadInt(out int value)
        {
            if (_index <= _topIndex - IntSizeInBits)
            {
                byte a, b, c, d;
                ReadByte(out a);
                ReadByte(out b);
                ReadByte(out c);
                ReadByte(out d);

                value = BytesToInt(a, b, c, d);
                return true;
            }
            value = 0;
            return false;
        }

        public bool ReadFloat(out float value)
        {
            int inValue;
            if (ReadInt(out inValue))
            {
                value = inValue/(float)FloatRounding;
                return true;
            }
            value = 0;
            return false;
        }

        public bool ReadLong(out long value)
        {
            int a, b;

            value = 0;

            if (ReadInt(out a) && ReadInt(out b))
            {
                value |= (long)a << 32;
                value |= (long)b;
                return true;
            }
            return false;
        }

        public bool ReadPartialNumber(int bits, out int value)
        {
            value = 0;
            if (bits > 0 && bits < IntSizeInBits && _index < _topIndex - bits)
            {
                bool state;
                for (int i = bits - 1; i >= 0; i--)
                {
                    ReadBit(out state);
                    value = Set(value, IntSizeInBits - i, state);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a new Bits instance that uses a reference to this objects bytes array.
        /// Useful for recycling the internal array without resizing.
        /// </summary>
        /// <returns>New instance of Bits.</returns>
        public Bits ReleaseArray()
        {
            Bits newBits = new Bits(_bytes, false);
            _bytes = null;
            _index = 0;
            _topIndex = 0;
            return newBits;
        }

        public Bits Clear()
        {
            _bytes = null;
            _index = 0;
            _topIndex = 0;
            return this;
        }

        public Bits() {}

        public Bits(Bits other)
        {
            if (other._bytes != null)
            {
                byte[] newBytes = new byte[BitIndexToByteIndex(other._topIndex)];
                for (int i = 0; i < newBytes.Length; i++)
                    newBytes[i] = other._bytes[i];

                _bytes = newBytes;
                _topIndex = other._topIndex;
            }
        }

        public Bits(byte[] existing, bool copy = false)
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