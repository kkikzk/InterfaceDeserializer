using System.IO;
using System.Security.Cryptography;

namespace InterfaceDeserializer
{
    internal class Encrypter
    {
        private static readonly byte[] _key = {
            0xED, 0x0B, 0x56, 0xAF, 0x61, 0xA2, 0x71, 0x39,
            0xE0, 0x4B, 0xDC, 0xC9, 0x23, 0x69, 0x8C, 0xBD,
            0xB9, 0x86, 0x98, 0x28, 0xC8, 0x3E, 0x62, 0xA7,
            0xFA, 0x17, 0xC1, 0x33, 0x64, 0xBF, 0x96, 0x24
        };

        private static readonly byte[] _iv = {
            0x6F, 0xDF, 0x98, 0x00, 0x67, 0x36, 0x7D, 0x3B,
            0xFF, 0xC9, 0x3B, 0x79, 0x4D, 0xD4, 0x81, 0x72
        };

        internal static byte[] Encrypt(byte[] src)
        {
            return Encrypt(src, _key, _iv);
        }

        internal static byte[] Encrypt(byte[] src, byte[] key, byte[] iv)
        {
            using var am = new AesManaged();
            using var encryptor = am.CreateEncryptor(key, iv);
            using var outStream = new MemoryStream();
            using var cs = new CryptoStream(outStream, encryptor, CryptoStreamMode.Write);
            cs.Write(src, 0, src.Length);
            return outStream.ToArray();
        }

        internal static byte[] Decrypt(byte[] src)
        {
            return Decrypt(src, _key, _iv);
        }

        internal static byte[] Decrypt(byte[] src, byte[] key, byte[] iv)
        {
            using var am = new AesManaged();
            using var decryptor = am.CreateDecryptor(key, iv);
            using var inStream = new MemoryStream(src, false);
            using (var outStream = new MemoryStream())
            {
                using (var cs = new CryptoStream(inStream, decryptor, CryptoStreamMode.Read))
                {
                    var buffer = new byte[4096];
                    var len = 0;
                    while ((len = cs.Read(buffer, 0, 4096)) > 0)
                    {
                        outStream.Write(buffer, 0, len);
                    }
                }
                return outStream.ToArray();
            }
        }
    }
}
