using System;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.DataCleanup;
using Fhi.Smittestopp.Verification.Server.BackgroundServices;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Server.BackgroundService
{
    public class DeleteExpiredDataBackgroundServiceTests
    {
        [Test]
        public async Task StartAsync_SendsDeleteExpiredDataCommand()
        {
            //Arrange
            var cancellationToken = new CancellationToken();

            var mediaterMock = new Mock<IMediator>();
            mediaterMock.Setup(x => x.Send(It.IsAny<DeleteExpiredData.Command>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);
            var services = new ServiceCollection();
            services.AddTransient(x => mediaterMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var automocker = new AutoMocker();
            automocker
                .Use<IServiceProvider>(serviceProvider);
            automocker
                .Setup<IOptions<DeleteExpiredDataBackgroundService.Config>, DeleteExpiredDataBackgroundService.Config>(x => x.Value)
                .Returns(new DeleteExpiredDataBackgroundService.Config
                {
                    Enabled = true,
                    RunInterval = TimeSpan.FromHours(2)
                });

            var target = automocker.CreateInstance<DeleteExpiredDataBackgroundService>();

            //Act
            await target.StartAsync(cancellationToken);
            Thread.Sleep(1000);
            await target.StopAsync(cancellationToken);

            //Assert
            mediaterMock.Verify(x => x.Send(It.IsAny<DeleteExpiredData.Command>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        public async Task StartAsync_GivenErrorInCommandHandling_HandlesErrorInternally()
        {
            //Arrange
            var cancellationToken = new CancellationToken();

            var mediaterMock = new Mock<IMediator>();
            mediaterMock.Setup(x => x.Send(It.IsAny<DeleteExpiredData.Command>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("test"));
            var services = new ServiceCollection();
            services.AddTransient(x => mediaterMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            var automocker = new AutoMocker();
            automocker
                .Use<IServiceProvider>(serviceProvider);
            automocker
                .Setup<IOptions<DeleteExpiredDataBackgroundService.Config>, DeleteExpiredDataBackgroundService.Config>(x => x.Value)
                .Returns(new DeleteExpiredDataBackgroundService.Config
                {
                    Enabled = true,
                    RunInterval = TimeSpan.FromHours(2)
                });

            var target = automocker.CreateInstance<DeleteExpiredDataBackgroundService>();

            //Act
            await target.StartAsync(cancellationToken);
            Thread.Sleep(1000);
            await target.StopAsync(cancellationToken);

            //Assert
            mediaterMock.Verify(x => x.Send(It.IsAny<DeleteExpiredData.Command>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
