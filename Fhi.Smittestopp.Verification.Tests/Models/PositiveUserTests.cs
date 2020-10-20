using System;
using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using IdentityModel;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Models
{
    [TestFixture]
    public class PositiveUserTests
    {
        [Test]
        public void Constructor_CreatesUserWithIdBasedOnProviderId()
        {
            var target = new PositiveUser("idporten", "pseudo-id-1", new PositiveTestResult
            {
                PositiveTestDate = DateTime.Today.AddDays(-3).Some()
            });

            target.Id.Should().Be("idporten:pseudo-id-1");
        }

        [Test]
        public void HasVerifiedPostiveTest_ForPositiveUser_ShouldBeTrue()
        {
            var target = new PositiveUser("idporten", "pseudo-id-1", new PositiveTestResult
            {
                PositiveTestDate = DateTime.Today.AddDays(-3).Some()
            });

            target.HasVerifiedPostiveTest.Should().BeTrue();
        }

        [Test]
        public void GetCustomClaims_ForNonPositiveUser_ShouldContainVerifiedPostiveRoleAndTestDateClaim()
        {
            var testdata = DateTime.Today.AddDays(-3);
            var target = new PositiveUser("idporten", "pseudo-id-1", new PositiveTestResult
            {
                PositiveTestDate = testdata.Some()
            });

            target.GetCustomClaims().Should().Contain(c => c.Type == JwtClaimTypes.Role && c.Value == VerificationRoles.VerifiedPositive);
            target.GetCustomClaims().Should().Contain(c => c.Type == VerificationClaims.VerifiedPositiveTestDate && c.Value == testdata.ToString("yyyy-MM-dd"));
        }
    }
}
