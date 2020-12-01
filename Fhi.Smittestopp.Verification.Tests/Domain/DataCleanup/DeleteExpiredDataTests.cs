using System;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.DataCleanup;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Domain.DataCleanup
{
    [TestFixture]
    public class DeleteExpiredDataTests
    {
        [Test]
        public async Task Handle_DeletesWithCutoffFromLimitConfig()
        {
            var verLimitDuration = TimeSpan.FromHours(new Random().Next(12, 48));

            var limitConfigMock = new Mock<IVerificationLimitConfig>();
            limitConfigMock.Setup(x => x.MaxLimitDuration).Returns(verLimitDuration);

            var automocker = new AutoMocker();

            automocker.Setup<IVerificationLimit, IVerificationLimitConfig>(x => x.Config)
                .Returns(limitConfigMock.Object);

            var target = automocker.CreateInstance<DeleteExpiredData.Handler>();

            var testStart = DateTime.Now;
            await target.Handle(new DeleteExpiredData.Command(), new CancellationToken());
            var testEnd = DateTime.Now;

            automocker.Verify<IVerificationRecordsRepository>(x => 
                x.DeleteExpiredRecords(It.IsInRange(
                    testStart - verLimitDuration,
                    testEnd - verLimitDuration,
                    Moq.Range.Inclusive)));
        }
    }
}
