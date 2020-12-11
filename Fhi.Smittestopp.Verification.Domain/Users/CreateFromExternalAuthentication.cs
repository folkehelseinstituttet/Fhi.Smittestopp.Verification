using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Constants;
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
        public class Command : IRequest<IdentifiedUser>
        {
            public ICollection<Claim> ExternalClaims { get; set; }
            public string Provider { get; set; }

            public Command(string provider, IEnumerable<Claim> claims)
            {
                Provider = provider;
                ExternalClaims = claims.ToList();
            }
        }

        public class Handler : IRequestHandler<Command, IdentifiedUser>
        {
            private readonly IPseudonymFactory _pseudonymFactory;

            public Handler(IPseudonymFactory pseudonymFactory)
            {
                _pseudonymFactory = pseudonymFactory;
            }

            public Task<IdentifiedUser> Handle(Command request, CancellationToken cancellationToken)
            {
                var nationalIdentifierClaim = FindNationalIdentifierClaim(request.Provider, request.ExternalClaims).ValueOr(() => 
                    throw new Exception("Unable to locate national identifier for external user from provider: " + request.Provider));

                var userIdClaim = FindUserIdClaim(request.ExternalClaims).ValueOr(() =>
                    throw new Exception("Unable to determine user-ID from external claims from provider: " + request.Provider));

                var pseudonym = _pseudonymFactory.Create(request.Provider + ":" + userIdClaim.Value);

                return Task.FromResult(new IdentifiedUser(nationalIdentifierClaim.Value, pseudonym));
            }

            private Option<Claim> FindUserIdClaim(ICollection<Claim> claims)
            {
                return claims.FirstOrNone(c => c.Type == JwtClaimTypes.Subject)
                    .Else(() => claims.FirstOrNone(c => c.Type == ClaimTypes.NameIdentifier));
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
