using System;
using System.Linq;
using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using IdentityModel;
using Moq;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Models
{
    [TestFixture]
    public class VerificationResultTests
    {
        [Test]
        public void GetVerificationClaims_ForNegativeResult_ShouldContainNegativeClaim()
        {
            var target = new VerificationResult();

            var verificationClaims = target.GetVerificationClaims().ToList();

            verificationClaims.Should().Contain(c => c.Type == DkSmittestopClaims.Covid19Status && c.Value == DkSmittestopClaims.StatusValues.Negative);
        }

        [Test]
        public void GetVerificationClaims_ForPositiveResult_ShouldBeTrue()
        {
            var target = new VerificationResult(new PositiveTestResult
            {
                PositiveTestDate = DateTime.Today.AddDays(-3).Some()
            }, new VerificationRecord[0], new Mock<IVerificationLimit>().Object);

            target.HasVerifiedPostiveTest.Should().BeTrue();
        }

        [Test]
        public void GetCustomClaims_ForNonPositiveUser_ShouldContainVerifiedPostiveRoleAndTestDateClaim()
        {
            var testdata = DateTime.Today.AddDays(-3);

            var target = new VerificationResult(new PositiveTestResult
            {
                PositiveTestDate = testdata.Some()
            }, new VerificationRecord[0], new Mock<IVerificationLimit>().Object);

            var verificationClaims = target.GetVerificationClaims().ToList();

            verificationClaims.Should().Contain(c => c.Type == JwtClaimTypes.Role && c.Value == VerificationRoles.VerifiedPositive);
            verificationClaims.Should().Contain(c => c.Type == VerificationClaims.VerifiedPositiveTestDate && c.Value == testdata.ToString("yyyy-MM-dd"));
        }
    }
}
