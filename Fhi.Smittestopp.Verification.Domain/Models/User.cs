using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public abstract class User
    {
        public Guid Id { get; }
        public string DisplayName => "Temporary user";

        public abstract string Pseudonym { get; }
        public abstract Option<string> NationalIdentifier { get; }
        public abstract bool IsPinVerified { get; }

        protected User()
        {
            Id = Guid.NewGuid();
        }

        public IEnumerable<Claim> GetCustomClaims()
        {
            var claims = new List<Claim>
            {
                new Claim(InternalClaims.Pseudonym, Pseudonym)
            };

            NationalIdentifier.MatchSome(x => claims.Add(new Claim(InternalClaims.NationalIdentifier, x)));

            if (IsPinVerified)
            {
                claims.Add(new Claim(InternalClaims.PinVerified, "true"));
            }

            return claims;
        }
    }
}
