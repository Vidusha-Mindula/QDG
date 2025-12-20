using System.Security.Cryptography;
using System.Text;

namespace QDG.Migration.Core.Helpers;

public static class EncryptionHelper
{
    private static readonly byte[] Key = DeriveKeyFromPassword("QDGMigrationTool2025SecretKey", 32);
    private static readonly byte[] IV = DeriveKeyFromPassword("QDGMigration_IV", 16);

    private static byte[] DeriveKeyFromPassword(string password, int keySize)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var key = new byte[keySize];
        Array.Copy(hash, key, keySize);
        return key;
    }

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
            
            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
        }
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            
            return srDecrypt.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
        }
    }
}