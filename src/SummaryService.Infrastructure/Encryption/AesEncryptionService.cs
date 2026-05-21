using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Options;

namespace SummaryService.Infrastructure.Encryption;

public sealed class AesEncryptionService(IOptions<AesOptions> options) : IAesEncryptionService
{
    private readonly byte[] _key = Convert.FromHexString(options.Value.Key);

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var combined = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.IV.Length];
        Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);

        var cipherBytes = new byte[combined.Length - iv.Length];
        Buffer.BlockCopy(combined, iv.Length, cipherBytes, 0, cipherBytes.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, iv);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
