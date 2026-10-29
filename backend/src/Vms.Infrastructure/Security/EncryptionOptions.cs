namespace Vms.Infrastructure.Security;

/// <summary>
/// Bound from the "Encryption" configuration section. Keys are Base64, 32 bytes
/// each (AES-256). Real keys live ONLY in gitignored config or a secret store.
/// </summary>
public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>The key id used for every NEW encryption.</summary>
    public string ActiveKeyId { get; set; } = "";

    /// <summary>
    /// Every key that may still be needed to DECRYPT. Rotating means adding a
    /// new key, making it active, and keeping the old one until no value uses it.
    /// </summary>
    public Dictionary<string, string> Keys { get; set; } = [];
}
