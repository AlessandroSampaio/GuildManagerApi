namespace GuildManagerApi.Domain.Interfaces;

public interface IFieldEncryptionService
{
    byte[] Encrypt(string plainText);
    string Decrypt(byte[] encryptedBytes);
}
