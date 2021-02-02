using System;
using System.Text;

namespace AProtobuf
{
    public class Util
    {
        // System.Convert Base64 convertion requires padding to be present
        public static byte[] FromBase64StringWithoutPadding(string str)
        {
            int rem = str.Length % 4;

            if (rem == 1)
            {
                throw new Exception("Impossible base64 padding");
            }

            if (rem != 0)
            {
                var paddingCount = 4 - rem;

                str = new StringBuilder(str)
                    .Append('=', paddingCount)
                    .ToString();
            }

            return Convert.FromBase64String(str);
        }
    }
}
