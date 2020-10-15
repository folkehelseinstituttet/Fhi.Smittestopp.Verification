using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fhi.Smittestopp.Verification.Domain.Constans;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class User
    {
        public string Id { get; set; }
        public string DisplayName => "Temporary user";
        public bool HasVerifiedPostiveTest { get; set; }
        public Option<DateTime> PositiveTestDate { get; set; }
        public string ExternalProviderUserId { get; set; }
        public string ExternalProvider { get; set; }

        public User(string provider, string providerUserId)
        {
            Id = provider + ":" + providerUserId;
            ExternalProvider = provider;
            ExternalProviderUserId = providerUserId;
        }

        public User(string provider, string providerUserId, PositiveTestResult testresult) : this(provider, providerUserId)
        {
            HasVerifiedPostiveTest = true;
            PositiveTestDate = testresult.PositiveTestDate;
        }

        public IEnumerable<Claim> GetCustomClaims()
        {
            var claims = new List<Claim>
            {
                new Claim(VerificationClaims.VerifiedPositive, HasVerifiedPostiveTest.ToString().ToLowerInvariant())
            };

            PositiveTestDate.MatchSome(testData => claims.Add(new Claim(VerificationClaims.VerifiedPositiveTestDate, testData.ToString("yyyy-MM-dd"))));
            return claims;
        }
    }
}
