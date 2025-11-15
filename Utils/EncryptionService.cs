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

        private const int TagByteSize = 16; // 128 bits
        private const int NonceByteSize = 12; // 96 bits

        /// <summary>
        /// Initializes the <see cref="EncryptionService"/> with a base64-encoded key from <paramref name="cfg"/> (<see cref="IConfiguration"/>).
        /// </summary>
        /// <param name="cfg">^The configuration containing the encryption key.</param>
        /// <exception cref="ArgumentNullException">Thrown if the encryption key is not configured.</exception>
        /// <exception cref="ArgumentException">Thrown if the encryption key is not 32 bytes (256 bits) long.</exception>
        public EncryptionService(IConfiguration cfg)
        {
            var base64Key = cfg["Secrets:EncryptKey64"];
            if (string.IsNullOrWhiteSpace(base64Key))
                throw new ArgumentNullException("Encryption key is not configured.");
            _key = Convert.FromBase64String(base64Key);
            int actualBytes = _key.Length;
            if (actualBytes != 32) // AES-256 requires a 32-byte key (256 bits)
                throw new ArgumentException($"Encryption key must be 32 bytes (256 bits) long. Current length: {actualBytes} bytes.");
        }

        /// <summary>
        /// Encrypts the given plaintext using AES-GCM and returns a base64-encoded string containing the nonce, tag, and ciphertext.
        /// </summary>
        /// <param name="plaintext">The plaintext to encrypt.</param>
        /// <returns>A base64-encoded string containing the nonce, tag, and ciphertext.</returns>
        public string Encrypt(string plaintext)
        {
            using AesGcm aes = new(_key, TagByteSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceByteSize); // 96-bit nonce
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] cyphertext = new byte[plaintextBytes.Length];
            byte[] tag = new byte[TagByteSize]; // 128-bit tag

            aes.Encrypt(nonce, plaintextBytes, cyphertext, tag);

            // Combine nonce, tag, and ciphertext for storage/transmission
            byte[] combined = new byte[nonce.Length + tag.Length + cyphertext.Length];
            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, combined, nonce.Length, tag.Length);
            Buffer.BlockCopy(cyphertext, 0, combined, nonce.Length + tag.Length, cyphertext.Length);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Decrypts the given base64-encoded string containing the nonce, tag, and ciphertext using AES-GCM and returns the plaintext.
        /// </summary>
        /// <param name="cyphertextBase64">The base64-encoded string containing the nonce, tag, and ciphertext.</param>
        /// <returns>The decrypted plaintext.</returns>
        public string Decrypt(string cyphertextBase64)
        {
            // Decode the base64 string
            byte[] combined = Convert.FromBase64String(cyphertextBase64);
            byte[] nonce = combined.Take(NonceByteSize).ToArray();
            byte[] tag = combined.Skip(NonceByteSize).Take(TagByteSize).ToArray();
            byte[] cyphertext = combined.Skip(NonceByteSize + TagByteSize).ToArray();

            using AesGcm aes = new(_key, TagByteSize);
            byte[] plaintext = new byte[cyphertext.Length];
            aes.Decrypt(nonce, cyphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
    }
}