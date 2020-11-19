using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Models
{
    [TestFixture]
    public class IdentifiedUserTests
    {
        [Test]
        public void Constructor_CreatesUserWithIdBasedOnProviderId()
        {
            var target = new IdentifiedUser("idporten", "pseudo-id-1");

            target.Id.Should().NotBeEmpty();
        }

        [Test]
        public void IsPinVerified_ShouldBeFalse()
        {
            var target = new IdentifiedUser("idporten", "pseudo-id-1");

            target.IsPinVerified.Should().BeFalse();
        }
    }
}
