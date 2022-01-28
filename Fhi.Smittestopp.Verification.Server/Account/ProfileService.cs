using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Domain.Utilities.NationalIdentifiers;
using Fhi.Smittestopp.Verification.Domain.Verifications;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
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
        private readonly AnonymousTokensConfig _anonymousTokensConfig;
        private readonly IVerificationLimitConfig _verificationLimitConfig;

        public ProfileService(IMediator mediator, ILogger<ProfileService> logger, IOptions<AnonymousTokensConfig> anonymousTokensConfig, IOptions<VerificationLimitConfig> verificationLimitConfig)
        {
            _mediator = mediator;
            _logger = logger;
            _verificationLimitConfig = verificationLimitConfig.Value;
            _anonymousTokensConfig = anonymousTokensConfig.Value;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            _logger.LogDebug("Retrieving claims for request: {requestedClaims}", context.RequestedClaimTypes);
            context.AddRequestedClaims(await GetCustomClaims(context.Subject, context.RequestedClaimTypes, context.RequestedResources?.ParsedScopes ?? new List<ParsedScopeValue>()));
            _logger.LogDebug("Issued claims: {issuedClaims}", context.IssuedClaims.Select(c => c.Type).ToList());
        }

      

        public Task IsActiveAsync(IsActiveContext context)
        {
            context.IsActive = true;

            return Task.CompletedTask;
        }

        public async Task<IEnumerable<Claim>> GetCustomClaims(ClaimsPrincipal subject, IEnumerable<string> requestedClaims, IEnumerable<ParsedScopeValue> parsedScopes)
        {
            var nationalIdentifier = subject?.Claims?.FirstOrDefault(c => c.Type == InternalClaims.NationalIdentifier)?.Value;
            if (!nationalIdentifier.CanDetermineAge() || nationalIdentifier.IsPersonYoungerThanAgeLimit(_verificationLimitConfig.MinimumAgeInYears))
            {
                // Return only blocking claims: Too young or can't determine age
                return GetClaimsForBlockedPerson();
            }

            try
            {
                bool skipMsisLookup = parsedScopes?.Select(p => p.ParsedName.ToLower()).Contains(VerificationScopes.SkipMsisLookup) == true;
                var originalClaims = subject.Claims.ToList();

                var pseudonym = originalClaims
                    .FirstOrNone(x => x.Type == InternalClaims.Pseudonym)
                    .Map(x => x.Value)
                    .ValueOr(subject.Identity.Name);

                var nationalIdentifierClaim = originalClaims.FirstOrNone(x => x.Type == InternalClaims.NationalIdentifier);

                var customClaims = new List<Claim>();
                if (VerificationResult.RequestedClaimsRequiresVerification(requestedClaims))
                {
                    var verificationResult = await nationalIdentifierClaim.MatchAsync(
                        none: async () =>
                        {
                            var isPinVerified = originalClaims
                                .FirstOrNone(x => x.Type == InternalClaims.PinVerified)
                                .Map(x => x.Value == "true")
                                .ValueOr(false);
                            return await _mediator.Send(new VerifyPinUser.Command(pseudonym, isPinVerified, skipMsisLookup));
                        },
                        some: natIdent => _mediator.Send(new VerifyIdentifiedUser.Command(natIdent.Value, pseudonym, skipMsisLookup)));

                    customClaims.AddRange(verificationResult.GetVerificationClaims());
                }

                if (_anonymousTokensConfig.Enabled)
                {
                    customClaims.AddRange(_anonymousTokensConfig.EnabledClientFlags.Select(clientFlag =>
                        new Claim(VerificationClaims.AnonymousToken, clientFlag)));
                }
                return customClaims;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error encountered when attempting to verify user infection status");
                return new []{new Claim(DkSmittestopClaims.Covid19Status, DkSmittestopClaims.StatusValues.Unknwon)};
            }
        }
        private IEnumerable<Claim> GetClaimsForBlockedPerson()
        {
            var customClaims = new List<Claim>();
            customClaims.AddRange(new[]
            {
                new Claim(DkSmittestopClaims.Covid19Blocked, "true"),
                new Claim(DkSmittestopClaims.Covid19LimitCount, _verificationLimitConfig.MaxVerificationsAllowed.ToString()),
                new Claim(DkSmittestopClaims.Covid19LimitDuration, _verificationLimitConfig.MaxLimitDuration.TotalHours.ToString()),
            });
            return customClaims;
        }
    }
}
