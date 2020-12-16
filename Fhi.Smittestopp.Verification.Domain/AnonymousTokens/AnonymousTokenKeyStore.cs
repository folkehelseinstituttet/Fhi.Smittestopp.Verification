﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

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
                return _cache.GetOrCreateAsync(nameof(AnonymousTokenKeyStore) + "_" + keyIntervalNumber, async (cache) =>
                {
                    cache.AbsoluteExpiration = DateTime.Now.AddDays(1);
                    var masterKeyCert = await _masterKeyCertLocator.GetMasterKeyCertificate();
                    var (privateKey, publiceKey) = GenerateKeypair(masterKeyCert, keyIntervalNumber);
                    return new AnonymousTokenSigningKeypair(keyIntervalNumber.ToString(), privateKey, publiceKey);
                });
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private (BigInteger privateKey, ECPublicKeyParameters publicKey) GenerateKeypair(X509Certificate2 masterKeyCert, long keyIntervalNumber)
        {
            var privateKeyBytes = GeneratePrivateKeyBytes(masterKeyCert, keyIntervalNumber);
            var privateKey = new BigInteger(privateKeyBytes);
            var publicKey = CalculatePublicKey(privateKey);
            return (privateKey, publicKey);
        }

        private byte[] GeneratePrivateKeyBytes(X509Certificate2 masterKeyCert, long keyIntervalNumber)
        {
            var masterKeyBytes = RetrievePrivateKeyBytes(masterKeyCert);
            var keyIntervalBytes = BitConverter.GetBytes(keyIntervalNumber);
            var newKeyInputBytes = masterKeyBytes.Concat(keyIntervalBytes).ToArray();
            using (HashAlgorithm algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(newKeyInputBytes);
            }
        }

        private ECPublicKeyParameters CalculatePublicKey(BigInteger privateKey)
        {
            var curve = CustomNamedCurves.GetByOid(X9ObjectIdentifiers.Prime256v1);
            var publicKeyPoint = curve.G.Multiply(privateKey);
            var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());
            return new ECPublicKeyParameters("ECDSA", publicKeyPoint, domainParams);
        }

        private byte[] RetrievePrivateKeyBytes(X509Certificate2 certificate)
        {
            var ecdsaPrivateKey = certificate.GetECDsaPrivateKey();
            if (ecdsaPrivateKey != null)
            {
                return ecdsaPrivateKey.ExportParameters(true).D;
            }

            var rsaPrivateKey = certificate.GetRSAPrivateKey();
            if (rsaPrivateKey != null)
            {
                return Encoding.UTF8.GetBytes(rsaPrivateKey.ToXmlString(true));
            }

            throw new NotSupportedException($"Unsupported private key for certificate {certificate.Thumbprint}");
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