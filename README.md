# arbitrary-protobuf-net

Library to encode and decode arbitrary protobuf data.

## Example usage

## Notes

There's no guarantee of producing valid output since there's a lot of guesswork involved when it comes to LengthDelimited WireType.

Packed integers are returned as byte arrays. 

Start and end group WireTypes are not supported.

## Thanks

Special thanks goes to Omar Roth for his crystal-lang [protodec](https://github.com/omarroth/protodec) tool.
