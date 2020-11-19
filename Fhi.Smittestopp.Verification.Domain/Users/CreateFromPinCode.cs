using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;
using MediatR;
using Optional;

namespace Fhi.Smittestopp.Verification.Domain.Users
{
    public class CreateFromPinCode
    {
        public class Command : IRequest<Option<PinVerifiedUser>>
        {
            public string PinCode { get; set; }

            public Command(string pincode)
            {
                PinCode = pincode;
            }
        }

        public class Handler : IRequestHandler<Command, Option<PinVerifiedUser>>
        {
            public Task<Option<PinVerifiedUser>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Future PIN-code validation logic can be hooked up here
                return Task.FromResult(Option.None<PinVerifiedUser>());
            }
        }
    }
}
