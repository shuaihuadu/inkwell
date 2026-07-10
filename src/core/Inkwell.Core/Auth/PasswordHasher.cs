// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Security.Cryptography;

namespace Inkwell;

/// <summary>
/// 密码哈希与校验的唯一封装点；算法 = PBKDF2（<see cref="Rfc2898DeriveBytes.Pbkdf2(ReadOnlySpan{byte}, ReadOnlySpan{byte}, int, HashAlgorithmName, int)"/>），
/// 零第三方包。<see cref="Hash"/> 返回自描述字符串，<c>User.PasswordHash</c> 列 schema 不随算法选型变化。
/// </summary>
internal static class PasswordHasher
{
    private const string Prefix = "PBKDF2";
    private const int Iterations = 600_000;
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password must not be null or empty.", nameof(password));
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSizeBytes);

        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password must not be null or empty.", nameof(password));
        }

        if (string.IsNullOrEmpty(passwordHash))
        {
            throw new ArgumentException("PasswordHash must not be null or empty.", nameof(passwordHash));
        }

        string[] parts = passwordHash.Split('$');

        if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], out int iterations))
        {
            throw new FormatException($"Unrecognized password hash format (expected '{Prefix}$<iterations>$<salt>$<hash>').");
        }

        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] expectedHash = Convert.FromBase64String(parts[3]);
        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
