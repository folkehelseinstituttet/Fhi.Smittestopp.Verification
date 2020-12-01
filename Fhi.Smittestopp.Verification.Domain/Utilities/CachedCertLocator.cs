using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Fhi.Smittestopp.Verification.Domain.Utilities
{
    public interface ICachedCertLocator
    {
        X509Certificate2 GetCertificate();
        Task<X509Certificate2> GetCertificateAsync();
    }

    public abstract class CachedCertLocator : ICachedCertLocator
    {
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1);

        private readonly string _certId;
        private readonly string _cacheKey;

        private readonly ICertificateLocator _certLocator;
        private readonly IMemoryCache _cache;

        protected CachedCertLocator(string certId, string cacheKey, ICertificateLocator certLocator, IMemoryCache cache)
        {
            _certLocator = certLocator;
            _cache = cache;
            _certId = certId;
            _cacheKey = cacheKey;
        }

        public X509Certificate2 GetCertificate()
        {
            _cacheLock.Wait();
            try
            {
                return _cache.GetOrCreate(_cacheKey, (cache) =>
                {
                    cache.AbsoluteExpiration = DateTime.Now.AddDays(1);
                    return _certLocator.GetCertificate(_certId);
                }).ValueOr(() => throw new Exception("Unable to locate certificate for ID: " + _certId));
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<X509Certificate2> GetCertificateAsync()
        {
            await _cacheLock.WaitAsync();
            try
            {
                return (await _cache.GetOrCreateAsync(_cacheKey, (cache) =>
                {
                    cache.AbsoluteExpiration = DateTime.Now.AddDays(1);
                    return _certLocator.GetCertificateAsync(_certId);
                })).ValueOr(() => throw new Exception("Unable to locate certificate for ID: " + _certId));
            }
            finally
            {
                _cacheLock.Release();
            }
        }
    }
}
