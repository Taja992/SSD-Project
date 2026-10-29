using System.Security.Cryptography;
using Vms.Infrastructure.Security;

namespace Vms.Api.Tests;

public class FieldEncryptionTests
{
    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static AesGcmFieldEncryptor With(string activeKeyId, params (string Id, string Key)[] keys) =>
        new(new EncryptionOptions
        {
            ActiveKeyId = activeKeyId,
            Keys = keys.ToDictionary(k => k.Id, k => k.Key),
        });

    private static readonly AesGcmFieldEncryptor Encryptor = With("k1", ("k1", NewKey()));

    [Fact]
    public void Round_trip_returns_the_original_text()
    {
        var stored = Encryptor.Encrypt("SQL injection in /login — ünïcødé 🔓", "Finding.Description");
        Assert.Equal("SQL injection in /login — ünïcødé 🔓", Encryptor.Decrypt(stored, "Finding.Description"));
    }

    [Fact]
    public void Stored_value_is_not_the_plain_text()
    {
        var stored = Encryptor.Encrypt("secret exploit details", "Finding.Description");
        Assert.StartsWith("enc:v1:k1:", stored);
        Assert.DoesNotContain("secret", stored);
    }

    [Fact] // a fresh random nonce every time
    public void Same_text_encrypts_differently_each_time() =>
        Assert.NotEqual(
            Encryptor.Encrypt("same", "Finding.Description"),
            Encryptor.Encrypt("same", "Finding.Description"));

    [Fact] // the authentication tag catches any change
    public void Tampered_ciphertext_is_rejected()
    {
        var stored = Encryptor.Encrypt("do not touch", "Finding.Description");
        var data = Convert.FromBase64String(stored["enc:v1:k1:".Length..]);
        data[^1] ^= 0x01; // flip one bit
        var tampered = "enc:v1:k1:" + Convert.ToBase64String(data);

        Assert.ThrowsAny<CryptographicException>(() => Encryptor.Decrypt(tampered, "Finding.Description"));
    }

    [Fact] // the purpose is bound in: an email cannot be moved into a description
    public void Value_cannot_be_moved_to_another_field()
    {
        var email = Encryptor.Encrypt("reporter@example.com", "Finding.ReporterEmail");
        Assert.ThrowsAny<CryptographicException>(() => Encryptor.Decrypt(email, "Finding.Description"));
    }

    [Fact] // key rotation: old values still decrypt, new values use the new key
    public void Rotated_key_still_reads_old_values()
    {
        var oldKey = NewKey();
        var before = With("k1", ("k1", oldKey)).Encrypt("written with k1", "Finding.Description");

        var rotated = With("k2", ("k1", oldKey), ("k2", NewKey()));

        Assert.Equal("written with k1", rotated.Decrypt(before, "Finding.Description"));
        Assert.StartsWith("enc:v1:k2:", rotated.Encrypt("new", "Finding.Description"));
    }

    [Fact]
    public void Wrong_key_size_is_refused_at_startup() =>
        Assert.Throws<InvalidOperationException>(() =>
            With("k1", ("k1", Convert.ToBase64String(new byte[16]))));

    [Fact]
    public void Missing_active_key_is_refused_at_startup() =>
        Assert.Throws<InvalidOperationException>(() => With("k2", ("k1", NewKey())));
}
