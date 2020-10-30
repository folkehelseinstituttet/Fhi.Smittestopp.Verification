using System;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class VerificationRecord
    {
        public string Pseudonym { get; }
        public DateTime VerifiedAtTime { get; }

        public VerificationRecord(string pseudonym) : this(pseudonym, DateTime.UtcNow)
        {
        }

        public VerificationRecord(string pseudonym, DateTime verifiedAtTime)
        {
            Pseudonym = pseudonym;
            VerifiedAtTime = verifiedAtTime;
        }
    }
}
