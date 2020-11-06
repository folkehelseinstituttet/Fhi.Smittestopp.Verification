using Fhi.Smittestopp.Verification.Domain.Factories;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Factories
{
    [TestFixture]
    public class OneWayPseudonymFactoryTests
    {
        [Test]
        public void Create_GivenAnInputValie_ReturnsNonEqualOutput()
        {
            var optionsMock = new Mock<IOptions<OneWayPseudonymFactory.Config>>();
            optionsMock.Setup(x => x.Value).Returns(new OneWayPseudonymFactory.Config
            {
                Key = "b3ZlcnJpZGUtdGhpcy1pbi1wcm9k"
            });

            var target = new OneWayPseudonymFactory(optionsMock.Object);

            var result = target.Create("some-value");

            result.Should().NotBe("some-value");
        }

        [Test]
        public void Create_GivenTwoDifferentOriginals_ReturnsDifferentOutputs()
        {
            var optionsMock = new Mock<IOptions<OneWayPseudonymFactory.Config>>();
            optionsMock.Setup(x => x.Value).Returns(new OneWayPseudonymFactory.Config
            {
                Key = "b3ZlcnJpZGUtdGhpcy1pbi1wcm9k"
            });

            var target = new OneWayPseudonymFactory(optionsMock.Object);

            var resultA = target.Create("some-value");
            var resultB = target.Create("some-other-value");

            resultA.Should().NotBe(resultB);
        }

        [Test]
        public void Create_GivenDuplicateInputs_ReturnsIdenticalOutputs()
        {
            var optionsMock = new Mock<IOptions<OneWayPseudonymFactory.Config>>();
            optionsMock.Setup(x => x.Value).Returns(new OneWayPseudonymFactory.Config
            {
                Key = "b3ZlcnJpZGUtdGhpcy1pbi1wcm9k"
            });

            var target = new OneWayPseudonymFactory(optionsMock.Object);

            var resultA = target.Create("some-value");
            var resultB = target.Create("some-value");

            resultA.Should().Be(resultB);
        }

        [Test]
        public void Config_SetGetKeyStringValue_ReturnsSameResult()
        {
            var target = new OneWayPseudonymFactory.Config
            {
                Key = "b3ZlcnJpZGUtdGhpcy1pbi1wcm9k"
            };

            target.Key.Should().Be("b3ZlcnJpZGUtdGhpcy1pbi1wcm9k");
        }
    }
}
