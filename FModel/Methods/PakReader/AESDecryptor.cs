using System;
using System.IO;
using System.Security.Cryptography;

namespace PakReader
{
    class AESDecryptor
    {
        const int AES_KEYBITS = 256;
        const int KEY_LENGTH = AES_KEYBITS / 8;

        public static byte[] DecryptAES(byte[] data, int size, byte[] key, int keyLen)
        {
            if (keyLen <= 0)
            {
                keyLen = key.Length;
            }

            if (keyLen == 0)
            {
                throw new ArgumentOutOfRangeException("Trying to decrypt AES block without providing an AES key");
            }
            if (keyLen < KEY_LENGTH)
            {
                throw new ArgumentOutOfRangeException("AES key is too short");
            }

            if ((size & 15) != 0)
            {
                throw new ArgumentOutOfRangeException("Size is invalid");
            }

            byte[] ret = new byte[data.Length];
            using (Rijndael cipher = Rijndael.Create())
            {
                cipher.Mode = CipherMode.ECB;
                cipher.Padding = PaddingMode.Zeros;
                cipher.Key = key;
                cipher.BlockSize = 16 * 8;
                using (var crypto = cipher.CreateDecryptor())
                using (MemoryStream msDecrypt = new MemoryStream(data))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, crypto, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    csDecrypt.Read(ret, 0, ret.Length);
            }
            return ret;
        }
    }
}
