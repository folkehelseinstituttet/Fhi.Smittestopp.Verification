using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Users;
using FluentAssertions;
using IdentityModel;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Users
{
    [TestFixture]
    public class CreateFromExternalAuthenticationTests
    {
        [Test]
        public void Handle_GivenNoPseudonymIdClaim_ThrowsException()
        {
            var automocker = new AutoMocker();

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
        public async Task Handle_GivenValidPseudoAndNationalId_ReturnsIdentifiedUser(string idClaimType)
        {
            var automocker = new AutoMocker();

            automocker.Setup<IPseudonymFactory, string>(x => x.Create(ExternalProviders.IdPorten + ":pseudo-id-123"))
                .Returns<string>(x => "internal-id-987");

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

            result.Id.Should().NotBeEmpty();
            result.Pseudonym.Should().Be("internal-id-987");
            result.NationalIdentifier.Should().Be("01019098765".Some());
            automocker.VerifyAll();
        }
    }
}
