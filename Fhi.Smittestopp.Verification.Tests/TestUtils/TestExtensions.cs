using System;
using IdentityServer4.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Moq.AutoMock;
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
            requestServices.Setup(x => x.GetService(typeof(IdentityServerOptions))).Returns(isOptions);
            requestServices.Setup(x => x.GetService(typeof(IAuthenticationService))).Returns(authService.Object);
            requestServices.Setup(x => x.GetService(typeof(ITempDataDictionaryFactory))).Returns(tempDataDictionaryFactory.Object);
            requestServices.Setup(x => x.GetService(typeof(IUrlHelperFactory))).Returns(() => automock?.Get<IUrlHelperFactory>());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = requestServices.Object
                }
            };

            return controller;
        }
    }
}
