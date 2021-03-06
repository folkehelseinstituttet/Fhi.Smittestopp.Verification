﻿using System;
using System.Linq;
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
using Org.BouncyCastle.Math;

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

            var masterKey = new byte[256];

            object cachedResult;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = false,
                    CurveName = "P-256",
                    MasterKeyCertId = "master-key-cert"
                });

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(It.IsAny<string>(), out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(It.IsAny<string>()))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IAnonymousTokenMasterKeyLoader, Task<byte[]>>(x => x.LoadMasterKeyBytes())
                .ReturnsAsync(masterKey);

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

            var masterKey = new byte[256];

            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = true,
                    CurveName = "P-256",
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
                .Setup<IAnonymousTokenMasterKeyLoader, Task<byte[]>>(x => x.LoadMasterKeyBytes())
                .ReturnsAsync(masterKey);

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

            var masterKey = new byte[256];

            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = false,
                    MasterKeyCertId = "master-key-cert",
                    CurveName = "P-256"
                });

            automocker
                .Setup<IMemoryCache, bool>(x => x.TryGetValue(It.IsAny<string>(), out cachedResult))
                .Returns(false);

            automocker
                .Setup<IMemoryCache, ICacheEntry>(x => x.CreateEntry(It.IsAny<string>()))
                .Returns(Mock.Of<ICacheEntry>());

            automocker
                .Setup<IAnonymousTokenMasterKeyLoader, Task<byte[]>>(x => x.LoadMasterKeyBytes())
                .ReturnsAsync(masterKey);

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

            var masterKey = new byte[256];

            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = true,
                    CurveName = "P-256",
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
                .Setup<IAnonymousTokenMasterKeyLoader, Task<byte[]>>(x => x.LoadMasterKeyBytes())
                .ReturnsAsync(masterKey);

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

            var masterKey = new byte[256];

            object cachedResult = null;

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    KeyRotationEnabled = true,
                    CurveName = "P-256",
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
                .Setup<IAnonymousTokenMasterKeyLoader, Task<byte[]>>(x => x.LoadMasterKeyBytes())
                .ReturnsAsync(masterKey);

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
            var (Q, c, z) = tokenGenerator.GenerateToken(result.PrivateKey, result.PublicKey, ecParameters, P);

            var keyDto = result.AsValidationKey().AsKeyDto();

            var clientSideEcParameters = CustomNamedCurves.GetByName(keyDto.Crv); // Matches keyDto.Crv == "P-256"
            var clientSidePublicKeyPoint = clientSideEcParameters.Curve.CreatePoint(new BigInteger(Convert.FromBase64String(keyDto.X)), new BigInteger(Convert.FromBase64String(keyDto.Y)));

            var W = initiator.RandomiseToken(clientSideEcParameters, clientSidePublicKeyPoint, P, Q, c, z, r);

            var tokenVerifier = new TokenVerifier(new InMemorySeedStore());
            var isVerified = await tokenVerifier.VerifyTokenAsync(result.PrivateKey, ecParameters.Curve, t, W);
            isVerified.Should().BeTrue();
        }
    }
}
