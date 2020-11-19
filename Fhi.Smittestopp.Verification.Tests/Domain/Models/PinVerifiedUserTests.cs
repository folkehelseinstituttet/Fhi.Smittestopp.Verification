using System.Linq;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Models
{
    [TestFixture]
    public class PinVerifiedUserTests
    {
        [Test]
        public void Constructor_CreatesUserWithTempIdAndPseudonym()
        {
            var target = new PinVerifiedUser("pseudo-1");

            target.Id.Should().NotBeEmpty();
            target.Pseudonym.Should().Be("pseudo-1");
            target.NationalIdentifier.Should().Be(Option.None<string>());
        }

        [Test]
        public void IsPinVerified_ShouldBeTrue()
        {
            var target = new PinVerifiedUser("pseudo-1");

            target.IsPinVerified.Should().BeTrue();
        }

        [Test]
        public void GetCustomClaims_ReturnsNationalIdAndPseudonym()
        {
            var target = new PinVerifiedUser("pseudo-id-1");

            var customClaims = target.GetCustomClaims().ToList();

            customClaims.Should().Contain(x => x.Type == InternalClaims.PinVerified && x.Value == "true");
            customClaims.Should().Contain(x => x.Type == InternalClaims.Pseudonym && x.Value == "pseudo-id-1");
        }
    }
}
