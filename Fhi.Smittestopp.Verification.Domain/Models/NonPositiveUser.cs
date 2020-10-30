using System;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class NonPositiveUser : User
    {
        public override bool HasVerifiedPostiveTest => false;
        public override bool VerificationLimitExceeded => false;
        public override Option<IVerificationLimitConfig> VerificationLimitConfig => Option.None<IVerificationLimitConfig>();
        public override Option<DateTime> PositiveTestDate => Option.None<DateTime>();
    }
}
