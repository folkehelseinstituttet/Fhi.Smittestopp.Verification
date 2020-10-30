using System;
using System.Security.Cryptography;
using System.Text;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
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
            using (var hmacsha256 = new HMACSHA256(_config.KeyBytes))
            {
                var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(original));
                return Convert.ToBase64String(hash);
            }
        }

        public class Config
        {
            public string Key
            {
                get => Convert.ToBase64String(KeyBytes ?? new byte[0]);
                set => KeyBytes = Encoding.UTF8.GetBytes(value);
            }

            public byte[] KeyBytes { get; set; }
        }
    }
}
