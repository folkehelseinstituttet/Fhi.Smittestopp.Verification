using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using Fhi.Smittestopp.Verification.Server.AnonymousTokens;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using IdentityModel;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Server.AnonymousTokens
{
    [TestFixture]
    public class AnonymousTokensControllerTests
    {
        [Test]
        public void IssueNewToken_JwtTokenIdClaimMissing_ThrowsException()
        {
            //Arrange
            var request = new AnonymousTokenRequest();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new []
            {
                new Claim(JwtClaimTypes.Expiration, DateTimeOffset.Now.ToUnixTimeSeconds().ToString())
            }));

            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<AnonymousTokensController>().SetUserForContext(user);
            
            //Act/Assert
            Assert.ThrowsAsync<Exception>(() => target.IssueNewToken(request));
        }

        [Test]
        public void IssueNewToken_JwtTokenIssuedAtClaimMissing_ThrowsException()
        {
            //Arrange
            var tokenId = Guid.NewGuid();
            var request = new AnonymousTokenRequest();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(JwtClaimTypes.JwtId, tokenId.ToString()),
            }));

            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<AnonymousTokensController>().SetUserForContext(user);

            //Act/Assert
            Assert.ThrowsAsync<Exception>(() => target.IssueNewToken(request));
        }

        [Test]
        public async Task IssueNewToken_RequiredClaimsPresentAndIssueOk_ReturnsAnonymousTokenResponse()
        {
            //Arrange
            var tokenId = Guid.NewGuid();
            var request = new AnonymousTokenRequest();
            var response = new AnonymousTokenResponse();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(JwtClaimTypes.JwtId, tokenId.ToString()),
                new Claim(JwtClaimTypes.Expiration, DateTimeOffset.Now.ToUnixTimeSeconds().ToString())
            }));

            var automocker = new AutoMocker();
            automocker.Setup<IMediator, Task<Option<AnonymousTokenResponse, string>>>(x => x.Send(
                It.IsAny<IssueAnonymousToken.Command>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(response.Some<AnonymousTokenResponse, string>());

            var target = automocker.CreateInstance<AnonymousTokensController>().SetUserForContext(user);

            //Act
            var result = await target.IssueNewToken(request);

            result.Value.Should().Be(response);
        }


        [Test]
        public async Task IssueNewToken_RequiredClaimsPresentAndIssueRejected_ReturnsConflictResponse()
        {
            //Arrange
            var tokenId = Guid.NewGuid();
            var request = new AnonymousTokenRequest();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(JwtClaimTypes.JwtId, tokenId.ToString()),
                new Claim(JwtClaimTypes.Expiration, DateTimeOffset.Now.ToUnixTimeSeconds().ToString())
            }));

            var automocker = new AutoMocker();
            automocker.Setup<IMediator, Task<Option<AnonymousTokenResponse, string>>>(x => x.Send(
                    It.IsAny<IssueAnonymousToken.Command>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Option.None<AnonymousTokenResponse, string>("Rejected."));

            var target = automocker.CreateInstance<AnonymousTokensController>().SetUserForContext(user);

            //Act
            var result = await target.IssueNewToken(request);

            result.Result.Should().BeOfType<ConflictObjectResult>();
        }
    }
}
