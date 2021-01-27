using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fhi.Smittestopp.Verification.Domain.Constants;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using IdentityModel;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class VerificationResult
    {
        private static readonly string[] PossibleVerificationClaims =
        {
            JwtClaimTypes.Role,
            VerificationClaims.VerifiedPositiveTestDate,
            DkSmittestopClaims.Covid19Status,
            DkSmittestopClaims.Covid19InfectionStart,
            DkSmittestopClaims.Covid19Blocked,
            DkSmittestopClaims.Covid19LimitDuration,
            DkSmittestopClaims.Covid19LimitCount
        };

        private Option<PositiveTestResult> _testresult;

        /// <summary>
        /// Constructor for positive verifications
        /// </summary>
        public VerificationResult(PositiveTestResult testresult,
            IEnumerable<VerificationRecord> priorVerifications,
            IVerificationLimit verificationLimit)
        {
            _testresult = testresult.Some();
            VerificationLimitExceeded = verificationLimit.HasReachedLimit(priorVerifications);
            VerificationLimitConfig = verificationLimit.Config.Some();
        }

        /// <summary>
        /// Constructor for negative verifications
        /// </summary>
        public VerificationResult()
        {
            _testresult = default;
            VerificationLimitConfig = default;
            VerificationLimitExceeded = false;
        }

        public bool HasVerifiedPostiveTest => _testresult.HasValue;
        public bool VerificationLimitExceeded { get; }
        public bool CanUploadKeys => HasVerifiedPostiveTest && !VerificationLimitExceeded;
        public Option<IVerificationLimitConfig> VerificationLimitConfig { get; }
        public Option<DateTime> PositiveTestDate => _testresult.FlatMap(x => x.PositiveTestDate);

        public IEnumerable<Claim> GetVerificationClaims()
        {
            var claims = new List<Claim>
            {
                new Claim(DkSmittestopClaims.Covid19Status, HasVerifiedPostiveTest
                    ? DkSmittestopClaims.StatusValues.Positive
                    : DkSmittestopClaims.StatusValues.Negative)
            };

            PositiveTestDate.Map(testDate => testDate.ToString("yyyy-MM-dd")).MatchSome(isoTestDate =>
            {
                claims.Add(new Claim(VerificationClaims.VerifiedPositiveTestDate, isoTestDate));
                claims.Add(new Claim(DkSmittestopClaims.Covid19InfectionStart, isoTestDate));
            });

            if (HasVerifiedPostiveTest)
            {
                // Blocking claims is only relevant for positive users
                claims.Add(new Claim(DkSmittestopClaims.Covid19Blocked, VerificationLimitExceeded.ToString().ToLowerInvariant()));
                if (VerificationLimitExceeded)
                {
                    VerificationLimitConfig.MatchSome(verLimCfg =>
                    {
                        claims.Add(new Claim(DkSmittestopClaims.Covid19LimitDuration, Convert.ToInt32(verLimCfg.MaxLimitDuration.TotalHours).ToString()));
                        claims.Add(new Claim(DkSmittestopClaims.Covid19LimitCount, verLimCfg.MaxVerificationsAllowed.ToString()));
                    });
                }
            }

            if (CanUploadKeys)
            {
                // grants access to JWT to anonymous token exchange
                claims.Add(new Claim(JwtClaimTypes.Role, VerificationRoles.UploadApproved));
            }

            return claims;
        }

        /// <summary>
        /// Check to see if verification claims has been requested, to see if a verification should be performed
        /// </summary>
        /// <param name="requestedClaims">The list of requested claims</param>
        /// <returns>Boolean indicating if any of the requested claims require a verification</returns>
        public static bool RequestedClaimsRequiresVerification(IEnumerable<string> requestedClaims)
        {
            return requestedClaims.Any(x => PossibleVerificationClaims.Contains(x));
        }
    }
}
