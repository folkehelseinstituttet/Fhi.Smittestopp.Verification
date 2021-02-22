using AnonymousTokens.Client.Protocol;
using AnonymousTokens.Core.Services.InMemory;
using AnonymousTokens.Server.Protocol;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional.Unsafe;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server.Credentials;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using Microsoft.Extensions.Caching.Memory;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Fhi.Smittestopp.Verification.Tests.Domain.AnonymousTokens
{
    [TestFixture]
    public class IssueAnonymousTokenTests
    {
        [Test]
        public async Task Handle_GivenFeatureNotEnabled_ReturnsNotEnabledErrorResult()
        {
            //Arrange
            var automocker = new AutoMocker();

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    Enabled = false
                });

            var target = automocker.CreateInstance<IssueAnonymousToken.Handler>();

            //Act
            var result = await target.Handle(new IssueAnonymousToken.Command
            {
                JwtTokenId = "token-a",
                JwtTokenExpiry = DateTime.Now.AddMinutes(10),
                RequestData = new AnonymousTokenRequest()
            }, new CancellationToken());

            //Assert
            using (new AssertionScope())
            {
                result.HasValue.Should().BeFalse();
                result.MatchNone(e => e.Should().Be("Anonymous tokens are not enabled for this environment."));
            }
        }

        [Test]
        public async Task Handle_GivenTokenAlreadyIssuedForJwt_ReturnsAlreadyIssuedErrorResult()
        {
            //Arrange
            var automocker = new AutoMocker();

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    Enabled = true
                });

            automocker
                .Setup<IAnonymousTokenIssueRecordRepository, Task<IEnumerable<AnonymousTokenIssueRecord>>>(x => x.RetrieveRecordsJwtToken(It.IsAny<string>()))
                .Returns<string>(x => Task.FromResult<IEnumerable<AnonymousTokenIssueRecord>>(new[]
                {
                    new AnonymousTokenIssueRecord(x, DateTime.Now.AddMinutes(10))
                }));

            var target = automocker.CreateInstance<IssueAnonymousToken.Handler>();

            //Act
            var result = await target.Handle(new IssueAnonymousToken.Command
            {
                JwtTokenId = "token-a",
                JwtTokenExpiry = DateTime.Now.AddMinutes(10),
                RequestData = new AnonymousTokenRequest()
            }, new CancellationToken());

            //Assert
            using (new AssertionScope())
            {
                result.HasValue.Should().BeFalse();
                result.MatchNone(e => e.Should().Be("Anonymous token already issued for the provided JWT-token ID."));
            }
        }

        [Test]
        public async Task Handle_GivenRequestForNewToken_ReturnsAnonymousTokenResponse()
        {
            //Arrange
            var automocker = new AutoMocker();

            var curveName = "P-256";
            var ecParameters = CustomNamedCurves.GetByName(curveName);

            var initiator = new Initiator();
            var init = initiator.Initiate(ecParameters.Curve);
            var t = init.t;
            var r = init.r;
            var P = init.P;

            var privateKey = await new InMemoryPrivateKeyStore().GetAsync();
            var publicKey = (await new InMemoryPublicKeyStore().GetAsync()).Q;
            var signingKeyPair = new AnonymousTokenSigningKeypair("some-kid-123", curveName, privateKey, publicKey);

            var tokenGenerator = new TokenGenerator();
            var (expectedSignedPoint, expectedProofChallenge, expectedProofResponse) = tokenGenerator.GenerateToken(privateKey, publicKey, ecParameters, P);

            var tokenVerifier = new TokenVerifier(new InMemorySeedStore());

            var tokenRequest = new IssueAnonymousToken.Command
            {
                JwtTokenId = "token-a",
                JwtTokenExpiry = DateTime.Now.AddMinutes(10),
                RequestData = new AnonymousTokenRequest
                {
                    MaskedPoint = Convert.ToBase64String(P.GetEncoded())
                }
            };

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    Enabled = true
                });

            automocker
                .Setup<IAnonymousTokenIssueRecordRepository, Task<IEnumerable<AnonymousTokenIssueRecord>>>(x => x.RetrieveRecordsJwtToken(It.IsAny<string>()))
                .Returns<string>(x => Task.FromResult(Enumerable.Empty<AnonymousTokenIssueRecord>()));

            automocker
                .Setup<IAnonymousTokensKeyStore, Task<AnonymousTokenSigningKeypair>>(x => x.GetActiveSigningKeyPair())
                .ReturnsAsync(signingKeyPair);

            automocker
                .Setup<ITokenGenerator, (ECPoint, BigInteger, BigInteger)>(x => x.GenerateToken(privateKey, publicKey, ecParameters, It.Is<ECPoint>(y => y.Equals(P))))
                .Returns((expectedSignedPoint, expectedProofChallenge, expectedProofResponse));

            var target = automocker.CreateInstance<IssueAnonymousToken.Handler>();

            //Act           
            var result = await target.Handle(tokenRequest, new CancellationToken());

            //Assert
            using (new AssertionScope())
            {
                result.HasValue.Should().BeTrue();
                var anonymousTokenResponse = result.ValueOrFailure();

                anonymousTokenResponse.Kid.Should().Be("some-kid-123");

                var Q = ecParameters.Curve.DecodePoint(Convert.FromBase64String(anonymousTokenResponse.SignedPoint));
                var c = new BigInteger(Convert.FromBase64String(anonymousTokenResponse.ProofChallenge));
                var z = new BigInteger(Convert.FromBase64String(anonymousTokenResponse.ProofResponse));

                Q.Should().Be(expectedSignedPoint);
                c.Should().Be(expectedProofChallenge);
                z.Should().Be(expectedProofResponse);

                var W = initiator.RandomiseToken(ecParameters, publicKey, P, Q, c, z, r);
                var isVerified = await tokenVerifier.VerifyTokenAsync(privateKey, ecParameters.Curve, t, W);
                isVerified.Should().BeTrue();
            }
        }

        [Test]
        public async Task Handle_GivenValidRequest_ShouldCreateResponseClientCanRandomizeAndUseForAuthorization()
        {
            //Arrange
            var config = new AnonymousTokensConfig
            {
                Enabled = true,
                CurveName = "P-256",
                KeyRotationEnabled = true,
                //MasterKeyCertId = "<your cert thumprint>", // uncomment to switch to use local certificate
                KeyRotationInterval = TimeSpan.FromDays(3),
                KeyRotationRollover = TimeSpan.FromHours(1)
            };

            var masterKey = new byte[256];

            var automocker = new AutoMocker();

            automocker
                .SetupOptions(config);

            automocker
                .Use<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));

            // comment to switch to use local certificate
            automocker
                .Setup<IAnonymousTokenMasterKeyLoader, Task<byte[]>>(x => x.LoadMasterKeyBytes())
                .ReturnsAsync(masterKey);
            // uncomment to switch to use local certificate
            //automocker.Use<ICertificateLocator>(new LocalCertificateLocator());
            //automocker.Use<IAnonymousTokenMasterKeyLoader>(automocker.CreateInstance<AnonymousTokenMasterKeyLoader>());

            automocker
                .Use<IAnonymousTokensKeyStore>(automocker.CreateInstance<AnonymousTokenKeyStore>());

            automocker
                .Use<ITokenGenerator>(new TokenGenerator());

            automocker
                .Setup<IAnonymousTokenIssueRecordRepository, Task<IEnumerable<AnonymousTokenIssueRecord>>>(x => x.RetrieveRecordsJwtToken(It.IsAny<string>()))
                .Returns<string>(x => Task.FromResult(Enumerable.Empty<AnonymousTokenIssueRecord>()));

            var targetA = automocker.CreateInstance<IssueAnonymousToken.Handler>();

            //Act: Part 1 - Issue token
            var ecParameters = CustomNamedCurves.GetByName("P-256");

            var initiator = new Initiator();
            var init = initiator.Initiate(ecParameters.Curve);

            var tokenRequest = new IssueAnonymousToken.Command
            {
                JwtTokenId = "token-a",
                JwtTokenExpiry = DateTime.Now.AddMinutes(10),
                RequestData = new AnonymousTokenRequest
                {
                    MaskedPoint = Convert.ToBase64String(init.P.GetEncoded())
                }
            };

            var tokenResponse = (await targetA.Handle(tokenRequest, new CancellationToken())).ValueOrFailure();

            tokenResponse.Should().NotBeNull();

            //Act: Part 2 - Validate, randomize and create auth header
            var targetB = automocker.CreateInstance<GetAnonymousTokenKeySet.Handler>();

            var keysetResponse = await targetB.Handle(new GetAnonymousTokenKeySet.Query(), new CancellationToken());

            var rawPublicKey = keysetResponse.Keys.First(x => x.Kid == tokenResponse.Kid);
            var curve = CustomNamedCurves.GetByName(rawPublicKey.Crv);
            var publicKey = curve.Curve.CreatePoint(
                new BigInteger(Convert.FromBase64String(rawPublicKey.X)),
                new BigInteger(Convert.FromBase64String(rawPublicKey.Y))
            );

            var signedPoint = ecParameters.Curve.DecodePoint(Convert.FromBase64String(tokenResponse.SignedPoint));
            var proofChallenge = new BigInteger(Convert.FromBase64String(tokenResponse.ProofChallenge));
            var proofResponse = new BigInteger(Convert.FromBase64String(tokenResponse.ProofResponse));
            var randomizedToken = initiator.RandomiseToken(ecParameters, publicKey, init.P, signedPoint, proofChallenge, proofResponse, init.r);

            var encodedToken = Convert.ToBase64String(randomizedToken.GetEncoded()) + "." + Convert.ToBase64String(init.t) + "." +
                               tokenResponse.Kid;

            encodedToken.Should().NotBeNullOrEmpty();
        }
    }
}
