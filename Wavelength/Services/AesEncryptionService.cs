using System.Security.Cryptography;

namespace Wavelength.Services
{
	/// <summary>
	/// Provides AES-based encryption and decryption services using a configurable encryption key.
	/// </summary>
	/// <remarks>This service uses the Advanced Encryption Standard (AES) algorithm to securely encrypt and decrypt
	/// string data. The encryption key must be supplied via configuration as a Base64-encoded string. The service is
	/// intended for scenarios where symmetric encryption is required for sensitive data. Thread safety is not guaranteed
	/// if the underlying key is modified concurrently.</remarks>
	public class AesEncryptionService
	{
		private readonly byte[] _key;

		/// <summary>
		/// Initializes a new instance of the AesEncryptionService class using the specified configuration settings.
		/// </summary>
		/// <remarks>The encryption key is expected to be stored in the configuration as a Base64-encoded string under
		/// the key 'EncryptionKey'. The key is converted to a byte array for use with AES encryption.</remarks>
		/// <param name="config">The configuration source containing the encryption key. The key must be provided as a Base64-encoded string with
		/// the key 'EncryptionKey'.</param>
		/// <exception cref="ArgumentNullException">Thrown if the 'EncryptionKey' configuration value is not set.</exception>
		public AesEncryptionService(IConfiguration config)
		{
			var key = config["Aes:Key"];
			if (key is null)
				throw new ArgumentNullException("EncryptionKey", "Encryption key is not configured.");

			// Load the encryption key from configuration (Base64 encoded string)
			// Convert it into a byte array, since AES requires raw bytes
			_key = Convert.FromBase64String(key);
		}

		/// <summary>
		/// Encrypts the specified plaintext string using AES encryption and returns the result as a Base64-encoded string.
		/// </summary>
		/// <remarks>The initialization vector (IV) is generated randomly for each encryption and is included in the
		/// output. To decrypt the result, the IV must be extracted from the beginning of the Base64-decoded byte array. This
		/// method is not thread-safe if the underlying key is modified concurrently.</remarks>
		/// <param name="plainText">The plaintext string to encrypt. Cannot be null.</param>
		/// <returns>A Base64-encoded string containing the encrypted data and the initialization vector. The result can be decrypted
		/// using the corresponding decryption method.</returns>
		public string Encrypt(string plainText)
		{
			using var aes = Aes.Create();
			aes.Key = _key; // Use the configured key.
			aes.GenerateIV(); // Generate a new random IV for this encryption.
			// IV == Initialization Vector.

			// Create an encryptor with the key and IV.
			using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
			using var ms = new MemoryStream();

			// Write the IV at the beginning of the stream so we can retrive it later.
			ms.Write(aes.IV, 0, aes.IV.Length);

			// Create a crypto stream that will perform encryption.
			using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
			using (var sw = new StreamWriter(cs))
			{
				// Write the plaintext into the crypto stream.
				// It will be encrypted and written into the memory stream.
				sw.Write(plainText);
			}

			// Convert the final byte array (IV + ciphertext) into Base64 for storage.
			return Convert.ToBase64String(ms.ToArray());
		}

		/// <summary>
		/// Decrypts the specified cipher text using the configured AES key and returns the original plain text.
		/// </summary>
		/// <remarks>The method expects the cipher text to be a Base64-encoded string with the initialization vector
		/// (IV) included at the beginning. The same key and IV used for encryption must be available for successful
		/// decryption. If the input is not in the expected format or the key is incorrect, decryption will fail.</remarks>
		/// <param name="cipherText">The encrypted text to decrypt, encoded as a Base64 string. The input must contain the initialization vector (IV)
		/// prepended to the encrypted data.</param>
		/// <returns>The decrypted plain text string.</returns>
		public string Decrypt(string cipherText)
		{
			// Convert the Base64 string back into raw byters.
			var fullCipher = Convert.FromBase64String(cipherText);

			using var aes = Aes.Create();
			aes.Key = _key;

			// Extract the IV from the first bytes of the cupherText.
			var iv = new byte[aes.BlockSize / 8];
			Array.Copy(fullCipher, iv, iv.Length);
			aes.IV = iv;

			// Create a decryptor with the key and extracted IV.
			using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

			// The rest of the byte array after the IV is the actual encrypted data.
			using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
			using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
			using var sr = new StreamReader(cs);

			// Read the decrypted plainText from the crypto stream.
			return sr.ReadToEnd();
		}
	}
}