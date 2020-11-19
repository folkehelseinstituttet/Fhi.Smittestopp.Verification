using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class PinVerifiedUser : User
    {
        public PinVerifiedUser(string pseudonym)
        {
            Pseudonym = pseudonym;
        }

        public override string Pseudonym { get; }
        public override bool IsPinVerified => true;
        public override Option<string> NationalIdentifier => default;
    }
}
