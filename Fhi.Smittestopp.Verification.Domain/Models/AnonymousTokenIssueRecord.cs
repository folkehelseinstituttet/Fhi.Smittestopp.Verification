using System;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class AnonymousTokenIssueRecord
    {
        public string JwtTokenId { get; }
        public DateTime JwtTokenExpiry { get; }

        public AnonymousTokenIssueRecord(string jwtTokenId, DateTime jwtTokenExpiry)
        {
            JwtTokenId = jwtTokenId;
            JwtTokenExpiry = jwtTokenExpiry;
        }
    }
}
