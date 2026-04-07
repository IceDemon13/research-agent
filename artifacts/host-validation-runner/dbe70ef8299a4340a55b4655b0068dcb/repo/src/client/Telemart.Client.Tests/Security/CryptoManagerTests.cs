using System.Threading.Tasks;
using Telemart.Client.Core.Security;
using Xunit;

//// ReSharper disable ExceptionNotDocumented

namespace Telemart.Client.Tests.Security
{
    public class CryptoManagerTests
    {
        private readonly ICryptoManager cryptoManager = new CryptoManager();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task DecryptNullOrEmptyAsync(string s)
        {
            string plainText = await cryptoManager.DecryptAsync(s);

            Assert.Equal(s, plainText);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task EncryptNullOrEmptyAsync(string s)
        {
            string cipherText = await cryptoManager.EncryptAsync(s);

            Assert.Equal(s, cipherText);
        }

        [Fact]
        public async Task EcryptionTestAsync()
        {
            string plainText = "some text to encrypt";

            string chipherText = await cryptoManager.EncryptAsync(plainText);
            string text = await cryptoManager.DecryptAsync(chipherText);

            Assert.Equal(plainText, text);
        }
    }
}