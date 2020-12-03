using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Moq.AutoMock;
using Moq.Protected;
using AuthenticationOptions = IdentityServer4.Configuration.AuthenticationOptions;

namespace Fhi.Smittestopp.Verification.Tests.TestUtils
{
    public static class TestExtensions
    {
        public static T AddTestControllerContext<T>(this T controller, AutoMocker automock = null) where T : Controller
        {
            var isOptions = new IdentityServerOptions
            {
                Authentication = new AuthenticationOptions
                {
                    CookieAuthenticationScheme = "cookie"
                }
            };
            var authService = new Mock<IAuthenticationService>();
            var tempDataDictionaryFactory = new Mock<ITempDataDictionaryFactory>();
            var requestServices = new Mock<IServiceProvider>();
            var urlHelper = new Mock<IUrlHelper>();
            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            urlHelperFactory.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>())).Returns(urlHelper.Object);

            requestServices.Setup(x => x.GetService(typeof(IdentityServerOptions))).Returns(isOptions);
            requestServices.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(automock?.Get<IAuthenticationService>() ?? authService.Object);
            requestServices.Setup(x => x.GetService(typeof(ITempDataDictionaryFactory))).Returns(tempDataDictionaryFactory.Object);
            requestServices.Setup(x => x.GetService(typeof(IUrlHelperFactory))).Returns(() => automock?.Get<IUrlHelperFactory>() ?? urlHelperFactory.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = requestServices.Object
                }
            };

            return controller;
        }

        public static Mock<HttpMessageHandler> Setup404Default(this Mock<HttpMessageHandler> mock)
        {
            mock
                .Protected()
                // Setup the PROTECTED method to mock
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                // prepare the expected response of the mocked http call
                .Returns<HttpRequestMessage, CancellationToken>((request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
            return mock;
        }

        public static Mock<HttpMessageHandler> SetupRequest(this Mock<HttpMessageHandler> mock, HttpMethod method, string path, HttpResponseMessage response)
        {
            mock
                .Protected()
                // Setup the PROTECTED method to mock
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == method && r.RequestUri.PathAndQuery == path),
                    ItExpr.IsAny<CancellationToken>()
                )
                // prepare the expected response of the mocked http call
                .ReturnsAsync(response)
                .Verifiable();
            return mock;
        }
    }
}
