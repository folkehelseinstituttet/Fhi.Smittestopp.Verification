using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Optional.Collections;

namespace Fhi.Smittestopp.Verification.Server.Credentials
{
    public class SigningCredentialsStore : ISigningCredentialStore, IValidationKeysStore
    {
        private const string MemoryCacheKey = "IsOAuthCerts";
        private const string SigningAlgorithm = SecurityAlgorithms.RsaSha256;

        private readonly Config _config;

        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1);
        private readonly ICertificateLocator _certificateLocator;
        private readonly IMemoryCache _cache;

        public SigningCredentialsStore(IMemoryCache cache, ICertificateLocator certificateLocator, IOptions<Config> config)
        {
            _cache = cache;
            _certificateLocator = certificateLocator;
            _config = config.Value;
        }

        public async Task<SigningCredentials> GetSigningCredentialsAsync()
        {
            await _cacheLock.WaitAsync();
            try
            {
                var (active, _) = await _cache.GetOrCreateAsync(MemoryCacheKey, RefreshCacheAsync);
                return active;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
        {
            await _cacheLock.WaitAsync();
            try
            {
                var (_, secondary) = await _cache.GetOrCreateAsync(MemoryCacheKey, RefreshCacheAsync);
                return secondary;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<(SigningCredentials active, IEnumerable<SecurityKeyInfo> secondary)> RefreshCacheAsync(ICacheEntry cache)
        {
            cache.AbsoluteExpiration = DateTime.Now.AddDays(1);
            var enabledCerts = await _certificateLocator.GetAllEnabledCertificateVersions(_config.Certificate);

            var rolloverTime = DateTime.UtcNow - _config.KeyRolloverDuration;
            var activeSigningCredentials = enabledCerts.FirstOrNone(x => x.Timestamp < rolloverTime)
                .Else(enabledCerts.FirstOrNone)
                .Map(x => new SigningCredentials(new X509SecurityKey(x.Certificate), SigningAlgorithm))
                .ValueOr(() => throw new Exception("Unable to locate signing certificate: " + _config.Certificate));

            var enabledValidationKeys = enabledCerts.Select(x => new SecurityKeyInfo
            {
                Key = new X509SecurityKey(x.Certificate),
                SigningAlgorithm = SigningAlgorithm
            }).ToList();

            return (activeSigningCredentials, enabledValidationKeys);
        }

        public class Config
        {
            public TimeSpan KeyRolloverDuration { get; set; } = TimeSpan.FromHours(24);
            public string Certificate { get; set; }
        }
    }
}
