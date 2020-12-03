using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Server;
using Fhi.Smittestopp.Verification.Server.Home;
using FluentAssertions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Server.Home
{
    [TestFixture]
    public class HomeControllerTests
    {
        [Test]
        public void Index_WhenHomePageDisabled_ReturnsNotFound()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    EnableHomePage = false
                });

            var target = automocker.CreateInstance<HomeController>();

            var result = target.Index();

            result.Should().BeOfType<NotFoundResult>();
        }


        [Test]
        public void Index_WhenHomePageEnabled_ReturnsViewResult()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    EnableHomePage = true
                });

            var target = automocker.CreateInstance<HomeController>();

            var result = target.Index();

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().BeNull();
        }

        [Test]
        public async Task Error_WhenErrorDescriptionDisabled_ViewResultWithBasicErrorInfo()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IIdentityServerInteractionService, Task<ErrorMessage>>(x => x.GetErrorContextAsync("error-1"))
                .ReturnsAsync(new ErrorMessage
                {
                    Error = "Some error",
                    ErrorDescription = "This is just a sample error which should not be displayed",
                    RequestId = "request-1"
                });


            automocker.Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    DisplayErrorDescription = false
                });

            var target = automocker.CreateInstance<HomeController>();

            var result = await target.Error("error-1");

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().Be("Error");
            vResult.Model.Should().BeOfType<ErrorViewModel>();
            var vm = (ErrorViewModel)vResult.Model;
            vm.Error.Should().Be("Some error");
            vm.RequestId.Should().Be("request-1");
            vm.ErrorDescription.Should().BeNull();
        }

        [Test]
        public async Task Error_WhenErrorDescriptionEnabled_IncludesErrorMessage()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IIdentityServerInteractionService, Task<ErrorMessage>>(x => x.GetErrorContextAsync("error-1"))
                .ReturnsAsync(new ErrorMessage
                {
                    Error = "Some error",
                    ErrorDescription = "This is just a sample error which should be displayed",
                    RequestId = "request-1"
                });

            automocker.Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    DisplayErrorDescription = true
                });

            var target = automocker.CreateInstance<HomeController>();

            var result = await target.Error("error-1");

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().Be("Error");
            vResult.Model.Should().BeOfType<ErrorViewModel>();
            var vm = (ErrorViewModel)vResult.Model;
            vm.ErrorDescription.Should().Be("This is just a sample error which should be displayed");
        }

        [Test]
        public async Task Error_NoErrorContext_ReturnsViewResultWithoutError()
        {
            var automocker = new AutoMocker();

            automocker.Setup<IOptions<InteractionConfig>, InteractionConfig>(x => x.Value)
                .Returns(new InteractionConfig
                {
                    DisplayErrorDescription = true
                });

            var target = automocker.CreateInstance<HomeController>();

            var result = await target.Error("error-1");

            result.Should().BeOfType<ViewResult>();
            var vResult = (ViewResult)result;
            vResult.ViewName.Should().Be("Error");
            vResult.Model.Should().BeOfType<ErrorViewModel>();
            var vm = (ErrorViewModel)vResult.Model;
            vm.Error.Should().BeNull();
        }
    }
}
