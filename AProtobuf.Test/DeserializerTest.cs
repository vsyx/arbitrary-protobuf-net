using System.Collections.Specialized;
using System.IO;
using NUnit.Framework;

namespace AProtobuf.Test
{
    public class DeserializerTest
    {
        [Test]
        // Varint always expects long when deserializing, check for valid convertion
        public void TestNumericIDictionaryDeserialize()
        {
            var table = new OrderedDictionary();
            using var ms = new MemoryStream();

            table["1:varint"] = (int)0; 

            Assert.DoesNotThrow(() => 
            {
                AProtobuf.Serializer.Deserialize(ms, table);
                ms.Position = 0;
            });
        }
    }
}