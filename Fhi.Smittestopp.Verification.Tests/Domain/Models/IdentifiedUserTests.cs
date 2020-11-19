using System.Linq;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Models
{
    [TestFixture]
    public class IdentifiedUserTests
    {
        [Test]
        public void Constructor_CreatesWithUniqueIdAndProvidedInfo()
        {
            var target = new IdentifiedUser("01019098765", "pseudo-id-1");

            target.Id.Should().NotBeEmpty();
            target.NationalIdentifier.Should().Be("01019098765".Some());
            target.Pseudonym.Should().Be("pseudo-id-1");
        }

        [Test]
        public void IsPinVerified_ShouldBeFalse()
        {
            var target = new IdentifiedUser("01019098765", "pseudo-id-1");

            target.IsPinVerified.Should().BeFalse();
        }

        [Test]
        public void GetCustomClaims_ReturnsNationalIdAndPseudonym()
        {
            var target = new IdentifiedUser("01019098765", "pseudo-id-1");

            var customClaims = target.GetCustomClaims().ToList();

            customClaims.Should().Contain(x => x.Type == InternalClaims.NationalIdentifier && x.Value == "01019098765");
            customClaims.Should().Contain(x => x.Type == InternalClaims.Pseudonym && x.Value == "pseudo-id-1");
        }
    }
}
