using System;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class PositiveUser : User
    {
        public PositiveUser(string provider, string providerUserId, PositiveTestResult testresult) : base(provider, providerUserId)
        {
            PositiveTestDate = testresult.PositiveTestDate;
        }

        public override bool HasVerifiedPostiveTest => true;
        public override Option<DateTime> PositiveTestDate { get; }
    }
}
