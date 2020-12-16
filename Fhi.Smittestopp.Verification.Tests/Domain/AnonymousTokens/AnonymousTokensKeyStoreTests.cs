using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using AnonymousTokens.Client.Protocol;
using AnonymousTokens.Core.Services.InMemory;
using AnonymousTokens.Server.Protocol;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;

namespace Fhi.Smittestopp.Verification.Tests.Domain.AnonymousTokens
{
    [TestFixture]
    public class AnonymousTokensKeyStoreTests
    {
        [Test]
        public async Task GetActiveValidationKeys_GivenNoRotationAndMasterKeyCert_ReturnsOneValidationKey()
        {
            //Arrange
            var automocker = new AutoMocker();

            var testCertificate = CertUtils.GenerateTestCert();
            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = false,
                    MasterKeyCertId = "master-key-cert"
                });

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(It.IsAny<string>(), out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(It.IsAny<string>()))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IAnonymousTokenMasterKeyCertificateLocator, Task<X509Certificate2>>(x => x.GetMasterKeyCertificate())
                .ReturnsAsync(testCertificate);

            var target = automocker.CreateInstance<AnonymousTokenKeyStore>();

            //Act
            var result = (await target.GetActiveValidationKeys()).ToList();

            //Assert
            result.Should().HaveCount(1);
            var key = result.First();
            key.PublicKey.Should().NotBeNull();
        }

        [Test]
        public async Task GetActiveValidationKeys_GivenRolloverAndMasterKeyCert_ReturnsKeysIncludingRollover()
        {
            //Arrange
            var automocker = new AutoMocker();

            var testCertificate = CertUtils.GenerateTestCert();
            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = true,
                    MasterKeyCertId = "master-key-cert",
                    KeyRotationInterval = TimeSpan.FromDays(3),
                    KeyRotationRollover = TimeSpan.FromDays(4)
                });

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(It.IsAny<string>(), out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(It.IsAny<string>()))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IAnonymousTokenMasterKeyCertificateLocator, Task<X509Certificate2>>(x => x.GetMasterKeyCertificate())
                .ReturnsAsync(testCertificate);

            var target = automocker.CreateInstance<AnonymousTokenKeyStore>();

            //Act
            var result = (await target.GetActiveValidationKeys()).ToList();

            //Assert
            result.Should().HaveCountGreaterThan(1);
            result.Should().NotContain(c => c.PublicKey == null);
        }

        [Test]
        public async Task GetActiveSigningKeyPair_GivenNoRotationAndMasterKeyCert_ReturnsFullKeyPair()
        {
            //Arrange
            var automocker = new AutoMocker();

            var testCertificate = CertUtils.GenerateTestCert();
            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = false,
                    MasterKeyCertId = "master-key-cert"
                });

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(It.IsAny<string>(), out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(It.IsAny<string>()))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IAnonymousTokenMasterKeyCertificateLocator, Task<X509Certificate2>>(x => x.GetMasterKeyCertificate())
                .ReturnsAsync(testCertificate);

            var target = automocker.CreateInstance<AnonymousTokenKeyStore>();

            //Act
            var result = await target.GetActiveSigningKeyPair();

            //Assert
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Kid.Should().NotBeNull();
                result.PublicKey.Should().NotBeNull();
                result.PrivateKey.Should().NotBeNull();
            }
        }

        [Test]
        public async Task GetActiveSigningKeyPair_GivenRotationAndMasterKeyCert_ReturnsFullKeyPair()
        {
            //Arrange
            var automocker = new AutoMocker();

            var testCertificate = CertUtils.GenerateTestCert();
            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = true,
                    MasterKeyCertId = "master-key-cert",
                    KeyRotationInterval = TimeSpan.FromDays(3),
                    KeyRotationRollover = TimeSpan.FromDays(4)
                });

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(It.IsAny<string>(), out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(It.IsAny<string>()))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IAnonymousTokenMasterKeyCertificateLocator, Task<X509Certificate2>>(x => x.GetMasterKeyCertificate())
                .ReturnsAsync(testCertificate);

            var target = automocker.CreateInstance<AnonymousTokenKeyStore>();

            //Act
            var result = await target.GetActiveSigningKeyPair();

            //Assert
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.Kid.Should().NotBeNull();
                result.PublicKey.Should().NotBeNull();
                result.PrivateKey.Should().NotBeNull();
            }
        }

        [Test]
        public async Task GetActiveSigningKeyPair_GivenRotationAndMasterKeyCert_ReturnsAKeyPairUsableForTokenSigning()
        {
            //Arrange
            var automocker = new AutoMocker();

            var testCertificate = CertUtils.GenerateTestCert();
            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = true,
                    MasterKeyCertId = "master-key-cert",
                    KeyRotationInterval = TimeSpan.FromDays(3),
                    KeyRotationRollover = TimeSpan.FromDays(4)
                });

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(It.IsAny<string>(), out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(It.IsAny<string>()))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IAnonymousTokenMasterKeyCertificateLocator, Task<X509Certificate2>>(x => x.GetMasterKeyCertificate())
                .ReturnsAsync(testCertificate);

            var target = automocker.CreateInstance<AnonymousTokenKeyStore>();

            //Act
            var result = await target.GetActiveSigningKeyPair();

            //Assert
            var ecParameters = CustomNamedCurves.GetByOid(X9ObjectIdentifiers.Prime256v1);
            var initiator = new Initiator();
            var init = initiator.Initiate(ecParameters.Curve);
            var t = init.t;
            var r = init.r;
            var P = init.P;

            var tokenGenerator = new TokenGenerator();
            var (Q, c, z) = tokenGenerator.GenerateToken(result.PrivateKey, result.PublicKey.Q, ecParameters, P);

            var tokenVerifier = new TokenVerifier(new InMemorySeedStore());
            var W = initiator.RandomiseToken(ecParameters, result.PublicKey, P, Q, c, z, r);
            var isVerified = await tokenVerifier.VerifyTokenAsync(result.PrivateKey, ecParameters.Curve, t, W);
            isVerified.Should().BeTrue();
        }
    }
}
