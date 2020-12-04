using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Verifications;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Verifications
{
    [TestFixture]
    public class VerifyIdentifiedUserTests
    {
        [Test]
        public async Task Handle_WhenNoTestResultIsFound_ReturnsNegativeResult()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMsisLookupService, Task<Option<PositiveTestResult>>>(x =>
                    x.FindPositiveTestResult("01019098765"))
                .ReturnsAsync(Option.None<PositiveTestResult>());

            automocker.Setup<IOptions<VerifyIdentifiedUser.Config>, VerifyIdentifiedUser.Config>(x => x.Value)
                .Returns(new VerifyIdentifiedUser.Config
                {
                    UseFixedTestCases = false
                });

            var target = automocker.CreateInstance<VerifyIdentifiedUser.Handler>();

            var result = await target.Handle(new VerifyIdentifiedUser.Command("01019098765", "pseudo-1"), new CancellationToken());

            result.HasVerifiedPostiveTest.Should().BeFalse();
            result.PositiveTestDate.Should().Be(Option.None<DateTime>());

            automocker.VerifyAll();
        }

        [Test]
        public async Task Handle_GivenValidIdAndPositiveTest_ReturnsPositiveResult()
        {
            var automocker = new AutoMocker();

            automocker
                .Setup<IMsisLookupService, Task<Option<PositiveTestResult>>>(x =>
                    x.FindPositiveTestResult("01019098765"))
                .ReturnsAsync(new PositiveTestResult
                {
                    PositiveTestDate = DateTime.Today.AddDays(-7).Some()
                }.Some);

            automocker.Setup<IOptions<VerifyIdentifiedUser.Config>, VerifyIdentifiedUser.Config>(x => x.Value)
                .Returns(new VerifyIdentifiedUser.Config
                {
                    UseFixedTestCases = false
                });

            var target = automocker.CreateInstance<VerifyIdentifiedUser.Handler>();

            var result = await target.Handle(new VerifyIdentifiedUser.Command("01019098765", "pseudo-1"), new CancellationToken());

            result.HasVerifiedPostiveTest.Should().BeTrue();
            result.PositiveTestDate.Should().Be(DateTime.Today.AddDays(-7).Some());
            automocker.VerifyAll();
        }

        [Test]
        public async Task Handle_GivenPositiveTestLimitExceeded_ReturnsExceededAndConfig()
        {
            var automocker = new AutoMocker();

            var verLimitConfig = new Mock<IVerificationLimitConfig>();

            automocker
                .Setup<IMsisLookupService, Task<Option<PositiveTestResult>>>(x =>
                    x.FindPositiveTestResult("01019098765"))
                .ReturnsAsync(new PositiveTestResult
                {
                    PositiveTestDate = DateTime.Today.AddDays(-7).Some()
                }.Some);

            automocker.Setup<IOptions<VerifyIdentifiedUser.Config>, VerifyIdentifiedUser.Config>(x => x.Value)
                .Returns(new VerifyIdentifiedUser.Config
                {
                    UseFixedTestCases = false
                });

            automocker
                .Setup<IVerificationLimit, bool>(x => x.HasReachedLimit(It.IsAny<IEnumerable<VerificationRecord>>()))
                .Returns(true);

            automocker
                .Setup<IVerificationLimit, IVerificationLimitConfig>(x => x.Config)
                .Returns(verLimitConfig.Object);

            var target = automocker.CreateInstance<VerifyIdentifiedUser.Handler>();

            var result = await target.Handle(new VerifyIdentifiedUser.Command("01019098765", "pseudo-1"), new CancellationToken());

            result.HasVerifiedPostiveTest.Should().BeTrue();
            result.VerificationLimitExceeded.Should().BeTrue();
            result.VerificationLimitConfig.Should().Be(verLimitConfig.Object.Some());

            automocker.VerifyAll();
        }
    }
}
