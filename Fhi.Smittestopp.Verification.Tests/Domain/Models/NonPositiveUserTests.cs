using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Models
{
    [TestFixture]
    public class NonPositiveUserTests
    {
        [Test]
        public void Constructor_CreatesUserWithTempId()
        {
            var target = new NonPositiveUser();

            target.Id.Should().NotBeEmpty();
        }

        [Test]
        public void HasVerifiedPostiveTest_ForNonPositiveUser_ShouldBeFalse()
        {
            var target = new NonPositiveUser();

            target.HasVerifiedPostiveTest.Should().BeFalse();
        }

        [Test]
        public void GetCustomClaims_ForNonPositiveUser_ShouldContainNegativeClaim()
        {
            var target = new NonPositiveUser();

            target.GetCustomClaims().Should().Contain(c => c.Type == DkSmittestopClaims.Covid19Status && c.Value == DkSmittestopClaims.StatusValues.Negative);
        }
    }
}
