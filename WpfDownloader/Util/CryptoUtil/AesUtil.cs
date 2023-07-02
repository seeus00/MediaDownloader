using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WpfDownloader.Util.CryptoUtil
{
    public static class AesUtil
    {
        private static void DeriveKeyAndIv(byte[] passphrase, byte[] salt, int iterations, out byte[] key, out byte[] iv)
        {
            var hashList = new List<byte>();

            var preHashLength = passphrase.Length + (salt?.Length ?? 0);
            var preHash = new byte[preHashLength];

            Buffer.BlockCopy(passphrase, 0, preHash, 0, passphrase.Length);
            if (salt != null)
                Buffer.BlockCopy(salt, 0, preHash, passphrase.Length, salt.Length);

            var hash = MD5.Create();
            var currentHash = hash.ComputeHash(preHash);

            for (var i = 1; i < iterations; i++)
            {
                currentHash = hash.ComputeHash(currentHash);
            }

            hashList.AddRange(currentHash);

            while (hashList.Count < 48) // for 32-byte key and 16-byte iv
            {
                preHashLength = currentHash.Length + passphrase.Length + (salt?.Length ?? 0);
                preHash = new byte[preHashLength];

                Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                Buffer.BlockCopy(passphrase, 0, preHash, currentHash.Length, passphrase.Length);
                if (salt != null)
                    Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + passphrase.Length, salt.Length);

                currentHash = hash.ComputeHash(preHash);

                for (var i = 1; i < iterations; i++)
                {
                    currentHash = hash.ComputeHash(currentHash);
                }

                hashList.AddRange(currentHash);
            }

            hash.Clear();
            key = new byte[32];
            iv = new byte[16];
            hashList.CopyTo(0, key, 0, 32);
            hashList.CopyTo(32, iv, 0, 16);
        }


        public static string DecryptAes(string encryptedString, string passphrase)
        {
            // encryptedString is a base64-encoded string starting with "Salted__" followed by a 8-byte salt and the
            // actual ciphertext. Split them here to get the salted and the ciphertext
            var base64Bytes = Convert.FromBase64String(encryptedString);
            var saltBytes = base64Bytes[8..16];
            var cipherTextBytes = base64Bytes[16..];

            // get the byte array of the passphrase
            var passphraseBytes = Encoding.UTF8.GetBytes(passphrase);

            // derive the key and the iv from the passphrase and the salt, using 1 iteration
            // (cryptojs uses 1 iteration by default)
            DeriveKeyAndIv(passphraseBytes, saltBytes, 1, out var keyBytes, out var ivBytes);

            // create the AES decryptor
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            // here are the config that cryptojs uses by default
            // https://cryptojs.gitbook.io/docs/#ciphers
            aes.KeySize = 256;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            var decryptor = aes.CreateDecryptor(keyBytes, ivBytes);

            // example code on MSDN https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-5.0
            using var msDecrypt = new MemoryStream(cipherTextBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            // read the decrypted bytes from the decrypting stream and place them in a string.
            return srDecrypt.ReadToEnd();
        }
    }
}
