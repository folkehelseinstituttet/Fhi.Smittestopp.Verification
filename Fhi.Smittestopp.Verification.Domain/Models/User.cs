using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fhi.Smittestopp.Verification.Domain.Constans;
using IdentityModel;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public abstract class User
    {
        public string Id { get; }
        public string DisplayName => "Temporary user";
        public Option<string> ExternalProviderUserId { get; }
        public Option<string> ExternalProvider { get; }

        public abstract bool HasVerifiedPostiveTest { get; }
        public abstract Option<DateTime> PositiveTestDate { get; }

        protected User(string id)
        {
            Id = id;
        }

        protected User(string provider, string providerUserId) : this(provider + ":" + providerUserId)
        {
            ExternalProvider = provider.Some();
            ExternalProviderUserId = providerUserId.Some();
        }

        public IEnumerable<Claim> GetCustomClaims()
        {
            var claims = new List<Claim>();

            if (HasVerifiedPostiveTest)
            {
                claims.Add(new Claim(JwtClaimTypes.Role, VerificationRoles.VerifiedPositive));
            }

            PositiveTestDate.MatchSome(testData => claims.Add(new Claim(VerificationClaims.VerifiedPositiveTestDate, testData.ToString("yyyy-MM-dd"))));

            return claims;
        }
    }
}
