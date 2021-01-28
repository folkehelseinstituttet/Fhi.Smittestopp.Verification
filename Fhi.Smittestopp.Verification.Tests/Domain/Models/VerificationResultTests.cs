using System;
using System.Collections.Generic;
using System.Linq;
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
        public void HasVerifiedPostiveTest_ForNegativeResult_ShouldContainNegativeClaim()
        {
            var target = new VerificationResult();

            target.HasVerifiedPostiveTest.Should().BeFalse();
        }

        [Test]
        public void CanUploadKeys_ForNegativeResult_ShouldContainNegativeClaim()
        {
            var target = new VerificationResult();

            target.CanUploadKeys.Should().BeFalse();
        }

        [Test]
        public void GetVerificationClaims_ForNegativeResult_ShouldContainNegativeClaim()
        {
            var target = new VerificationResult();

            var verificationClaims = target.GetVerificationClaims().ToList();

            verificationClaims.Should().Contain(c => c.Type == DkSmittestopClaims.Covid19Status && c.Value == DkSmittestopClaims.StatusValues.Negative);
            verificationClaims.Should().NotContain(c => c.Type == JwtClaimTypes.Role);
        }

        [Test]
        public void HasVerifiedPostiveTest_ForPositiveResult_ShouldBeTrue()
        {
            var target = new VerificationResult(new PositiveTestResult
            {
                PositiveTestDate = DateTime.Today.AddDays(-3).Some()
            }, new VerificationRecord[0], Mock.Of<IVerificationLimit>());

            target.HasVerifiedPostiveTest.Should().BeTrue();
        }

        [Test]
        public void CanUploadKeys_ForPositiveResultNotBlocked_ShouldBeTrue()
        {
            var verificationLimitMock = new Mock<IVerificationLimit>();

            verificationLimitMock
                .Setup(x => x.HasReachedLimit(It.IsAny<IEnumerable<VerificationRecord>>()))
                .Returns(false);

            var target = new VerificationResult(new PositiveTestResult
            {
                PositiveTestDate = DateTime.Today.AddDays(-3).Some()
            }, new VerificationRecord[0], verificationLimitMock.Object);

            target.CanUploadKeys.Should().BeTrue();
        }

        [Test]
        public void CanUploadKeys_ForPositiveResultBlocked_ShouldBeFalse()
        {
            var verificationLimitMock = new Mock<IVerificationLimit>();

            verificationLimitMock
                .Setup(x => x.HasReachedLimit(It.IsAny<IEnumerable<VerificationRecord>>()))
                .Returns(true);

            var target = new VerificationResult(new PositiveTestResult
            {
                PositiveTestDate = DateTime.Today.AddDays(-3).Some()
            }, new VerificationRecord[0], verificationLimitMock.Object);

            target.CanUploadKeys.Should().BeFalse();
        }

        [Test]
        public void GetCustomClaims_ForPositiveNonBlockedUser_ShouldContainCanUploadClaimsButNotBlockedConfigClaims()
        {
            var testdata = DateTime.Today.AddDays(-3);

            var verificationLimitMock = new Mock<IVerificationLimit>();

            verificationLimitMock
                .Setup(x => x.HasReachedLimit(It.IsAny<IEnumerable<VerificationRecord>>()))
                .Returns(false);

            var target = new VerificationResult(new PositiveTestResult
            {
                PositiveTestDate = testdata.Some()
            }, new VerificationRecord[0], verificationLimitMock.Object);

            var verificationClaims = target.GetVerificationClaims().ToList();

            verificationClaims.Should().Contain(c => c.Type == DkSmittestopClaims.Covid19Status && c.Value == DkSmittestopClaims.StatusValues.Positive);
            verificationClaims.Should().Contain(c => c.Type == JwtClaimTypes.Role && c.Value == VerificationRoles.UploadApproved);
            verificationClaims.Should().Contain(c => c.Type == VerificationClaims.VerifiedPositiveTestDate && c.Value == testdata.ToString("yyyy-MM-dd"));
            verificationClaims.Should().Contain(c => c.Type == DkSmittestopClaims.Covid19Blocked && c.Value == "false");
            verificationClaims.Should().NotContain(c => c.Type == DkSmittestopClaims.Covid19LimitCount);
            verificationClaims.Should().NotContain(c => c.Type == DkSmittestopClaims.Covid19LimitDuration);
        }

        [Test]
        public void GetCustomClaims_ForPositiveBlockedUser_ShouldContainResultClaimsBlockedClaimsAndNotCanUploadClaims()
        {
            var testdata = DateTime.Today.AddDays(-3);

            var positiveTestResult = new PositiveTestResult
            {
                PositiveTestDate = testdata.Some()
            };

            var verificationLimitMock = new Mock<IVerificationLimit>();

            verificationLimitMock
                .Setup(x => x.HasReachedLimit(It.IsAny<IEnumerable<VerificationRecord>>()))
                .Returns(true);

            verificationLimitMock
                .Setup(x => x.Config)
                .Returns(new VerificationLimitConfig
                {
                    MaxLimitDuration = TimeSpan.FromDays(2),
                    MaxVerificationsAllowed = 42
                });

            var target = new VerificationResult(positiveTestResult, new VerificationRecord[0], verificationLimitMock.Object);

            var verificationClaims = target.GetVerificationClaims().ToList();

            verificationClaims.Should().Contain(c => c.Type == DkSmittestopClaims.Covid19Status && c.Value == DkSmittestopClaims.StatusValues.Positive);
            verificationClaims.Should().Contain(c => c.Type == VerificationClaims.VerifiedPositiveTestDate && c.Value == testdata.ToString("yyyy-MM-dd"));
            verificationClaims.Should().Contain(c => c.Type == DkSmittestopClaims.Covid19Blocked && c.Value == "true");
            verificationClaims.Should().Contain(c => c.Type == DkSmittestopClaims.Covid19LimitCount);
            verificationClaims.Should().Contain(c => c.Type == DkSmittestopClaims.Covid19LimitDuration);
            verificationClaims.Should().NotContain(c => c.Type == JwtClaimTypes.Role);
        }
    }
}
