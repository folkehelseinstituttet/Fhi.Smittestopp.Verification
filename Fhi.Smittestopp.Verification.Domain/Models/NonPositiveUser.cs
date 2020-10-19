using System;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Models
{
    public class NonPositiveUser : User
    {
        public NonPositiveUser() : base(Guid.NewGuid().ToString())
        {

        }

        public override bool HasVerifiedPostiveTest => false;
        public override Option<DateTime> PositiveTestDate => Option.None<DateTime>();
    }
}
