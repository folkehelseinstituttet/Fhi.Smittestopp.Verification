using System;
using System.Collections.Generic;
using System.Security.Claims;
using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using IdentityModel;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public abstract class User
    {
        public Guid Id { get; }
        public string DisplayName => "Temporary user";
        public Option<string> ExternalProviderUserId { get; }
        public Option<string> ExternalProvider { get; }

        public abstract bool HasVerifiedPostiveTest { get; }
        public abstract bool VerificationLimitExceeded { get; }
        public abstract Option<IVerificationLimitConfig> VerificationLimitConfig { get; }
        public abstract Option<DateTime> PositiveTestDate { get; }

        protected User()
        {
            Id = Guid.NewGuid();
        }

        protected User(string provider, string providerUserId) : this()
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

                claims.Add(new Claim(DkSmittestopClaims.Covid19Status, DkSmittestopClaims.StatusValues.Positive));
                claims.Add(new Claim(DkSmittestopClaims.Covid19Blocked, VerificationLimitExceeded.ToString().ToLowerInvariant()));
            }
            else
            {
                claims.Add(new Claim(DkSmittestopClaims.Covid19Status, DkSmittestopClaims.StatusValues.Negative));
            }

            if (VerificationLimitExceeded)
            {
                claims.Add(new Claim(DkSmittestopClaims.Covid19Blocked, VerificationLimitExceeded.ToString().ToLowerInvariant()));

                VerificationLimitConfig.MatchSome(verLimCfg =>
                {
                    claims.Add(new Claim(DkSmittestopClaims.Covid19LimitDuration, Convert.ToInt32(verLimCfg.MaxLimitDuration.TotalHours).ToString()));
                    claims.Add(new Claim(DkSmittestopClaims.Covid19LimitCount, verLimCfg.MaxVerificationsAllowed.ToString()));
                });
            }

            PositiveTestDate.Map(testDate => testDate.ToString("yyyy-MM-dd")).MatchSome(isoTestData =>
            {
                claims.Add(new Claim(VerificationClaims.VerifiedPositiveTestDate, isoTestData));
                claims.Add(new Claim(DkSmittestopClaims.Covid19InfectionStart, isoTestData));
            });

            return claims;
        }
    }
}
