# arbitrary-protobuf-net

Library to encode and decode arbitrary protobuf data.

# Installation

```sh
dotnet add package arbitrary-protobuf-net
```

## Example usage

Serializing
```csharp
var payload = UrlBase64.Decode("CLlgEgVTdGV2ZRIFU21pdGgaFQoGRmxhdCAxEgtUaGUgTWVhZG93cyIcQ2daR2JHRjBJREVTQzFSb1pTQk5aV0ZrYjNkeioFGIABjAQ"); // UrlBase64 is a nuget package
using var ms = new MemoryStream(payload);

OrderedDictionary dictionary = AProtobuf.Serializer.SerializeAsOrderedDictionary(ms);
// or IDictionary dict = Serializer.Serialize(ms, () => new ClassThatImplementsIDictionary());
```

Deserializing

```csharp
// IDictionary
byte[] iDictionaryDeserialized = AProtobuf.Serializer.Deserialize(dictionary);

// JsonElement
var json = JsonSerializer.Serialize(dictionary);
using var jsonDoc = JsonDocument.Parse(json);
byte[] jsonDeserialized = AProtobuf.Serializer.Deserialize(jsonDoc.RootElement);
```

Example object

```csharp
new Person()
{
    Id = 12345, 
    Name = new string[] { "Steve", "Smith" }, 
    Address = new Address
    {
        Line1 = "Flat 1",
        Line2 = "The Meadows"
    },
    Base64Address = "CgZGbGF0IDESC1RoZSBNZWFkb3dz", // same as Address above, but already encoded
    IntArray = new int[] { 24, 128, 524 } // packed
};
```
Person -> OrderedDictionary -> Json

```json
{
    "1:0:varint": 12345,
    "2:1:string": "Steve", 
    "2:2:string": "Smith",
    "3:3:embedded": {
        "1:0:string": "Flat 1",
        "2:1:string": "The Meadows"
    },
    "4:4:base64": {
        "1:0:string": "Flat 1",
        "2:1:string": "The Meadows"
    },
    "5:5:bytes": "GIABjAQ="
}
```

## Notes

There's no guarantee of producing valid output since there's a lot of guesswork involved when it comes to the LengthDelimited WireType. Additional padding may be added for previously unpadded base64 strings. 

Base64-like strings are expanded as embedded objects (assuming that no exceptions are thrown). When deserializing, Base64 is url safe encoded.

Packed integers are returned as byte arrays. 

Start and end group WireTypes are not supported.

## Thanks

Special thanks goes to Omar Roth for his crystal-lang [protodec](https://github.com/omarroth/protodec) tool which provided a solid base to work off.
