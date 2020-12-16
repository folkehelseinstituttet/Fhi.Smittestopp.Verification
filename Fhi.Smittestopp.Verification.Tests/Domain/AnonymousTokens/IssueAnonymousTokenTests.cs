using AnonymousTokens.Client.Protocol;
using AnonymousTokens.Core.Services;
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

using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities.Encoders;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using Org.BouncyCastle.Math.EC;

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

            var ecParameters = CustomNamedCurves.GetByOid(X9ObjectIdentifiers.Prime256v1);

            var initiator = new Initiator();
            var init = initiator.Initiate(ecParameters.Curve);
            var t = init.t;
            var r = init.r;
            var P = init.P;

            var tokenGenerator = new TokenGenerator();
            var privateKey = await new InMemoryPrivateKeyStore().GetAsync();
            var publicKey = await new InMemoryPublicKeyStore().GetAsync();

            var (expectedQ, expectedProofC, expectedProofZ) = tokenGenerator.GenerateToken(privateKey, publicKey.Q, ecParameters, P);

            var tokenVerifier = new TokenVerifier(new InMemorySeedStore());

            var tokenRequest = new IssueAnonymousToken.Command
            {
                JwtTokenId = "token-a",
                JwtTokenExpiry = DateTime.Now.AddMinutes(10),
                RequestData = new AnonymousTokenRequest
                {
                    PAsHex = Hex.ToHexString(P.GetEncoded())
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
                .ReturnsAsync(new AnonymousTokenSigningKeypair("some-kid-123", privateKey, publicKey));

            automocker
                .Setup<IPrivateKeyStore, Task<BigInteger>>(x => x.GetAsync()).Returns(new InMemoryPrivateKeyStore().GetAsync());

            automocker
                .Setup<IPublicKeyStore, Task<ECPublicKeyParameters>>(x => x.GetAsync()).Returns(new InMemoryPublicKeyStore().GetAsync());

            automocker
                .Setup<ITokenGenerator, (ECPoint, BigInteger, BigInteger)>(x => x.GenerateToken(privateKey, publicKey.Q, ecParameters, It.Is<ECPoint>(x => x.Equals(P))))
                .Returns((expectedQ, expectedProofC, expectedProofZ));

            var target = automocker.CreateInstance<IssueAnonymousToken.Handler>();

            //Act           
            var result = await target.Handle(tokenRequest, new CancellationToken());

            //Assert
            using (new AssertionScope())
            {
                result.HasValue.Should().BeTrue();
                var anonymousTokenReponse = result.ValueOrFailure();

                anonymousTokenReponse.Should().NotBeNull();

                var Q = ecParameters.Curve.DecodePoint(Hex.Decode(anonymousTokenReponse.QAsHex));
                var c = new BigInteger(Hex.Decode(anonymousTokenReponse.ProofCAsHex));
                var z = new BigInteger(Hex.Decode(anonymousTokenReponse.ProofZAsHex));

                Q.Should().Be(expectedQ);
                c.Should().Be(expectedProofC);
                z.Should().Be(expectedProofZ);

                var W = initiator.RandomiseToken(ecParameters, publicKey, P, Q, c, z, r);
                var isVerified = await tokenVerifier.VerifyTokenAsync(privateKey, ecParameters.Curve, t, W);
                isVerified.Should().BeTrue();
            }
        }
    }
}
