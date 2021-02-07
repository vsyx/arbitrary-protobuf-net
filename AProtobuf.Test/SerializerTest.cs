using System.IO;
using NeoSmart.Utils;
using NUnit.Framework;

namespace AProtobuf.Test
{
    public class SerializerTest
    {
        private const string ProtoBufferUrlSafeBase64 = "CLlgEgVTdGV2ZRIFU21pdGgaFQoGRmxhdCAxEgtUaGUgTWVhZG93cyIcQ2daR2JHRjBJREVTQzFSb1pTQk5aV0ZrYjNkeioFGIABjAQ";

        [Test]
        public void SerializeAndDeserializeEqualityTest()
        {
            var payload = UrlBase64.Decode(ProtoBufferUrlSafeBase64);
            using var ms = new MemoryStream(payload);

            var dictionaryOriginal = AProtobuf.Serializer.SerializeAsOrderedDictionary(ms);

            var payloadDeserialized = UrlBase64.Encode(AProtobuf.Serializer.Deserialize(dictionaryOriginal));

            Assert.AreEqual(ProtoBufferUrlSafeBase64, payloadDeserialized);
        }
    }
}
