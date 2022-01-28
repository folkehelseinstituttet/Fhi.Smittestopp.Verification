using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Verifications;
using Fhi.Smittestopp.Verification.Server.Account;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using IdentityModel;
using IdentityServer4.Models;
using MediatR;
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
                .SetupOptions(new AnonymousTokensConfig
                {
                    Enabled = false
                })
                .SetupOptions(DefaultVerificationLimitConfig);

            var context = new ProfileDataRequestContext
            {
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new []
                {
                    new Claim(InternalClaims.NationalIdentifier, "08089409382"),
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
        public async Task GetProfileDataAsync_GivenNationalYoungerThanThreshold_GetsBlockedAndNoUploadClaim()
        {
            var automocker = new AutoMocker();

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    Enabled = true,
                    EnabledClientFlags = new[] { "some-flag", "some-other-flag" }
                })
                .SetupOptions(DefaultVerificationLimitConfig);

            var context = new ProfileDataRequestContext
            {
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(InternalClaims.NationalIdentifier, "01012068359"),
                    new Claim(InternalClaims.Pseudonym, "pseudo-1")
                })),
                RequestedClaimTypes = new[]
                {
                    JwtClaimTypes.Role, 
                    DkSmittestopClaims.Covid19Blocked,
                    DkSmittestopClaims.Covid19Status,
                    DkSmittestopClaims.Covid19LimitCount,
                    DkSmittestopClaims.Covid19LimitDuration,
                    
                }
            };

            var target = automocker.CreateInstance<ProfileService>();

            await target.GetProfileDataAsync(context);

            context.IssuedClaims.Should().NotContain(x => x.Type == JwtClaimTypes.Role && x.Value == VerificationRoles.UploadApproved);
            context.IssuedClaims.Should().Contain(x => x.Type == DkSmittestopClaims.Covid19Blocked && x.Value == "true");
            context.IssuedClaims.Should().Contain(x => x.Type == DkSmittestopClaims.Covid19LimitCount);
            context.IssuedClaims.Should().Contain(x => x.Type == DkSmittestopClaims.Covid19LimitDuration);
        }
        
        //01012068359

        [Test]
        public async Task GetProfileDataAsync_GivenPinUser_GetsRejected()
        {
            var automocker = new AutoMocker();
            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    Enabled = true,
                    EnabledClientFlags = new []{"some-flag", "some-other-flag"}
                })
                .SetupOptions(DefaultVerificationLimitConfig);

            var context = new ProfileDataRequestContext
            {
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(InternalClaims.PinVerified, "true"),
                    new Claim(InternalClaims.Pseudonym, "pseudo-1")
                })),
                RequestedClaimTypes = new[]
                {
                    DkSmittestopClaims.Covid19Status,
                    DkSmittestopClaims.Covid19Blocked,
                    JwtClaimTypes.Role
                }
            };

            var target = automocker.CreateInstance<ProfileService>();

            await target.GetProfileDataAsync(context);

            context.IssuedClaims.Should().NotContain(x => x.Type == DkSmittestopClaims.Covid19Status && x.Value == DkSmittestopClaims.StatusValues.Positive);
            context.IssuedClaims.Should().NotContain(x => x.Type == JwtClaimTypes.Role && x.Value == VerificationRoles.UploadApproved);
            context.IssuedClaims.Should().Contain(x => x.Type == DkSmittestopClaims.Covid19Blocked && x.Value == "true");
            automocker.VerifyAll();
        }

        [Test]
        public async Task GetProfileDataAsync_AnonymouTokensEnabled_IncludesAnonTokenClaims()
        {
            var automocker = new AutoMocker();

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    Enabled = true,
                    EnabledClientFlags = new[] { "some-flag", "some-other-flag" }
                })
                 .SetupOptions(DefaultVerificationLimitConfig);;

            var context = new ProfileDataRequestContext
            {
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(InternalClaims.NationalIdentifier, "08089409382"),
                    new Claim(InternalClaims.Pseudonym, "pseudo-1")
                })),
                RequestedClaimTypes = new[] { VerificationClaims.AnonymousToken }
            };

            var target = automocker.CreateInstance<ProfileService>();

            await target.GetProfileDataAsync(context);

            context.IssuedClaims.Should().Contain(x => x.Type == VerificationClaims.AnonymousToken && x.Value == "some-flag");
            context.IssuedClaims.Should().Contain(x => x.Type == VerificationClaims.AnonymousToken && x.Value == "some-other-flag");
        }


        [Test]
        public async Task GetProfileDataAsync_AnonymouTokensDisabled_DoesNotIncludeAnonymousTokenClaims()
        {
            var automocker = new AutoMocker();

            automocker
                .SetupOptions(new AnonymousTokensConfig
                {
                    Enabled = false,
                    EnabledClientFlags = new [] {"should-not-be-returned"}
                })
                .SetupOptions(DefaultVerificationLimitConfig);

            var context = new ProfileDataRequestContext
            {
                Subject = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(InternalClaims.NationalIdentifier, "01019098765"),
                    new Claim(InternalClaims.Pseudonym, "pseudo-1")
                })),
                RequestedClaimTypes = new[] { VerificationClaims.AnonymousToken }
            };

            var target = automocker.CreateInstance<ProfileService>();

            await target.GetProfileDataAsync(context);

            context.IssuedClaims.Should().NotContain(x => x.Type == VerificationClaims.AnonymousToken);
        }
        
        private static VerificationLimitConfig DefaultVerificationLimitConfig = new VerificationLimitConfig()
        {
            MaxLimitDuration = new TimeSpan(0,24,0),
            MaxVerificationsAllowed = 3,
            MinimumAgeInYears = 16
        };
    }
}
