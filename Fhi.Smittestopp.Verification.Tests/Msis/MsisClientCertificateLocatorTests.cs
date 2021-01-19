using System.Security.Cryptography.X509Certificates;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Utilities;
using Fhi.Smittestopp.Verification.Msis;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Msis
{
    [TestFixture]
    public class MsisClientCertificateLocatorTests
    {
        [Test]
        public void GetCertificate_CertNotFound_ThrowsException()
        {
            var options = new MsisClientCertLocator.Config
            {
                CertId = "key-id"
            };
            var cacheKey = nameof(MsisClientCertLocator);
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
                .Setup<ICertificateLocator, Option<X509Certificate2>>(x => x.GetCertificate("key-id"))
                .Returns(Option.None<X509Certificate2>());

            var target = automocker.CreateInstance<MsisClientCertLocator>();

            Assert.Throws<CertificateNotFoundException>(() => target.GetCertificate());
        }

        [Test]
        public void GetMasterKeyCertificate_NotFoundCertCached_ThrowsException()
        {
            var options = new MsisClientCertLocator.Config
            {
                CertId = "key-id"
            };
            var cacheKey = nameof(MsisClientCertLocator);
            object cachedResult = Option.None<X509Certificate2>();

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(cacheKey, out cachedResult))
                .Returns(true);

            var target = automocker.CreateInstance<MsisClientCertLocator>();

            Assert.Throws<CertificateNotFoundException>(() => target.GetCertificate());
        }

        [Test]
        public void GetMasterKeyCertificate_CertCached_ReturnsFromCache()
        {
            var options = new MsisClientCertLocator.Config
            {
                CertId = "key-id"
            };
            var cacheKey = nameof(MsisClientCertLocator);
            var certificate = CertUtils.GenerateTestCert();
            object cachedResult = certificate.Some();

            var automocker = new AutoMocker();

            automocker.SetupOptions(options);

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(cacheKey, out cachedResult))
                .Returns(true);

            var target = automocker.CreateInstance<MsisClientCertLocator>();

            var result = target.GetCertificate();

            result.Should().Be(certificate);
        }

        [Test]
        public void GetMasterKeyCertificate_CertNotCached_ReturnsFromLocator()
        {
            var options = new MsisClientCertLocator.Config
            {
                CertId = "key-id"
            };
            var cacheKey = nameof(MsisClientCertLocator);
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
                .Setup<ICertificateLocator, Option<X509Certificate2>>(x => x.GetCertificate("key-id"))
                .Returns(certificate.Some());

            var target = automocker.CreateInstance<MsisClientCertLocator>();

            var result = target.GetCertificate();

            result.Should().Be(certificate);
        }
    }
}
