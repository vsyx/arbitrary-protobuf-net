using System;
using System.Text;

namespace AProtobuf
{
    public class Util
    {
        // System.Convert Base64 convertion requires padding to be present
        public static byte[] FromBase64StringWithoutPadding(string str)
        {
            int padding = str.Length % 4;

            if (padding != 0)
            {
                str = new StringBuilder(str)
                    .Append('=', padding)
                    .ToString();
            }

            return Convert.FromBase64String(str);
        }
    }
}
