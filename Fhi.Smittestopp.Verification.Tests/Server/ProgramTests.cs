using System.Collections.Generic;
using Fhi.Smittestopp.Verification.Server;
using Fhi.Smittestopp.Verification.Server.BackgroundServices;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Server
{
    public class ProgramTests
    {
        [Test]
        public void CreateHostBuilder_ExecutesWithoutError()
        {
            var hostBuilder = Program.CreateHostBuilder(new string[0]);

            hostBuilder.Should().NotBeNull();
        }

        [Test]
        public void AddEnabledBackgroundServices_GivenCleanupNotEnabled_DoesNotAddCleanupServices()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                {"cleanupTask:enabled", "False"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddEnabledBackgroundServices(configuration);

            using (new AssertionScope())
            {
                serviceCollection.Should().NotContain(x => x.ImplementationType == typeof(DeleteExpiredDataBackgroundService));
            }
        }

        [Test]
        public void AddEnabledBackgroundServices_GivenCleanupEnabled_AddsCleanupServices()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                {"cleanupTask:enabled", "True"},
                {"cleanupTask:runInterval", "2:00:00.0"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddEnabledBackgroundServices(configuration);

            using (new AssertionScope())
            {
                serviceCollection.Should().Contain(x => x.ImplementationType == typeof(DeleteExpiredDataBackgroundService));
            }
        }
    }
}
