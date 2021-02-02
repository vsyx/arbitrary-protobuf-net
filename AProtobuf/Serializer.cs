using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;

namespace AProtobuf
{
    public class Serializer
    {
        public enum Tag {
            Varint = 0,
            Bit64 = 1,
            LengthDelimited = 2,
            StartGroup = 3,
            EndGroup = 4,
            Bit32 = 5
        }

        private static UTF8Encoding UTF8 = new UTF8Encoding(false, true);
        public static Hashtable SerializeAsHashtable(MemoryStream ms)
        {
            Hashtable dictionary = new Hashtable();

            while (ms.Position != ms.Length)
            {
                long header;
                try
                {
                    header = Varint.GetLong(ms);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.Message);
                    continue;
                }

                long fieldNumber = (long)header >> 3;
                Tag wireType = (Tag)(header & 0x7);

                string fieldStr = fieldNumber.ToString();

                switch (wireType)
                {
                    case Tag.Varint:
                        dictionary[$"{fieldStr}:varint"] = Varint.GetLong(ms);
                        break;
                    case Tag.Bit32:
                        Span<byte> buf32 = new byte[4];
                        ms.Read(buf32);

                        try {
                            dictionary[$"{fieldStr}:float32"] = BitConverter.ToSingle(buf32);
                        } 
                        catch 
                        {
                            dictionary[$"{fieldStr}:int32"] = BitConverter.ToInt32(buf32);
                        }
                        break;
                    case Tag.Bit64:
                        Span<byte> buf64 = new byte[8];
                        ms.Read(buf64);

                        try {
                            dictionary[$"{fieldStr}:float64"] = BitConverter.ToDouble(buf64);
                        } 
                        catch 
                        {
                            dictionary[$"{fieldStr}:int64"] = BitConverter.ToInt64(buf64);
                        }
                        break;
                    case Tag.LengthDelimited:
                        const int MaxDelimitedLength = 1 << 22; // Arbritrary

                        var length = Varint.GetLong(ms);

                        if (length > MaxDelimitedLength)
                        {
                            throw new Exception("Invalid delimited length");
                        }

                        byte[] buf = new byte[length];
                        ms.Read(buf);

                        // no checks for repeated elements
                        try
                        {
                            string bufStr = UTF8.GetString(buf);

                            if (bufStr.Length == 0)
                            {
                                dictionary[$"{fieldStr}:string"] = String.Empty;
                                break;
                            }

                            // this could still be anything
                            try // base64
                            {
                                var payload = Util.FromBase64StringWithoutPadding(HttpUtility.UrlDecode(bufStr));

                                using var innerMs = new MemoryStream(payload);
                                dictionary[$"{fieldStr}:base64"] = SerializeAsHashtable(innerMs);
                            }
                            catch 
                            {
                                try // embedded
                                {
                                    using var innerMs = new MemoryStream(buf);
                                    dictionary[$"{fieldStr}:embedded"] = SerializeAsHashtable(innerMs);
                                }
                                catch
                                {
                                    dictionary[$"{fieldStr}:string"] = bufStr;
                                }
                            }
                        }
                        catch (ArgumentException) // bad utf8
                        {
                            try // embedded
                            {
                                using var innerMs = new MemoryStream(buf);
                                dictionary[$"{fieldStr}:embedded"] = SerializeAsHashtable(innerMs);
                            }
                            catch // bytes
                            {
                                dictionary[$"{fieldStr}:bytes"] = buf;
                            }
                        }
                        break;
                    default:
                        throw new Exception($"Couldn't match wiretype : {wireType}");
                }
            }
            return dictionary;
        }

        public static readonly Dictionary<string, long> TagMap = new Dictionary<string, long>
        {
            {"varint", 0 },
            {"float32", 5 },
            {"int32", 5 },
            {"float64", 1 },
            {"int64", 1 },
            {"string", 2 },
            {"embedded", 2 },
            {"base64", 2 },
            {"bytes", 2 },
        };

        public static byte[] Deserialize(Hashtable protobuf)
        {
            using var ms = new MemoryStream();
            Deserialize(ms, protobuf);
            return ms.ToArray();
        }

        public static void Deserialize(MemoryStream ms, Hashtable protobuf)
        {
            foreach (DictionaryEntry element in protobuf)
            {
                var parts = element.Key.ToString().Split(':');
                var fieldNumber = Convert.ToInt64(parts[0]);
                var type = parts[1];

                Console.WriteLine(fieldNumber);

                var header = (fieldNumber << 3) | TagMap[type];
                Varint.Write(ms, header);

                switch (type)
                {
                    case "varint":
                        Varint.Write(ms, (long)element.Value);
                        break;
                    case "int32":
                        ms.Write(BitConverter.GetBytes((int)element.Value));
                        break;
                    case "float32":
                        ms.Write(BitConverter.GetBytes((float)element.Value));
                        break;
                    case "int64":
                        ms.Write(BitConverter.GetBytes((long)element.Value));
                        break;
                    case "string":
                        string str = (string)element.Value;
                        Varint.Write(ms, str.Length);
                        ms.Write(Encoding.UTF8.GetBytes(str));
                        break;
                    case "base64":
                        {
                            using var base64ms = new MemoryStream();
                            Deserialize(base64ms, (Hashtable)element.Value);
                            base64ms.Position = 0;

                            var base64Buffer = Encoding.UTF8.GetBytes(Convert.ToBase64String(base64ms.ToArray()));
                            Varint.Write(ms, base64Buffer.Length);

                            ms.Write(base64Buffer);
                            break;
                        }
                    case "embedded":
                        {
                            using var embeddedMs = new MemoryStream();
                            Deserialize(embeddedMs, (Hashtable)element.Value);
                            embeddedMs.Position = 0;

                            Varint.Write(ms, (long)embeddedMs.Length);
                            embeddedMs.CopyTo(ms);
                            break;
                        }
                    case "bytes":
                        var buffer = (byte[])element.Value;
                        Varint.Write(ms, buffer.Length);
                        ms.Write(buffer);
                        break;
                    default:
                        throw new Exception("Unknown wireType");
                }
            }
        }
    }
}
