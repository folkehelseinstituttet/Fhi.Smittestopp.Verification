using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Server.Account;
using FluentAssertions;
using IdentityServer4.Models;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Server.Account
{
    [TestFixture]
    public class ProfileServiceTests
    {
        [Test]
        public async Task IsActiveAsync_SetsIsActiveTrue()
        {
            var automocker = new AutoMocker();
            var context = new IsActiveContext(new ClaimsPrincipal(new ClaimsIdentity(new Claim[0], "idporten")), new Client(), "some-caller");

            var target = automocker.CreateInstance<ProfileService>();

            await target.IsActiveAsync(context);

            context.IsActive.Should().Be(true);
        }

        [Test]
        public async Task GetProfileDataAsync_GivenNationalIdentifiedWithPositiveResult_VerifiesStatusAndAddsRequestedClaim()
        {
            var automocker = new AutoMocker();

            var verificationLimit = new Mock<IVerificationLimit>();

            automocker
                .Setup<IMediator, Task<VerificationResult>>(x => x.Send(It.IsAny<VerifyIdentifiedUser.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VerificationResult(new PositiveTestResult
                {
                    PositiveTestDate = DateTime.Today.AddDays(-1).Some()
                }, new VerificationRecord[0], verificationLimit.Object));
            automocker
                .Setup<IOptions<AnonymousTokensConfig>, AnonymousTokensConfig>(x => x.Value)
                .Returns(new AnonymousTokensConfig
                {
                });

            var context = new ProfileDataRequestContext
            {
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new []
                {
                    new Claim(InternalClaims.NationalIdentifier, "01019098765"),
                    new Claim(InternalClaims.Pseudonym, "pseudo-1")
                })),
                RequestedClaimTypes = new []{ DkSmittestopClaims.Covid19Status }
            };

            var target = automocker.CreateInstance<ProfileService>();

            await target.GetProfileDataAsync(context);

            context.IssuedClaims.Should().Contain(x => x.Type == DkSmittestopClaims.Covid19Status && x.Value == DkSmittestopClaims.StatusValues.Positive);
            automocker.VerifyAll();
        }

        [Test]
        public async Task GetProfileDataAsync_GivenPinUser_VerifiesStatusAndAddsRequestedClaim()
        {
            var automocker = new AutoMocker();

            var verificationLimit = new Mock<IVerificationLimit>();

            automocker
                .Setup<IMediator, Task<VerificationResult>>(x => x.Send(It.IsAny<VerifyPinUser.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new VerificationResult(new PositiveTestResult(), new VerificationRecord[0], verificationLimit.Object));
            automocker
                .Setup<IOptions<AnonymousTokensConfig>, AnonymousTokensConfig>(x => x.Value)
                .Returns(new AnonymousTokensConfig
                {
                });

            var context = new ProfileDataRequestContext
            {
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(InternalClaims.PinVerified, "true"),
                    new Claim(InternalClaims.Pseudonym, "pseudo-1")
                })),
                RequestedClaimTypes = new[] { DkSmittestopClaims.Covid19Status }
            };

            var target = automocker.CreateInstance<ProfileService>();

            await target.GetProfileDataAsync(context);

            context.IssuedClaims.Should().Contain(x => x.Type == DkSmittestopClaims.Covid19Status && x.Value == DkSmittestopClaims.StatusValues.Positive);
            automocker.VerifyAll();
        }
    }
}
