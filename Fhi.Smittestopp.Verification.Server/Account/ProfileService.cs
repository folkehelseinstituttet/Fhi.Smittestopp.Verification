using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Users;
using Fhi.Smittestopp.Verification.Domain.Verifications;
using IdentityServer4.Models;
using IdentityServer4.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optional.Async.Extensions;
using Optional.Collections;

namespace Fhi.Smittestopp.Verification.Server.Account
{
    public class ProfileService : IProfileService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProfileService> _logger;
        private readonly AnonymousTokensConfig _anonymousTokensConfig
            ;

        public ProfileService(IMediator mediator, ILogger<ProfileService> logger, IOptions<AnonymousTokensConfig> anonymousTokensConfig)
        {
            _mediator = mediator;
            _logger = logger;
            _anonymousTokensConfig = anonymousTokensConfig.Value;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            _logger.LogInformation("Retrieving claims for request: {requestedClaims}", context.RequestedClaimTypes);
            context.AddRequestedClaims(await GetCustomClaims(context.Subject, context.RequestedClaimTypes));
            _logger.LogInformation("Issued claims: {issuedClaims}", context.IssuedClaims.Select(c => c.Type).ToList());
        }

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Claim>> GetCustomClaims(ClaimsPrincipal subject, IEnumerable<string> requestedClaims)
        {
            var originalClaims = subject.Claims.ToList();

            var pseudonym = originalClaims
                .FirstOrNone(x => x.Type == InternalClaims.Pseudonym)
                .Map(x => x.Value)
                .ValueOr(subject.Identity.Name);

            var nationalIdentifierClaim = originalClaims.FirstOrNone(x => x.Type == InternalClaims.NationalIdentifier);

            var verificationClaims = new List<Claim>();
            if (VerificationResult.RequestedClaimsRequiresVerification(requestedClaims))
            {
                try
                {
                    var verificationResult = await nationalIdentifierClaim.MatchAsync(
                        none: async () =>
                        {
                            var isPinVerified = originalClaims
                                .FirstOrNone(x => x.Type == InternalClaims.PinVerified)
                                .Map(x => x.Value == "true")
                                .ValueOr(false);

                            return isPinVerified
                                ? await _mediator.Send(new VerifyPinUser.Command(pseudonym))
                                : new VerificationResult();

                        },
                        some: natIdent => _mediator.Send(new VerifyIdentifiedUser.Command(natIdent.Value, pseudonym)));

                    verificationClaims.AddRange(verificationResult.GetVerificationClaims(_anonymousTokensConfig.Enabled));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error encountered when attempting to verify user infection status");
                    verificationClaims.Add(new Claim(DkSmittestopClaims.Covid19Status, DkSmittestopClaims.StatusValues.Unknwon));
                }
            }
;
            return verificationClaims;
        }
    }
}
