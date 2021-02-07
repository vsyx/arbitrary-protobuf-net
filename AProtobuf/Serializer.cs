using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Web;
using NeoSmart.Utils;
using System.Collections.Specialized;

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
        public static IDictionary Serialize(
            MemoryStream ms,
            Func<IDictionary> dictionaryConstructor,
            bool TryDecodeBase64 = true)
        {
            IDictionary dictionary = dictionaryConstructor();
            int index = 0; // needed for repeated elements (assuming that you want to keep within a dict)

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

                string fieldStr = fieldNumber.ToString() + ":" + index.ToString();

                switch (wireType)
                {
                    case Tag.Varint:
                        dictionary[$"{fieldStr}:varint"] = Varint.GetLong(ms);
                        break;
                    case Tag.Bit32:
                        Span<byte> buf32 = new byte[4];
                        if (ms.Read(buf32) != buf32.Length)
                        {
                            throw new Exception("Invalid Bit32 length");
                        }

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
                        if (ms.Read(buf64) != buf64.Length)
                        {
                            throw new Exception("Invalid Bit64 length");
                        }

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
                        if (ms.Read(buf, 0, (int)length) != length)
                        {
                            throw new Exception("Malformed LengthDelimited");
                        }

                        // no checks for repeated elements
                        try
                        {
                            string bufStr = UTF8.GetString(buf);

                            if (bufStr.Length == 0)
                            {
                                dictionary[$"{fieldStr}:string"] = String.Empty;
                                break;
                            }
                            if (!ContainsNonFeedControlCharacters(buf))
                            {
                                if (TryDecodeBase64)
                                {
                                    try
                                    {
                                        var payload = UrlBase64.Decode(HttpUtility.UrlDecode(bufStr));
                                        using var innerMs = new MemoryStream(payload);
                                        dictionary[$"{fieldStr}:base64"] = Serialize(innerMs, dictionaryConstructor);
                                        break;
                                    }
                                    catch { }
                                }
                                // assume that it's a valid string, either way you can always handle it manually
                                dictionary[$"{fieldStr}:string"] = bufStr;
                                break;
                            }
                        }
                        catch (ArgumentException) {  } // bad utf8

                        try 
                        {
                            using var innerMs = new MemoryStream(buf);
                            dictionary[$"{fieldStr}:embedded"] = Serialize(innerMs, dictionaryConstructor);
                        }
                        catch 
                        {
                            dictionary[$"{fieldStr}:bytes"] = buf;
                        }
                        break;
                    default:
                        throw new Exception($"Couldn't match wiretype : {wireType}");
                }
                index++;
            }
            return dictionary;
        }

        public static OrderedDictionary SerializeAsOrderedDictionary(MemoryStream ms)
        {
            return (OrderedDictionary)Serialize(ms, () => new OrderedDictionary());
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

        public static byte[] Deserialize(IDictionary protobuf)
        {
            using var ms = new MemoryStream();
            Deserialize(ms, protobuf);
            return ms.ToArray();
        }

        public static void Deserialize(MemoryStream ms, IDictionary protobuf)
        {
            foreach (DictionaryEntry element in protobuf)
            {
                var parts = element.Key.ToString().Split(':');
                var fieldNumber = Convert.ToInt64(parts[0]);
                var type = parts[parts.Length - 1];

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
                    case "float64":
                        ms.Write(BitConverter.GetBytes((double)element.Value));
                        break;
                    case "string":
                        string str = (string)element.Value;
                        Varint.Write(ms, str.Length);
                        ms.Write(Encoding.UTF8.GetBytes(str));
                        break;
                    case "base64":
                        {
                            using var base64ms = new MemoryStream();
                            Deserialize(base64ms, (IDictionary)element.Value);
                            base64ms.Position = 0;

                            var base64Buffer = Encoding.UTF8.GetBytes(Convert.ToBase64String(base64ms.ToArray()));
                            Varint.Write(ms, base64Buffer.Length);

                            ms.Write(base64Buffer);
                            break;
                        }
                    case "embedded":
                        {
                            using var embeddedMs = new MemoryStream();
                            Deserialize(embeddedMs, (IDictionary)element.Value);
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
                        throw new Exception("Unknown WireType");
                }
            }
        }

        public static byte[] Deserialize(JsonElement json)
        {
            using var ms = new MemoryStream();
            Deserialize(ms, json);
            return ms.ToArray();
        }

        public static void Deserialize(MemoryStream ms, JsonElement json)
        {
            var enumerate = json.EnumerateObject();

            while (enumerate.MoveNext())
            {
                var element = enumerate.Current;

                var parts = element.Name.ToString().Split(':');
                var fieldNumber = Convert.ToInt64(parts[0]);
                var type = parts[parts.Length - 1];

                var header = (fieldNumber << 3) | TagMap[type];
                Varint.Write(ms, header);

                switch (type)
                {
                    case "varint":
                        Varint.Write(ms, element.Value.GetInt64());
                        break;
                    case "int32":
                        ms.Write(BitConverter.GetBytes(element.Value.GetInt32()));
                        break;
                    case "float32":
                        ms.Write(BitConverter.GetBytes(element.Value.GetSingle()));
                        break;
                    case "int64":
                        ms.Write(BitConverter.GetBytes(element.Value.GetInt64()));
                        break;
                    case "float64":
                        ms.Write(BitConverter.GetBytes(element.Value.GetDouble()));
                        break;
                    case "string":
                        string str = element.Value.GetString();
                        Varint.Write(ms, str.Length);
                        ms.Write(Encoding.UTF8.GetBytes(str));
                        break;
                    case "base64":  
                        {
                            switch (element.Value.ValueKind)
                            {
                                case JsonValueKind.Object: 
                                {
                                    using var base64ms = new MemoryStream();
                                    Deserialize(base64ms, element.Value);

                                    var buf = Encoding.UTF8.GetBytes(UrlBase64.Encode(base64ms.ToArray()));
                                    Varint.Write(ms, buf.Length);

                                    ms.Write(buf);
                                    break;
                                }
                                case JsonValueKind.String:
                                    string base64str = element.Value.GetString();
                                    Varint.Write(ms, base64str.Length);
                                    ms.Write(Encoding.UTF8.GetBytes(base64str));
                                    break;
                            }
                            break;
                        }
                    case "embedded":
                        {
                            using var embeddedMs = new MemoryStream();
                            Deserialize(embeddedMs, element.Value);

                            Varint.Write(ms, embeddedMs.Length);

                            embeddedMs.WriteTo(ms);
                            break;
                        }
                    case "bytes": 
                        var buffer = element.Value.GetBytesFromBase64();
                        Varint.Write(ms, buffer.Length);
                        ms.Write(buffer);
                        break;
                    default:
                        throw new Exception("Unknown WireType");
                }
            }
        }

        private static bool ContainsNonFeedControlCharacters(byte[] buffer)
        {
            foreach (var b in buffer)
            {
                if (b >= 0x0 && b <= 0x1f)
                {
                    // feed
                    if (b == 0x09 || b == 0x0a || b == 0x0d)
                    {
                        continue;
                    }
                    return true;
                }
            }

            return false;
        }
    }
}
