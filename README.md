# arbitrary-protobuf-net

Library to encode and decode arbitrary protobuf data.

## Example usage

```csharp
// Serialize
var payload = "CLlgEgMNyAMaFQoGRmxhdCAxEgtUaGUgTWVhZG93cw==";
var data = AProtobuf.Util.FromBase64StringWithoutPadding(payload);
using var ms = new MemoryStream(data);

Hashtable dict = AProtobuf.Serializer.SerializeAsHashtable(ms);

Console.WriteLine(JsonSerializer.Serialize(dict));
// stdout: {"3:2:embedded":{"1:0:string":"Flat 1","2:1:string":"The Meadows"},"1:0:varint":12345,"2:1:bytes":"DcgD"}

// deserialize
using var msOut = new MemoryStream(data);
AProtobuf.Serializer.Deserialize(msOut, dict);

Console.WriteLine(Encoding.UTF8.GetString(msOut.ToArray()));
// stdout: EgMNyAMaFQoGRmxhdCAxEgtUaGUgTWVhZG93cwi5YA== 
```

## Notes


There's no guarantee of producing valid output since there's a lot of guesswork involved when it comes to LengthDelimited WireType.

Since Hashtables are used under the hood, order is not maintained and hence a serialize -> deserialize pipeline will most likely produce different bytes (but the data should remain the same). The order can be deduced from the key since it's structured as <fieldNumber:index:type>, however the index is *not* taken in consideration when deserializating.

Additional padding may be added for previously unpadded base64 strings. 

Packed integers are returned as byte arrays. 

Start and end group WireTypes are not supported.

## Thanks

Special thanks goes to Omar Roth for his crystal-lang [protodec](https://github.com/omarroth/protodec) tool which provided a solid base to work off.
