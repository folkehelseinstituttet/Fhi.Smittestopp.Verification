using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using NUnit.Framework;

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
        }

        [Test]
        public void IsPinVerified_ShouldBeTrue()
        {
            var target = new PinVerifiedUser("pseudo-1");

            target.IsPinVerified.Should().BeTrue();
        }
    }
}
