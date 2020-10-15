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
using Optional;
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

            public Handler(IMsisLookupService msisLookupService)
            {
                _msisLookupService = msisLookupService;
            }

            public async Task<User> Handle(Command request, CancellationToken cancellationToken)
            {
                var userIdClaim = FindUserIdClaim(request.Provider, request.ExternalClaims).ValueOr(() =>
                    throw new Exception("Unable to determine user-ID from external claims from provider: " + request.Provider));

                var nationalIdentifierClaim = FindNationalIdentifierClaim(request.Provider, request.ExternalClaims).ValueOr(() =>
                    throw new Exception("Unable to determine national identifier from external claims from provider: " + request.Provider));

                var positiveTest = await _msisLookupService.FindPositiveTestResult(nationalIdentifierClaim.Value);

                return positiveTest.Match(
                    none: () => new User(request.Provider, userIdClaim.Value),
                    some: t => new User(request.Provider, userIdClaim.Value, t));
            }

            private Option<Claim> FindUserIdClaim(string provider, ICollection<Claim> claims)
            {
                switch (provider)
                {
                    case ExternalProviders.IdPorten:
                        return claims.FirstOrNone(c => c.Type == IdPortenClaims.SubjectIdentifier);
                    default:
                        // try to determine the unique id of the external user (issued by the provider)
                        // the most common claim type for that are the sub claim and the NameIdentifier
                        // depending on the external provider, some other claim type might be used
                        return claims.FirstOrNone(c => c.Type == JwtClaimTypes.Subject)
                            .Else(() => claims.FirstOrNone(c => c.Type == ClaimTypes.NameIdentifier));
                }
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
