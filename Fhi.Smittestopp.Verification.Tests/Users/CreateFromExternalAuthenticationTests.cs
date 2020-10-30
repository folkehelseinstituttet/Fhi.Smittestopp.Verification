using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Users;
using FluentAssertions;
using IdentityModel;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Users
{
    [TestFixture]
    public class CreateFromExternalAuthenticationTests
    {
        [Test]
        public void Handle_GivenVerifiedPostiveButNoPseudonymIdClaim_ThrowsException()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMsisLookupService, Task<Option<PositiveTestResult>>>(x =>
                    x.FindPositiveTestResult("01019098765"))
                .ReturnsAsync(new PositiveTestResult
                {
                    PositiveTestDate = DateTime.Today.AddDays(-7).Some()
                }.Some);

            var target = automocker.CreateInstance<CreateFromExternalAuthentication.Handler>();

            Assert.ThrowsAsync<Exception>(() => target.Handle(new CreateFromExternalAuthentication.Command
            (
                ExternalProviders.IdPorten,
                new List<Claim>
                {
                    new Claim("not-sub-id-claim", "not a sub-id"),
                    new Claim(IdPortenClaims.NationalIdentifier, "01019098765")
                }
            ), new CancellationToken()));
        }

        [TestCase(JwtClaimTypes.Subject)]
        [TestCase(ClaimTypes.NameIdentifier)]
        public async Task Handle_GivenValidIdNoPositiveTest_ReturnsEmptyTempUser(string idClaimType)
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMsisLookupService, Task<Option<PositiveTestResult>>>(x =>
                    x.FindPositiveTestResult("01019098765"))
                .ReturnsAsync(Option.None<PositiveTestResult>());

            var target = automocker.CreateInstance<CreateFromExternalAuthentication.Handler>();

            var result = await target.Handle(new CreateFromExternalAuthentication.Command
            (
                ExternalProviders.IdPorten,
                new List<Claim>
                {
                    new Claim(idClaimType, "pseudo-id-123"),
                    new Claim(IdPortenClaims.NationalIdentifier, "01019098765")
                }
            ), new CancellationToken());

            result.HasVerifiedPostiveTest.Should().BeFalse();
            result.Id.Should().NotBeEmpty();
            result.PositiveTestDate.Should().Be(Option.None<DateTime>());
            automocker.VerifyAll();
        }

        [TestCase(JwtClaimTypes.Subject)]
        [TestCase(ClaimTypes.NameIdentifier)]
        public async Task Handle_GivenValidIdAndPositiveTest_ReturnsPositiveUser(string idClaimType)
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMsisLookupService, Task<Option<PositiveTestResult>>>(x =>
                    x.FindPositiveTestResult("01019098765"))
                .ReturnsAsync(new PositiveTestResult
                {
                    PositiveTestDate = DateTime.Today.AddDays(-7).Some()
                }.Some);

            var target = automocker.CreateInstance<CreateFromExternalAuthentication.Handler>();

            var result = await target.Handle(new CreateFromExternalAuthentication.Command
            (
                ExternalProviders.IdPorten,
                new List<Claim>
                {
                    new Claim(idClaimType, "pseudo-id-123"),
                    new Claim(IdPortenClaims.NationalIdentifier, "01019098765")
                }
            ), new CancellationToken());

            result.HasVerifiedPostiveTest.Should().BeTrue();
            result.Id.Should().Be(ExternalProviders.IdPorten + ":pseudo-id-123");
            result.PositiveTestDate.Should().Be(DateTime.Today.AddDays(-7).Some());
            automocker.VerifyAll();
        }
    }
}
