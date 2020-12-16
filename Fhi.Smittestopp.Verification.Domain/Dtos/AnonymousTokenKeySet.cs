using System.Collections.Generic;

namespace Fhi.Smittestopp.Verification.Domain.Dtos
{
    public class AnonymousTokenKeySet
    {
        public ICollection<AnonymousTokenKey> Keys { get; set; }
    }

    public class AnonymousTokenKey
    {
        public string Kid { get; set; }
        public string PublicKeyAsHex { get; set; }
    }
}
