using System;
using System.IO;

namespace AProtobuf
{
    public class Varint
    {
        const int MaxVarintBytesRead = 10; // arbitrary

        public static long GetLong(MemoryStream ms)
        {
            long result = 0;
            int bytesRead = 0;

            while (true)
            {
                int readByte = ms.ReadByte();
                if (readByte == -1)
                {
                    throw new Exception("End of stream");
                }
                int value = readByte & 0x7f; // discard MSB

                result |= ((long)value << 7 * bytesRead);
                bytesRead++;

                if ((readByte & 0x80) == 0) // continue if MSB is 1
                {
                    break;
                }

                if (bytesRead > MaxVarintBytesRead)
                {
                    throw new Exception("Invalid Varint");
                }
            }

            return result;
        }

        public static void Write(MemoryStream ms, long value)
        {
            if (value == 0)
            {
                ms.WriteByte(0);
                return;
            }

            var uValue = (ulong)value;
            bool done = uValue == 0;

            do
            {
                byte b = (byte)(uValue & 0x7f);
                uValue >>= 7;

                done = uValue == 0;

                if (!done)
                {
                    b |= 0x80; // set msb to 1 (i.e. more digits to come)
                }

                ms.WriteByte(b);
            } while (!done);
        }
    }
}
