using System.Security.Cryptography;

namespace HumanBirthPredictionSystem.Data
{
    /// <summary>
    /// Secure password hashing using PBKDF2 (Rfc2898DeriveBytes) with a
    /// per-password random salt. Passwords are never stored as plain text.
    /// Format stored: {iterations}.{base64(salt)}.{base64(hash)}
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;       // 128 bit
        private const int HashSize = 32;       // 256 bit
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            using var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                HashAlgorithmName.SHA256);

            var salt = algorithm.Salt;
            var hash = algorithm.GetBytes(HashSize);

            return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string hashedValue)
        {
            var parts = hashedValue.Split('.', 3);
            if (parts.Length != 3) return false;

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var storedHash = Convert.FromBase64String(parts[2]);

            using var algorithm = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);

            var computedHash = algorithm.GetBytes(storedHash.Length);
            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
    }
}
