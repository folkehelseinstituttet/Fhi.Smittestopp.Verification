using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public interface IAnonymousTokensKeyStore
    {
        Task<AnonymousTokenSigningKeypair> GetActiveSigningKeyPair();
        Task<IEnumerable<AnonymousTokenValidationKey>> GetActiveValidationKeys();
    }

    public class AnonymousTokenKeyStore : IAnonymousTokensKeyStore
    {
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1);

        private readonly IMemoryCache _cache;

        private readonly AnonymousTokensConfig _config;
        private readonly IAnonymousTokenMasterKeyCertificateLocator _masterKeyCertLocator;

        public AnonymousTokenKeyStore(IOptions<AnonymousTokensConfig> config, IMemoryCache cache, IAnonymousTokenMasterKeyCertificateLocator masterKeyCertLocator)
        {
            _cache = cache;
            _masterKeyCertLocator = masterKeyCertLocator;
            _config = config.Value;
        }

        public Task<AnonymousTokenSigningKeypair> GetActiveSigningKeyPair()
        {
            // with no key rollover, key interval is always 1
            var keyIntervalNumber = _config.KeyRotationEnabled
                ? GetActiveKeyIntervalNumber()
                : 1;

            return GetOrCreateSigningKeypairForInterval(keyIntervalNumber);
        }

        public async Task<IEnumerable<AnonymousTokenValidationKey>> GetActiveValidationKeys()
        {
            var validKeyIntervals = _config.KeyRotationEnabled
                ? GetAllValidIntervalNumbers()
                : new long[] { 1 };

            // User parent cert to generate new certs that are rotated
            var validSigningCerts = new List<AnonymousTokenValidationKey>();
            foreach (var validInterval in validKeyIntervals)
            {
                var signingKeyPair = await GetOrCreateSigningKeypairForInterval(validInterval);
                validSigningCerts.Add(signingKeyPair.AsValidationKey());
            }
            return validSigningCerts;
        }

        private Task<AnonymousTokenSigningKeypair> GetOrCreateSigningKeypairForInterval(long keyIntervalNumber)
        {
            _cacheLock.Wait();
            try
            {
                return _cache.GetOrCreateAsync(nameof(AnonymousTokenKeyStore) + "_" + keyIntervalNumber, (cache) =>
                {
                    cache.AbsoluteExpiration = DateTime.Now.AddDays(1);
                    return CreateKeyPairForInterval(keyIntervalNumber);
                });
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<AnonymousTokenSigningKeypair> CreateKeyPairForInterval(long keyIntervalNumber)
        {
            var masterKeyCert = await _masterKeyCertLocator.GetMasterKeyCertificate();
            var crvName = "P-256";
            var ecParameters = CustomNamedCurves.GetByOid(X9ObjectIdentifiers.Prime256v1);
            var keyPairGenerator = new RollingKeyPairGenerator(masterKeyCert, ecParameters);
            var (privateKey, publiceKey) = keyPairGenerator.GenerateKeyPairForInterval(keyIntervalNumber);
            return new AnonymousTokenSigningKeypair(keyIntervalNumber.ToString(), crvName, ecParameters, privateKey, publiceKey);
        }

        private long GetActiveKeyIntervalNumber()
        {
            return ToIntervalNumber(DateTimeOffset.UtcNow);
        }

        private IEnumerable<long> GetAllValidIntervalNumbers()
        {
            var currentTime = DateTimeOffset.UtcNow;
            return new []
            {
                currentTime - _config.KeyRotationRollover,
                currentTime,
                currentTime + _config.KeyRotationRollover
            }.Select(ToIntervalNumber).Distinct();
        }

        private long ToIntervalNumber(DateTimeOffset pointInTime)
        {
            return pointInTime.ToUnixTimeSeconds() / Convert.ToInt64(_config.KeyRotationInterval.TotalSeconds);
        }
    }
}
