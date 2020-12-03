using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Server.Account.Models;
using Fhi.Smittestopp.Verification.Server.ExternalController.Models;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optional;

namespace Fhi.Smittestopp.Verification.Server.ExternalController
{
    public interface IExternalService
    {
        Option<ExtChallengeResult, string> CreateChallenge(ExtChallengeRequest request);
        Task<Option<ExtAuthenticationResult, string>> ProcessExternalAuthentication(AuthenticateResult extAuthResult);
    }

    public class ExternalService : IExternalService
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ILogger<ExternalService> _logger;
        private readonly IMediator _mediator;
        private readonly IEventService _events;
        private readonly IOptions<InteractionConfig> _interactionConfig;

        public ExternalService(IUrlHelperFactory urlHelperFactory, IActionContextAccessor actionContextAccessor, IIdentityServerInteractionService interaction, ILogger<ExternalService> logger, IMediator mediator, IEventService events, IOptions<InteractionConfig> interactionConfig)
        {
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
            _interaction = interaction;
            _logger = logger;
            _mediator = mediator;
            _events = events;
            _interactionConfig = interactionConfig;
        }

        public Option<ExtChallengeResult, string> CreateChallenge(ExtChallengeRequest request)
        {
            var returnUrl = request.ReturnUrl.ValueOr("~/");

            // validate returnUrl - Must be either back to a local page or be a valid OIDC URL
            var validReturnUrl =
                _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext).IsLocalUrl(returnUrl) ||
                _interaction.IsValidReturnUrl(returnUrl);
            if (!validReturnUrl)
            {
                _logger.LogWarning("Potential malicious link for external challenge detected: " + request.ReturnUrl);
                return Option.None<ExtChallengeResult, string>("invalid return URL");
            }

            return new ExtChallengeResult
            {
                Scheme = request.Scheme,
                TrustedReturnUrl = returnUrl
            }.Some<ExtChallengeResult, string>();
        }

        public async Task<Option<ExtAuthenticationResult, string>> ProcessExternalAuthentication(AuthenticateResult extAuthResult)
        {
            if (extAuthResult?.Succeeded != true)
            {
                return Option.None<ExtAuthenticationResult, string>("External authentication error");
            }

            _logger.LogDebug("Processing external login callback");

            // retrieve return URL
            var returnUrl = extAuthResult.Properties.Items["returnUrl"] ?? "~/";
            // check if external login is in the context of an OIDC request
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

            if (_interactionConfig.Value.RequireAuthorizationRequest && context == null)
            {
                return Option.None<ExtAuthenticationResult, string>("A valid authorization request is required for login.");
            }

            var provider = extAuthResult.Properties.Items["scheme"];
            var providerClaims = extAuthResult.Principal.Claims.ToList();

            // We create a new temporary local user for each sign in
            var internalUser = await _mediator.Send(new CreateFromExternalAuthentication.Command(provider, providerClaims));

            // this allows us to collect any additional claims or properties
            // for the specific protocols used and store them in the local auth cookie.
            // this is typically used to store data needed for signout from those protocols.
            var additionalLocalClaims = new List<Claim>();

            // if the external system sent a session id claim, copy it over
            // so we can use it for single sign-out
            var sid = extAuthResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                additionalLocalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // issue authentication cookie for user
            var isuser = new IdentityServerUser(internalUser.Id.ToString())
            {
                DisplayName = internalUser.DisplayName,
                IdentityProvider = provider,
                AdditionalClaims = internalUser.GetCustomClaims().Concat(additionalLocalClaims).ToList()
            };

            var useNativeRedirect = context?.IsNativeClient() ?? false;

            var externalIdToken = extAuthResult.Properties.GetTokenValue("id_token").SomeNotNull();

            await _events.RaiseAsync(new UserLoginSuccessEvent(isuser.IdentityProvider, provider, isuser.SubjectId, isuser.DisplayName, true, context?.Client.ClientId));

            return new ExtAuthenticationResult
            {
                IsUser = isuser,
                UseNativeClientRedirect = useNativeRedirect,
                ExternalIdToken = externalIdToken,
                ReturnUrl = returnUrl
            }.Some<ExtAuthenticationResult, string>();
        }
    }
}
