using System;
using System.Collections.Generic;

namespace BlobIO
{
    public class BlobIOOutputTest
    {
        public const int NumValues = 100;

        private static bool ReadObject(Bits bits, object previous, out object read)
        {
            if (previous is bool)
            {
                bool value;
                if (bits.TryReadBit(out value))
                {
                    read = value;
                    return (bool)read == (bool)previous;
                }
            }
            else if (previous is byte)
            {
                byte value;
                if (bits.TryReadByte(out value))
                {
                    read = value;
                    return (byte)read == (byte)previous;
                }
            }
            else if (previous is short)
            {
                short value;
                if (bits.TryReadShort(out value))
                {
                    read = value;
                    return (short)read == (short)previous;
                }
            }
            else if (previous is ushort)
            {
                ushort value;
                if (bits.TryReadUShort(out value))
                {
                    read = value;
                    return (ushort)read == (ushort)previous;
                }
            }
            else if (previous is int)
            {
                int value;
                if (bits.TryReadInt(out value))
                {
                    read = value;
                    return (int)read == (int)previous;
                }
            }
            else if (previous is float)
            {
                float value;
                if (bits.TryReadFloat(out value))
                {
                    read = value;
                    return (float)read == (float)previous;
                }
            }
            else if (previous is string)
            {
                string value;
                if (bits.TryReadString(out value))
                {
                    read = value;
                    return (string)read == (string)previous;
                }
            }
            else
                Console.WriteLine("!!! Unknown type: " + previous.GetType().ToString());


            read = null;
            return false;
        }

        public static void Main (string[] args)
        {
            var objects = new List<object>();

            Random random = new Random();

            Bits bits = new Bits();

            for (int j = 0; j < NumValues; j++)
            {
                bool bit = random.NextDouble() > 0.5;
                bits.WriteBit(bit);
                objects.Add(bit);

                byte b = (byte)(random.Next() % byte.MaxValue);
                bits.WriteByte(b);
                objects.Add(b);

                short s = (short)(random.Next() % short.MaxValue);
                bits.WriteShort(s);
                objects.Add(s);

                ushort us = (ushort)(random.Next() % ushort.MaxValue);
                bits.WriteUShort(us);
                objects.Add(us);

                int i = random.Next();
                bits.WriteInt(i);
                objects.Add(i);

                float f = (float)random.NextDouble();
                bits.WriteFloat(f);
                objects.Add(f);

                string str = "\"" + random.Next().ToString() + "\"";
                bits.WriteString(str);
                objects.Add(str);
            }

            Console.WriteLine(string.Format("Top index: {0} Current index: {1}", bits.TopBitIndex, bits.BitIndex));

            bits.SeekBits(0, Bits.SeekMode.Begin);

            Console.WriteLine(string.Format("Top index: {0} Current index: {1}", bits.TopBitIndex, bits.BitIndex));

            foreach (var previous in objects)
            {
                object read;
                
                Console.WriteLine("Index: " + bits.BitIndex);

                bool worked = ReadObject(bits, previous, out read);

                Console.WriteLine(string.Format("{0} {1} {2} {3}", worked ? "    " : "!!!!", previous, worked ? "==" : "!=", read));

                if (!worked)
                    return;
            }
        }
    }
}
