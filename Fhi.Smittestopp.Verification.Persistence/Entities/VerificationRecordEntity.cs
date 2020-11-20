using System;

namespace Fhi.Smittestopp.Verification.Persistence.Entities
{
    public class VerificationRecordEntity
    {
        public int Id { get; set; }
        public string Pseudonym { get; set; }
        public DateTimeOffset VerifiedAtTime { get; set; }
    }
}
