using System.Security.Cryptography;
using System.Text;

namespace Vms.Infrastructure.Security;

/// <summary>
/// AES-256-GCM field encryption (FIND-11).
///
/// Stored format:  enc:v1:{keyId}:{Base64(nonce | tag | ciphertext)}
///
/// "v1" names the algorithm (AES-256-GCM) and "keyId" names the key. Both are
/// written into every value, so the algorithm or key can be swapped later
/// (crypto agility) without breaking data that was encrypted before.
/// </summary>
public sealed class AesGcmFieldEncryptor : IFieldEncryptor
{
    private const string Prefix = "enc:v1:";
    private const int KeySize = 32;   // AES-256
    private const int NonceSize = 12; // 96 bits, the size GCM is designed for
    private const int TagSize = 16;   // 128-bit authentication tag
    public const int MaxKeyIdLength = 16;

    private readonly string _activeKeyId;
    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.Ordinal);

    public AesGcmFieldEncryptor(EncryptionOptions options)
    {
        foreach (var (id, base64) in options.Keys)
        {
            if (id.Length is 0 or > MaxKeyIdLength || id.Contains(':'))
                throw new InvalidOperationException(
                    $"Encryption key id '{id}' must be 1-{MaxKeyIdLength} characters, without ':'.");

            byte[] key;
            try { key = Convert.FromBase64String(base64); }
            catch (FormatException)
            {
                throw new InvalidOperationException($"Encryption key '{id}' is not valid Base64.");
            }

            if (key.Length != KeySize)
                throw new InvalidOperationException(
                    $"Encryption key '{id}' must be {KeySize} bytes (AES-256), not {key.Length}.");

            _keys[id] = key;
        }

        if (!_keys.ContainsKey(options.ActiveKeyId))
            throw new InvalidOperationException(
                "Encryption:ActiveKeyId must name one of the keys in Encryption:Keys.");

        _activeKeyId = options.ActiveKeyId;
    }

    public string Encrypt(string plaintext, string purpose)
    {
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var blob = new byte[NonceSize + TagSize + plain.Length];
        var nonce = blob.AsSpan(0, NonceSize);
        var tag = blob.AsSpan(NonceSize, TagSize);
        var cipher = blob.AsSpan(NonceSize + TagSize);

        // A fresh random nonce for EVERY value. Reusing a nonce with the same
        // key breaks GCM completely - never derive it from anything predictable.
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_keys[_activeKeyId], TagSize);
        aes.Encrypt(nonce, plain, cipher, tag, AssociatedData(_activeKeyId, purpose));

        return $"{Prefix}{_activeKeyId}:{Convert.ToBase64String(blob)}";
    }

    public string Decrypt(string stored, string purpose)
    {
        // Every row is encrypted now (guide 08, section 7). A plain-text value
        // means someone wrote to the database directly - refuse it.
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("Value is not encrypted.");

        var rest = stored[Prefix.Length..];
        var separator = rest.IndexOf(':');
        if (separator <= 0)
            throw new CryptographicException("Encrypted value is malformed.");

        var keyId = rest[..separator];
        if (!_keys.TryGetValue(keyId, out var key))
            throw new CryptographicException($"No key with id '{keyId}' is configured.");

        var blob = Convert.FromBase64String(rest[(separator + 1)..]);
        if (blob.Length < NonceSize + TagSize)
            throw new CryptographicException("Encrypted value is too short.");

        var plain = new byte[blob.Length - NonceSize - TagSize];
        using var aes = new AesGcm(key, TagSize);

        // Throws AuthenticationTagMismatchException if a single bit of the
        // ciphertext, the key id or the purpose was changed.
        aes.Decrypt(
            blob.AsSpan(0, NonceSize),
            blob.AsSpan(NonceSize + TagSize),
            blob.AsSpan(NonceSize, TagSize),
            plain,
            AssociatedData(keyId, purpose));

        return Encoding.UTF8.GetString(plain);
    }

    /// <summary>
    /// Longest stored value for a plaintext of at most <paramref name="maxChars"/>
    /// characters. Used for the database column length, so the cap stays exact.
    /// </summary>
    public static int MaxStoredLength(int maxChars)
    {
        var maxBlob = NonceSize + TagSize + 3 * maxChars;     // UTF-8: max 3 bytes per char
        var base64 = (maxBlob + 2) / 3 * 4;
        return Prefix.Length + MaxKeyIdLength + 1 + base64;   // "enc:v1:" + id + ":" + data
    }

    // Authenticated but not encrypted: binds the ciphertext to its key id and
    // its field, so neither can be swapped without decryption failing.
    private static byte[] AssociatedData(string keyId, string purpose) =>
        Encoding.UTF8.GetBytes($"{Prefix}{keyId}|{purpose}");
}
