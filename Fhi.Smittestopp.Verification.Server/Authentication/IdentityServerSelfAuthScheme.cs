using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Optional;
using Optional.Async.Extensions;

namespace Fhi.Smittestopp.Verification.Server.Authentication
{
    public class IdentityServerSelfAuthScheme
    {
        public const string Scheme = OidcConstants.AuthenticationSchemes.AuthorizationHeaderBearer;
        public const string AuthorizationHeader = "Authorization";

        public class AuthenticationHandler : AuthenticationHandler<ApiKeyOptions>
        {
            private readonly ITokenValidator _tokenValidator;

            public AuthenticationHandler(IOptionsMonitor<ApiKeyOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ITokenValidator tokenValidator) : base(options, logger, encoder, clock)
            {
                _tokenValidator = tokenValidator;
            }

            protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var tokenAuthResult = await ExtractBearerToken()
                    .MapAsync(CreateResultForBearerToken);

                return tokenAuthResult.ValueOr(AuthenticateResult.NoResult);
            }

            private async Task<AuthenticateResult> CreateResultForBearerToken(string token)
            {
                var validationResult = await _tokenValidator.ValidateAccessTokenAsync(token);

                if (validationResult.IsError)
                {
                    return AuthenticateResult.Fail("Invalid token provided");
                }

                return CreateSuccessResult(validationResult);
            }

            private AuthenticateResult CreateSuccessResult(TokenValidationResult tokenValidationResult)
            {
                var identity = new ClaimsIdentity(tokenValidationResult.Claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            private Option<string> ExtractBearerToken()
            {
                return ExtractAuthHeaderValue()
                    .FlatMap(headerValue => headerValue.Scheme == OidcConstants.AuthenticationSchemes.AuthorizationHeaderBearer
                        ? headerValue.Parameter.Some()
                        : Option.None<string>());
            }

            private Option<AuthenticationHeaderValue> ExtractAuthHeaderValue()
            {
                var authHeader = Request.Headers.ContainsKey(AuthorizationHeader)
                    ? Option.Some(Request.Headers[AuthorizationHeader])
                    : Option.None<StringValues>();
                return authHeader.FlatMap(header => AuthenticationHeaderValue.TryParse(header, out var headerValue)
                    ? headerValue.Some()
                    : Option.None<AuthenticationHeaderValue>());
            }
        }

        public class ApiKeyOptions : AuthenticationSchemeOptions
        {
        }
    }
}
