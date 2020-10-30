using System;
using System.Collections.Generic;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class PositiveUser : User
    {
        public PositiveUser(string provider, string providerUserId, PositiveTestResult testresult, IEnumerable<VerificationRecord> existingVerificationRecords, IVerificationLimit verificationLimit) : base(provider, providerUserId)
        {
            PositiveTestDate = testresult.PositiveTestDate;
            VerificationLimitExceeded = verificationLimit.HasExceededLimit(existingVerificationRecords);
            VerificationLimitConfig = verificationLimit.Config.Some();
        }

        public override bool HasVerifiedPostiveTest => true;
        public override bool VerificationLimitExceeded { get; }
        public override Option<IVerificationLimitConfig> VerificationLimitConfig { get; }
        public override Option<DateTime> PositiveTestDate { get; }
    }
}
