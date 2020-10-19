using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using IdentityModel;
using MediatR;
using Microsoft.Extensions.Logging;
using Optional;
using Optional.Async.Extensions;
using Optional.Collections;

namespace Fhi.Smittestopp.Verification.Domain.Users
{
    public class CreateFromExternalAuthentication
    {
        public class Command : IRequest<User>
        {
            public ICollection<Claim> ExternalClaims { get; set; }
            public string Provider { get; set; }
        }

        public class Handler : IRequestHandler<Command, User>
        {
            private readonly IMsisLookupService _msisLookupService;
            private readonly ILogger<CreateFromExternalAuthentication> _logger;

            public Handler(IMsisLookupService msisLookupService, ILogger<CreateFromExternalAuthentication> logger)
            {
                _msisLookupService = msisLookupService;
                _logger = logger;
            }

            public async Task<User> Handle(Command request, CancellationToken cancellationToken)
            {
                var userIdClaim = FindUserIdClaim(request.ExternalClaims).ValueOr(() =>
                    throw new Exception("Unable to determine user-ID from external claims from provider: " + request.Provider));

                var positiveTest = await FindTestresultForExternalUser(request.Provider, request.ExternalClaims);

                return positiveTest.Match<User>(
                    none: () => new NonPositiveUser(),
                    some: pt => new PositiveUser(request.Provider, userIdClaim.Value, pt));
            }

            private Option<Claim> FindUserIdClaim(ICollection<Claim> claims)
            {
                return claims.FirstOrNone(c => c.Type == JwtClaimTypes.Subject)
                    .Else(() => claims.FirstOrNone(c => c.Type == ClaimTypes.NameIdentifier));
            }

            private Task<Option<PositiveTestResult>> FindTestresultForExternalUser(string provider, ICollection<Claim> claims)
            {
                var nationalIdentifierClaim = FindNationalIdentifierClaim(provider, claims);
                nationalIdentifierClaim.MatchNone(() => _logger.LogWarning("Unable to locate national identifier for external user from provider: " + provider));
                return nationalIdentifierClaim.FlatMapAsync(natIdClaim => _msisLookupService.FindPositiveTestResult(natIdClaim.Value));
            }

            private Option<Claim> FindNationalIdentifierClaim(string provider, ICollection<Claim> claims)
            {
                switch (provider)
                {
                    case ExternalProviders.IdPorten:
                        return claims.FirstOrNone(c => c.Type == IdPortenClaims.NationalIdentifier);
                    default:
                        return Option.None<Claim>();
                }
            }
        }
    }
}
