using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Verifications;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Verifications
{
    public class VerifyPinUserTests
    {

        [Test]
        public async Task Handle_GivenValidIdAndPositiveTest_ReturnsPositiveResult()
        {
            var automocker = new AutoMocker();
            
            var target = automocker.CreateInstance<VerifyPinUser.Handler>();

            var result = await target.Handle(new VerifyPinUser.Command("pseudo-1"), new CancellationToken());

            result.HasVerifiedPostiveTest.Should().BeTrue();
            result.PositiveTestDate.Should().Be(Option.None<DateTime>());
        }

        [Test]
        public async Task Handle_GivenPositiveTestLimitExceeded_ReturnsExceededAndConfig()
        {
            var automocker = new AutoMocker();

            var verLimitConfig = new Mock<IVerificationLimitConfig>();

            automocker
                .Setup<IVerificationLimit, bool>(x => x.HasReachedLimit(It.IsAny<IEnumerable<VerificationRecord>>()))
                .Returns(true);

            automocker
                .Setup<IVerificationLimit, IVerificationLimitConfig>(x => x.Config)
                .Returns(verLimitConfig.Object);

            var target = automocker.CreateInstance<VerifyPinUser.Handler>();

            var result = await target.Handle(new VerifyPinUser.Command("pseudo-1"), new CancellationToken());

            result.HasVerifiedPostiveTest.Should().BeTrue();
            result.VerificationLimitExceeded.Should().BeTrue();
            result.VerificationLimitConfig.Should().Be(verLimitConfig.Object.Some());

            automocker.VerifyAll();
        }
    }
}
