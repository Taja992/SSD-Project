namespace Vms.Infrastructure.Security;

/// <summary>
/// Encrypts single database fields (FIND-11). The purpose names the field, so a
/// value encrypted as one field cannot be copied into another and still decrypt.
/// </summary>
public interface IFieldEncryptor
{
    string Encrypt(string plaintext, string purpose);
    string Decrypt(string stored, string purpose);
}
