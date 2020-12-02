using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional.Unsafe;

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
                .Setup<IOptions<AnonymousTokensConfig>, AnonymousTokensConfig>(x => x.Value)
                .Returns(new AnonymousTokensConfig
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
                .Setup<IOptions<AnonymousTokensConfig>, AnonymousTokensConfig>(x => x.Value)
                .Returns(new AnonymousTokensConfig
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

            automocker
                .Setup<IOptions<AnonymousTokensConfig>, AnonymousTokensConfig>(x => x.Value)
                .Returns(new AnonymousTokensConfig
                {
                    Enabled = true
                });

            automocker
                .Setup<IAnonymousTokenIssueRecordRepository, Task<IEnumerable<AnonymousTokenIssueRecord>>>(x => x.RetrieveRecordsJwtToken(It.IsAny<string>()))
                .Returns<string>(x => Task.FromResult(Enumerable.Empty<AnonymousTokenIssueRecord>()));

            automocker
                .Setup<IAnonymousTokensCertLocator, Task<X509Certificate2>>(x => x.GetCertificateAsync())
                .ReturnsAsync(new X509Certificate2()); // TODO: replace with certificate expected in test

            var target = automocker.CreateInstance<IssueAnonymousToken.Handler>();

            //Act
            var result = await target.Handle(new IssueAnonymousToken.Command
            {
                JwtTokenId = "token-a",
                JwtTokenExpiry = DateTime.Now.AddMinutes(10),
                RequestData = new AnonymousTokenRequest
                {
                    // TODO: add properties needed in the request
                }
            }, new CancellationToken());

            //Assert
            using (new AssertionScope())
            {
                result.HasValue.Should().BeTrue();
                var anonymousTokenReponse = result.ValueOrFailure();

                // TODO: add proper checks
                anonymousTokenReponse.Should().NotBeNull();
            }
        }
    }
}
