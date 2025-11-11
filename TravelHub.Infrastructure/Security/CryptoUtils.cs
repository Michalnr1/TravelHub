using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TravelHub.Infrastructure.Security
{
    public static class CryptoUtils
    {
        /// <summary>
        /// Generate a cryptographically secure password containing letters, digits and symbols.
        /// </summary>
        public static string GenerateStrongPassword(int length = 20)
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // omit ambiguous chars like I,O
            const string lower = "abcdefghijkmnopqrstuvwxyz"; // omit l
            const string digits = "23456789"; // omit 0,1
            const string symbols = "!@#$%^&*-_+=?";

            var categories = new[] { upper, lower, digits, symbols };
            var rng = RandomNumberGenerator.Create();

            // ensure at least one char from each category if length allows
            var passwordChars = new char[length];
            int pos = 0;
            for (int i = 0; i < categories.Length && pos < length; i++)
            {
                passwordChars[pos++] = GetRandomChar(categories[i], rng);
            }

            // fill remaining
            var all = string.Concat(categories);
            while (pos < length)
            {
                passwordChars[pos++] = GetRandomChar(all, rng);
            }

            // shuffle
            var shuffled = passwordChars.OrderBy(_ => NextInt(rng)).ToArray();
            return new string(shuffled);
        }

        private static char GetRandomChar(string pool, RandomNumberGenerator rng)
        {
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var idx = BitConverter.ToUInt32(bytes, 0) % (uint)pool.Length;
            return pool[(int)idx];
        }

        private static int NextInt(RandomNumberGenerator rng)
        {
            var b = new byte[4];
            rng.GetBytes(b);
            return Math.Abs(BitConverter.ToInt32(b, 0));
        }

        /// <summary>
        /// Derive a 32-byte AES key from password and salt using PBKDF2.
        /// </summary>
        public static byte[] DeriveKeyFromPassword(string password, byte[] salt, int iterations = 150_000)
        {
            using var kdf = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32); // 256-bit key
        }
    }
}
