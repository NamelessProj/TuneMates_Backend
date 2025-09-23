using System.Security.Cryptography;
using System.Text;

namespace TuneMates_Backend.Utils
{
    public interface IEncryptionService
    {
        string Encrypt(string plaintext);
        string Decrypt(string ciphertext);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;

        private const int TagByteLength = 16; // 128 bits
        private const int NonceByteLength = 12; // 96 bits

        public EncryptionService(IConfiguration cfg)
        {
            var base64Key = cfg["Secrets:EncryptKey64"];
            if (string.IsNullOrEmpty(base64Key))
                throw new ArgumentNullException("Encryption key is not configured.");
            _key = Convert.FromBase64String(base64Key);
            if (_key.Length != 32) // AES-256 requires a 32-byte key (256 bits)
                throw new ArgumentException("Encryption key must be 32 bytes (256 bits) long.");
        }

        public string Encrypt(string plaintext)
        {
            using var aes = new AesGcm(_key);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceByteLength); // 96-bit nonce
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] cyphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[TagByteLength]; // 128-bit tag

            aes.Encrypt(nonce, plaintextBytes, cyphertext, tag);

            // Combine nonce, tag, and ciphertext for storage/transmission
            byte[] combined = new byte[nonce.Length + tag.Length + cyphertext.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(cyphertext, 0, combined, nonce.Length + tag.Length, cyphertext.Length);

            return Convert.ToBase64String(combined);
        }

        public string Decrypt(string cyphertextBase64)
        {
            // Decode the base64 string
            byte[] combined = Convert.FromBase64String(cyphertextBase64);
            byte[] nonce = combined.Take(NonceByteLength).ToArray();
            byte[] tag = combined.Skip(NonceByteLength).Take(TagByteLength).ToArray();
            byte[] cyphertext = combined.Skip(NonceByteLength + TagByteLength).ToArray();

            using var aes = new AesGcm(_key);
            byte[] plaintext = new byte[cyphertext.Length];
            aes.Decrypt(nonce, cyphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
    }
}