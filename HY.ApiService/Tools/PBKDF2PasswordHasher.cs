using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace HY.ApiService.Tools
{
    //public static class PBKDF2PasswordHasher
    //{
    //    private const int SaltSize = 16;      // 128 bit
    //    private const int KeySize = 32;       // 256 bit
    //    private const int Iterations = 100_000;

    //    public static (string hash, string salt) Hash(string password)
    //    {
    //        // 1. 生成随机 Salt
    //        var saltByte = RandomNumberGenerator.GetBytes(SaltSize);

    //        salt = Convert.ToBase64String(saltByte);

    //        // 2. PBKDF2 推导 Key
    //        var key = Rfc2898DeriveBytes.Pbkdf2(
    //            password: Encoding.UTF8.GetBytes(password),
    //            salt: saltByte,
    //            iterations: Iterations,
    //            hashAlgorithm: HashAlgorithmName.SHA256,
    //            outputLength: KeySize);

    //        // 3. 组合结果：version.iterations.salt.hash
    //        return string.Join('.',
    //            "v1",
    //            Iterations,
    //            Convert.ToBase64String(saltByte),
    //            Convert.ToBase64String(key));
    //    }

    //    public static bool Verify(string password, string hash, string salt)
    //    {
    //        var parts = hash.Split('.');
    //        if (parts.Length != 4 || parts[0] != "v1")
    //            return false;

    //        var iterations = int.Parse(parts[1]);
    //        var salt = Convert.FromBase64String(parts[2]);
    //        var storedKey = Convert.FromBase64String(parts[3]);

    //        var key = Rfc2898DeriveBytes.Pbkdf2(
    //            password: Encoding.UTF8.GetBytes(password),
    //            salt: salt,
    //            iterations: iterations,
    //            hashAlgorithm: HashAlgorithmName.SHA256,
    //            outputLength: storedKey.Length);

    //        // 固定时间比较，防时序攻击
    //        return CryptographicOperations.FixedTimeEquals(key, storedKey);
    //    }
    //}


    public static class PBKDF2PasswordHasher
    {
        private const int SaltSize = 16;      // 128 bit
        private const int KeySize = 32;       // 256 bit
        private const int Iterations = 100_000;

        public static string Hash(string password, out string salt)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be empty.");

            // 1. 生成随机 Salt
            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);

            salt = Convert.ToBase64String(saltBytes);

            // 2. 计算 PBKDF2
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool Verify(string password, string hash, string salt)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;

            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] hashBytes = Convert.FromBase64String(hash);

            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

            // 常量时间比较（防止 Timing Attack）
            return CryptographicOperations.FixedTimeEquals(computedHash, hashBytes);
        }
    }

}
