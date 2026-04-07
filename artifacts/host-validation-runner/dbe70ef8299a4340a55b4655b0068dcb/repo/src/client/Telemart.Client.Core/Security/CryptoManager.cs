using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Telemart.Client.Core.Security
{
    public sealed class CryptoManager : ICryptoManager
    {
        private const string PassPhrase = "UiJlGMH9";

        // This size of the IV (in bytes) must = (keysize / 8)
        // Default keysize is 256, so the IV must be 32 bytes long
        // Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private const string InitVector = "lbWSx9rzbXYreWAA";

        // This constant is used to determine the keysize of the encryption algorithm
        private const int KeySize = 256;

        public async Task<string> DecryptAsync(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return cipherText;
            }

            using (RijndaelManaged symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC })
            {
                PasswordDeriveBytes password = new PasswordDeriveBytes(PassPhrase, null);

                byte[] initVectorBytes = Encoding.UTF8.GetBytes(InitVector);
                byte[] keyBytes = password.GetBytes(KeySize / 8);

                using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                {
                    byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

                    using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            byte[] plainTextBytes = new byte[cipherTextBytes.Length];

                            StringBuilder sb = new StringBuilder();
                            int readed = 0;
                            int offset = 0;
                            do
                            {
                                readed = await cryptoStream.ReadAsync(plainTextBytes, 0, plainTextBytes.Length);

                                offset += readed;

                                sb.Append(Encoding.UTF8.GetString(plainTextBytes, 0, readed));
                            }
                            while (readed > 0);

                            return sb.ToString();
                        }
                    }
                }
            }
        }

        public async Task<string> EncryptAsync(string clearText)
        {
            if (string.IsNullOrEmpty(clearText))
            {
                return clearText;
            }

            using (RijndaelManaged rijndaelManaged = new RijndaelManaged { Mode = CipherMode.CBC })
            {
                PasswordDeriveBytes password = new PasswordDeriveBytes(PassPhrase, null);

                byte[] initVectorBytes = Encoding.UTF8.GetBytes(InitVector);
                byte[] keyBytes = password.GetBytes(KeySize / 8);

                using (ICryptoTransform encryptor = rijndaelManaged.CreateEncryptor(keyBytes, initVectorBytes))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            byte[] plainTextBytes = Encoding.UTF8.GetBytes(clearText);

                            await cryptoStream.WriteAsync(plainTextBytes, 0, plainTextBytes.Length);
                            cryptoStream.FlushFinalBlock();

                            byte[] cipherTextBytes = memoryStream.ToArray();

                            return Convert.ToBase64String(cipherTextBytes);
                        }
                    }
                }
            }
        }
    }
}