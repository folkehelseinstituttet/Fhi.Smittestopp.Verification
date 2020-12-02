using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
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
        public static T EnsureTestContextAdded<T>(this T controller) where T : ControllerBase
        {
            if (controller.ControllerContext == null)
            {
                controller.ControllerContext = new ControllerContext();
            }

            if (controller.ControllerContext.HttpContext == null)
            {
                controller.ControllerContext.HttpContext = new DefaultHttpContext();
            }
            return controller;
        }

        public static T AddTestControllerContext<T>(this T controller, AutoMocker automock = null) where T : ControllerBase
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
            requestServices.Setup(x => x.GetService(typeof(IdentityServerOptions))).Returns(isOptions);
            requestServices.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(authService.Object);
            requestServices.Setup(x => x.GetService(typeof(ITempDataDictionaryFactory))).Returns(tempDataDictionaryFactory.Object);
            requestServices.Setup(x => x.GetService(typeof(IUrlHelperFactory))).Returns(() => automock?.Get<IUrlHelperFactory>());

            controller.EnsureTestContextAdded();
            controller.ControllerContext.HttpContext.RequestServices = requestServices.Object;

            return controller;
        }

        public static T SetUserForContext<T>(this T controller, ClaimsPrincipal principal) where T : ControllerBase
        {
            controller.EnsureTestContextAdded();
            controller.ControllerContext.HttpContext.User = principal;
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
