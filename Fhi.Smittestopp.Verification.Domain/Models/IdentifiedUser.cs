using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class IdentifiedUser : User
    {
        public IdentifiedUser(string nationalIdentifier, string pseudonym)
        {
            NationalIdentifier = nationalIdentifier.Some();
            Pseudonym = pseudonym;
        }

        public override Option<string> NationalIdentifier { get; }
        public override bool IsPinVerified => false;
        public override string Pseudonym { get; }
    }
}
