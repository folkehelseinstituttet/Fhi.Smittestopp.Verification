using System;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;

namespace Fhi.Smittestopp.Verification.Domain.Factories
{
    public class OneWayPseudonymFactory : IPseudonymFactory
    {
        private readonly Config _config;

        public OneWayPseudonymFactory(IOptions<Config> config)
        {
            _config = config.Value;
        }

        public string Create(string original)
        {
            return Hash(original, _config.KeyBytes, KeyDerivationPrf.HMACSHA256, _config.NumIterations, _config.NumBytes);
        }

        /// <summary>
        /// Simplified version of https://github.com/dotnet/aspnetcore/blob/9428ac6f4c2103227c46a4da23b4de9cb780dc08/src/Identity/Extensions.Core/src/PasswordHasher.cs#L141
        /// Uses a fixed salt, as we need reproducible results
        /// </summary>
        private string Hash(string original, byte[] salt, KeyDerivationPrf prf, int iterCount, int numBytesRequested)
        {
            var hashBytes = KeyDerivation.Pbkdf2(original, salt, prf, iterCount, numBytesRequested);
            return Convert.ToBase64String(hashBytes);
        }

        public class Config
        {
            public string Key
            {
                get => Convert.ToBase64String(KeyBytes ?? new byte[0]);
                set => KeyBytes = Convert.FromBase64String(value);
            }

            public byte[] KeyBytes { get; set; }
            public int NumIterations { get; set; } = 10000;
            public int NumBytes { get; set; } = 256 / 8;
        }
    }
}
