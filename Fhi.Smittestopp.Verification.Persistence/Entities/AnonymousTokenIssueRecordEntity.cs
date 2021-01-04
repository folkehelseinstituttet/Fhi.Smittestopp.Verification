using System;

namespace Fhi.Smittestopp.Verification.Persistence.Entities
{
    public class AnonymousTokenIssueRecordEntity
    {
        public int Id { get; set; }
        public string JwtTokenId { get; set; }
        public DateTimeOffset JwtTokenExpiry { get; set; }
    }
}
