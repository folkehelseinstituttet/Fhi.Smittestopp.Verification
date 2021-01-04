using System;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Models
{
    [TestFixture]
    public class VerificationLimitTests
    {
        [Test]
        public void RecordsCutoff_GivenDurationConfig_ReturnsUtcNowMinusDuration()
        {
            //Arrange
            var limitDuration = TimeSpan.FromHours(3);
            var epsilon = TimeSpan.FromMilliseconds(1);
            var config = new VerificationLimitConfig
            {
                MaxLimitDuration = limitDuration
            };

            var automocker = new AutoMocker();

            automocker.SetupOptions(config);

            var target = automocker.CreateInstance<VerificationLimit>();

            //Act
            var testStart = DateTime.UtcNow;
            var result = target.RecordsCutoff;
            var testEnd = DateTime.UtcNow;

            //Assert
            result.Should().BeAfter(testStart - limitDuration - epsilon).And.BeBefore(testEnd - limitDuration + epsilon);
        }

        [Test]
        public void Config_ReturnsActiveConfig()
        {
            //Arrange
            var config = new VerificationLimitConfig();

            var automocker = new AutoMocker();

            automocker.SetupOptions(config);

            var target = automocker.CreateInstance<VerificationLimit>();

            //Act
            var result = target.Config;

            //Assert
            result.Should().Be(config);
        }

        [Test]
        public void HasReachedLimit_NumberOfRecordsAboveLimit_ReturnsTrue()
        {
            //Arrange
            var records = new []
            {
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-23)),
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-21)),
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-19)),
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-16))
            };

            var config = new VerificationLimitConfig
            {
                MaxLimitDuration = TimeSpan.FromHours(24),
                MaxVerificationsAllowed = 3
            };

            var automocker = new AutoMocker();

            automocker.SetupOptions(config);

            var target = automocker.CreateInstance<VerificationLimit>();

            //Act
            var result = target.HasReachedLimit(records);

            //Assert
            result.Should().BeTrue();
        }

        [Test]
        public void HasReachedLimit_NumberOfRecordsBelowLimit_ReturnsFalse()
        {
            //Arrange
            var records = new[]
            {
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-23)),
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-21))
            };

            var config = new VerificationLimitConfig
            {
                MaxLimitDuration = TimeSpan.FromHours(24),
                MaxVerificationsAllowed = 3
            };

            var automocker = new AutoMocker();

            automocker.SetupOptions(config);

            var target = automocker.CreateInstance<VerificationLimit>();

            //Act
            var result = target.HasReachedLimit(records);

            //Assert
            result.Should().BeFalse();
        }

        [Test]
        public void HasReachedLimit_NumberOfRecordsAtLimit_ReturnsTrue()
        {
            //Arrange
            var records = new[]
            {
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-23)),
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-21)),
                new VerificationRecord("pseudo-1", DateTime.UtcNow.AddHours(-19))
            };

            var config = new VerificationLimitConfig
            {
                MaxLimitDuration = TimeSpan.FromHours(24),
                MaxVerificationsAllowed = 3
            };

            var automocker = new AutoMocker();

            automocker.SetupOptions(config);

            var target = automocker.CreateInstance<VerificationLimit>();

            //Act
            var result = target.HasReachedLimit(records);

            //Assert
            result.Should().BeTrue();
        }
    }
}
