using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Server.Credentials;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using IdentityServer4.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Server.Credentials
{
    [TestFixture]
    public class SigningCredentialsStoreTests
    {
        [Test]
        public async Task GetSigningCredentialsAsync_ResultIsCached_ReturnsCachedResult()
        {
            //Arrange
            var cert = CertUtils.GenerateTestCert();
            var alg = SecurityAlgorithms.RsaSha256;
            var activeSigningCredentials = new SigningCredentials(new X509SecurityKey(cert), alg);
            IEnumerable<SecurityKeyInfo> enabledValidationKeys = new[]
            {
                new SecurityKeyInfo
                {
                    Key = new X509SecurityKey(cert),
                    SigningAlgorithm = SecurityAlgorithms.RsaSha256
                }
            };
            object cachedResult = (activeSigningCredentials, enabledValidationKeys);

            var automocker = new AutoMocker();

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue("IsOAuthCerts", out cachedResult))
                .Returns(true);

            var target = automocker.CreateInstance<SigningCredentialsStore>();

            //Act
            var result = await target.GetSigningCredentialsAsync();

            //Assert
            result.Should().Be(activeSigningCredentials);
        }

        [Test]
        public async Task GetSigningCredentialsAsync_ResultNotCachedConfigSigningOnly_ReturnsBasedOnCertLocator()
        {
            //Arrange
            var cert = CertUtils.GenerateTestCert();
            object cachedResult = null;

            var automocker = new AutoMocker();

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue("IsOAuthCerts", out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry("IsOAuthCerts"))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IOptions<SigningCredentialsStore.Config>, SigningCredentialsStore.Config>(x => x.Value)
                .Returns(new SigningCredentialsStore.Config
                {
                    Signing = "cert-id"
                });

            automocker
                .Setup<ICertificateLocator, Task<ICollection<CertificateVersion>>>(x =>
                    x.GetAllEnabledCertificateVersionsAsync("cert-id"))
                .ReturnsAsync(new[]
                {
                    new CertificateVersion
                    {
                        Certificate = cert,
                        Timestamp = DateTime.Now
                    }
                });

            var target = automocker.CreateInstance<SigningCredentialsStore>();

            //Act
            var result = await target.GetSigningCredentialsAsync();

            //Assert
            result.Key.Should().BeOfType<X509SecurityKey>();
            ((X509SecurityKey) result.Key).Certificate.Should().Be(cert);
        }

        [Test]
        public async Task GetSigningCredentialsAsync_ResultNotCachedMultipleValidVersions_ReturnsFirstOlderThanRollover()
        {
            //Arrange
            var cert1 = CertUtils.GenerateTestCert();
            var cert2 = CertUtils.GenerateTestCert();
            var cert3 = CertUtils.GenerateTestCert();

            var rollover = TimeSpan.FromHours(2);
            var epsilon = TimeSpan.FromSeconds(1);
            object cachedResult = null;

            var automocker = new AutoMocker();

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue("IsOAuthCerts", out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry("IsOAuthCerts"))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IOptions<SigningCredentialsStore.Config>, SigningCredentialsStore.Config>(x => x.Value)
                .Returns(new SigningCredentialsStore.Config
                {
                    Signing = "cert-id",
                    KeyRolloverDuration = rollover
                });

            automocker
                .Setup<ICertificateLocator, Task<ICollection<CertificateVersion>>>(x =>
                    x.GetAllEnabledCertificateVersionsAsync("cert-id"))
                .ReturnsAsync(new[]
                {
                    new CertificateVersion
                    {
                        Certificate = cert1,
                        Timestamp = DateTime.UtcNow
                    },
                    new CertificateVersion
                    {
                        Certificate = cert2,
                        Timestamp = DateTime.UtcNow - rollover - epsilon
                    },
                    new CertificateVersion
                    {
                        Certificate = cert3,
                        Timestamp = DateTime.UtcNow - rollover - epsilon
                    }
                });

            var target = automocker.CreateInstance<SigningCredentialsStore>();

            //Act
            var result = await target.GetSigningCredentialsAsync();

            //Assert
            result.Key.Should().BeOfType<X509SecurityKey>();
            ((X509SecurityKey)result.Key).Certificate.Should().Be(cert2);
        }

        [Test]
        public async Task GetSigningCredentialsAsync_ResultNotCachedMultipleVersionWithinRollover_ReturnsFirst()
        {
            //Arrange
            var cert1 = CertUtils.GenerateTestCert();
            var cert2 = CertUtils.GenerateTestCert();

            var rollover = TimeSpan.FromHours(2);
            object cachedResult = null;

            var automocker = new AutoMocker();

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue("IsOAuthCerts", out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry("IsOAuthCerts"))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IOptions<SigningCredentialsStore.Config>, SigningCredentialsStore.Config>(x => x.Value)
                .Returns(new SigningCredentialsStore.Config
                {
                    Signing = "cert-id",
                    KeyRolloverDuration = rollover
                });

            automocker
                .Setup<ICertificateLocator, Task<ICollection<CertificateVersion>>>(x =>
                    x.GetAllEnabledCertificateVersionsAsync("cert-id"))
                .ReturnsAsync(new[]
                {
                    new CertificateVersion
                    {
                        Certificate = cert1,
                        Timestamp = DateTime.UtcNow
                    },
                    new CertificateVersion
                    {
                        Certificate = cert2,
                        Timestamp = DateTime.UtcNow
                    }
                });

            var target = automocker.CreateInstance<SigningCredentialsStore>();

            //Act
            var result = await target.GetSigningCredentialsAsync();

            //Assert
            result.Key.Should().BeOfType<X509SecurityKey>();
            ((X509SecurityKey)result.Key).Certificate.Should().Be(cert1);
        }

        [Test]
        public void GetSigningCredentialsAsync_ResultNotCachedNoValidCertVersion_ThrowsException()
        {
            //Arrange
            object cachedResult = null;

            var automocker = new AutoMocker();

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue("IsOAuthCerts", out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry("IsOAuthCerts"))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IOptions<SigningCredentialsStore.Config>, SigningCredentialsStore.Config>(x => x.Value)
                .Returns(new SigningCredentialsStore.Config
                {
                    Signing = "cert-id"
                });

            automocker
                .Setup<ICertificateLocator, Task<ICollection<CertificateVersion>>>(x =>
                    x.GetAllEnabledCertificateVersionsAsync("cert-id"))
                .ReturnsAsync(new CertificateVersion[0]);

            var target = automocker.CreateInstance<SigningCredentialsStore>();

            //Act / Assert
            Assert.ThrowsAsync<Exception>(() => target.GetSigningCredentialsAsync());
        }

        [Test]
        public async Task GetValidationKeysAsync_ResultNotCachedAdditonalValidationKeys_ReturnsAllVersionsAllCerts()
        {
            //Arrange
            var cert1 = CertUtils.GenerateTestCert();
            var cert2 = CertUtils.GenerateTestCert();
            var cert3 = CertUtils.GenerateTestCert();
            var cert4 = CertUtils.GenerateTestCert();

            var rollover = TimeSpan.FromHours(2);
            var epsilon = TimeSpan.FromSeconds(1);
            object cachedResult = null;

            var automocker = new AutoMocker();

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue("IsOAuthCerts", out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry("IsOAuthCerts"))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IOptions<SigningCredentialsStore.Config>, SigningCredentialsStore.Config>(x => x.Value)
                .Returns(new SigningCredentialsStore.Config
                {
                    Signing = "cert-id-1",
                    AdditionalValidation = new []{ "cert-id-2" },
                    KeyRolloverDuration = rollover
                });

            automocker
                .Setup<ICertificateLocator, Task<ICollection<CertificateVersion>>>(x =>
                    x.GetAllEnabledCertificateVersionsAsync("cert-id-1"))
                .ReturnsAsync(new[]
                {
                    new CertificateVersion
                    {
                        Certificate = cert1,
                        Timestamp = DateTime.Now
                    },
                    new CertificateVersion
                    {
                        Certificate = cert2,
                        Timestamp = DateTime.Now - rollover - epsilon
                    }
                });

            automocker
                .Setup<ICertificateLocator, Task<ICollection<CertificateVersion>>>(x =>
                    x.GetAllEnabledCertificateVersionsAsync("cert-id-2"))
                .ReturnsAsync(new[]
                {
                    new CertificateVersion
                    {
                        Certificate = cert3,
                        Timestamp = DateTime.Now - rollover - epsilon
                    },
                    new CertificateVersion
                    {
                        Certificate = cert4,
                        Timestamp = DateTime.Now - rollover - epsilon
                    }
                });

            var target = automocker.CreateInstance<SigningCredentialsStore>();

            //Act
            var result = (await target.GetValidationKeysAsync()).ToList();

            //Assert
            result.Count.Should().Be(4);
            result.Select(x => x.Key)
                .OfType<X509SecurityKey>()
                .Select(x => x.Certificate)
                .ToList()
                .Should().Contain(cert1)
                .And.Contain(cert2)
                .And.Contain(cert3)
                .And.Contain(cert4);
        }
    }
}
