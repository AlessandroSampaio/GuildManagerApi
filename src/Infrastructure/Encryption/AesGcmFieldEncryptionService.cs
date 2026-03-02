using System.Security.Cryptography;
using System.Text;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Infrastructure.Encryption;

public class EncryptionOptions
{
    public const string Section = "Encryption";
    public string MasterKey { get; set; } = string.Empty;
}

public class AesGcmFieldEncryptionService : IFieldEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int HeaderSize = NonceSize + TagSize;

    private static readonly byte[] DerivedKeySalt =
        "WclApi-AES256GCM-v1"u8.ToArray();

    private readonly byte[] _key;

    public AesGcmFieldEncryptionService(IOptions<EncryptionOptions> opts)
    {
        var raw = opts.Value.MasterKey;
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Encryption masterkey missing. " +
                "Generate one with Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))");

        byte[] masterBytes;
        try
        {
            masterBytes = Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Encryption masterkey is not a valid base64 string.");
        }

        _key = Rfc2898DeriveBytes.Pbkdf2(
            password: masterBytes,
            salt: DerivedKeySalt,
            iterations: 200_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32
        );


    }


    public string Decrypt(byte[] encryptedBytes)
    {
        ArgumentNullException.ThrowIfNull(encryptedBytes);
        if (encryptedBytes.Length < HeaderSize + 1)
            throw new CryptographicException(
                "Invalid cipher blob: too short to contain nonce + tag + ciphertext.");

        var nonce = encryptedBytes[..NonceSize];
        var tag = encryptedBytes[NonceSize..HeaderSize];
        var ciphertext = encryptedBytes[HeaderSize..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        // Lança AuthenticationTagMismatchException se o blob foi adulterado
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public byte[] Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherText = new byte[plainTextBytes.Length];
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainTextBytes, cipherText, tag);

        var blob = new byte[HeaderSize + cipherText.Length];
        nonce.CopyTo(blob, 0);
        tag.CopyTo(blob, NonceSize);
        cipherText.CopyTo(blob, HeaderSize);

        return blob;
    }
}
