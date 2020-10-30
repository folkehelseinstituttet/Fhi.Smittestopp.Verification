using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Users;
using FluentAssertions;
using Moq.AutoMock;
using NUnit.Framework;
using Optional;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Users
{
    [TestFixture]
    public class CreateFromPinCodeTests
    {
        /// <summary>
        /// Currently no PIN-storage exists, so all PINs are invalid..
        /// </summary>
        [Test]
        public async Task Handle_GivenInvalidPin_ReturnsNone()
        {
            var automocker = new AutoMocker();

            var target = automocker.CreateInstance<CreateFromPinCode.Handler>();

            var result = await target.Handle(new CreateFromPinCode.Command("invalid-pin"), new CancellationToken());

            result.Should().Be(Option.None<User>());
        }
    }
}
