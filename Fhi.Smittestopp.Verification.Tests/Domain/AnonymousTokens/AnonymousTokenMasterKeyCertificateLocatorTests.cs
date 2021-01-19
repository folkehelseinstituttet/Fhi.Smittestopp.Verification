using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Utilities;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.AnonymousTokens
{
    [TestFixture]
    public class AnonymousTokenMasterKeyCertificateLocatorTests
    {
        [Test]
        public void GetMasterKeyCertificate_CertNotFound_ThrowsException()
        {
            var options = new AnonymousTokensConfig
            {
                MasterKeyCertId = "key-id"
            };
            var cacheKey = nameof(AnonymousTokenMasterKeyCertificateLocator);
            object emptyCachedResult = null;

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(cacheKey, out emptyCachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(cacheKey))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<ICertificateLocator, Task<Option<X509Certificate2>>>(x => x.GetCertificateAsync("key-id"))
                .ReturnsAsync(Option.None<X509Certificate2>());

            var target = automocker.CreateInstance<AnonymousTokenMasterKeyCertificateLocator>();

            Assert.ThrowsAsync<CertificateNotFoundException>(() => target.GetMasterKeyCertificate());
        }

        [Test]
        public void GetMasterKeyCertificate_NotFoundCertCached_ThrowsException()
        {
            var options = new AnonymousTokensConfig
            {
                MasterKeyCertId = "key-id"
            };
            var cacheKey = nameof(AnonymousTokenMasterKeyCertificateLocator);
            object cachedResult = Option.None<X509Certificate2>();

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(cacheKey, out cachedResult))
                .Returns(true);

            var target = automocker.CreateInstance<AnonymousTokenMasterKeyCertificateLocator>();

            Assert.ThrowsAsync<CertificateNotFoundException>(() => target.GetMasterKeyCertificate());
        }

        [Test]
        public async Task GetMasterKeyCertificate_CertCached_ReturnsFromCache()
        {
            var options = new AnonymousTokensConfig
            {
                MasterKeyCertId = "key-id"
            };
            var cacheKey = nameof(AnonymousTokenMasterKeyCertificateLocator);
            var certificate = CertUtils.GenerateTestCert();
            object cachedResult = certificate.Some();

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(cacheKey, out cachedResult))
                .Returns(true);

            var target = automocker.CreateInstance<AnonymousTokenMasterKeyCertificateLocator>();

            var result = await target.GetMasterKeyCertificate();

            result.Should().Be(certificate);
        }

        [Test]
        public async Task GetMasterKeyCertificate_CertNotCached_ReturnsFromLocator()
        {
            var options = new AnonymousTokensConfig
            {
                MasterKeyCertId = "key-id"
            };
            var cacheKey = nameof(AnonymousTokenMasterKeyCertificateLocator);
            object emptyCachedResult = null;
            var certificate = CertUtils.GenerateTestCert();

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(cacheKey, out emptyCachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(cacheKey))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<ICertificateLocator, Task<Option<X509Certificate2>>>(x => x.GetCertificateAsync("key-id"))
                .ReturnsAsync(certificate.Some());

            var target = automocker.CreateInstance<AnonymousTokenMasterKeyCertificateLocator>();

            var result = await target.GetMasterKeyCertificate();

            result.Should().Be(certificate);
        }
    }
}
