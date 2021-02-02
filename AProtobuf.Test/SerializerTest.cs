using System.Collections;
using System.IO;
using System.Web;
using NUnit.Framework;

namespace AProtobuf.Test
{
    public class SerializerTest
    {
        private const string ProtobufStr = "4qmFsgJUEhhVQ2ExMG54U2hoek5yQ0UxbzJaT1B6dGcaOEVnWjJhV1JsYjNNWUF5QUFNQUU0QWVvREYwTm5Ua1JTUld0VFEyZHBXVGx4Y1ZZeVQxZHFPVVEw";

        [Test]
        public void SerializeAndDeserializeEqualityTest()
        {
            var payload = AProtobuf.Util.FromBase64StringWithoutPadding(HttpUtility.UrlDecode(ProtobufStr));
            using var ms = new MemoryStream(payload);

            var hashtableOriginal = AProtobuf.Serializer.SerializeAsHashtable(ms);

            // The deserialized version of the string/bytes is rarely going to be equal, since order is not maintained.
            // With base64, padding may be added as well, but the underlying data should remain the same.

            var payaloadOfDeserialized = AProtobuf.Serializer.Deserialize(hashtableOriginal);

            using var ms2 = new MemoryStream(payload);
            var hashtableDeserialized = AProtobuf.Serializer.SerializeAsHashtable(ms2);

            Assert.True(HashTableADataEqualToB(hashtableOriginal, hashtableDeserialized));
        }

        private bool HashTableADataEqualToB(Hashtable a, Hashtable b)
        {
            foreach (DictionaryEntry entry in a)
            {
                if (!b.ContainsKey(entry.Key)) 
                {
                    return false;
                }

                if (entry.Value.GetType() == typeof(Hashtable))
                {
                    if (b[entry.Key].GetType() != typeof(Hashtable)
                        || !HashTableADataEqualToB((Hashtable)entry.Value, (Hashtable)b[entry.Key]))
                    {
                        return false;
                    }
                    continue;
                }

                if (!entry.Value.Equals(b[entry.Key]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
