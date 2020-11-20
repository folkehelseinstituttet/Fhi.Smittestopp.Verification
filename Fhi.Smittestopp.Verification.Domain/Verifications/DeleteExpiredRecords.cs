using System;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fhi.Smittestopp.Verification.Domain.Verifications
{
    public class DeleteExpiredRecords
    {
        public class Command : IRequest
        {
        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly IVerificationLimit _verificationLimit;
            private readonly IVerificationRecordsRepository _verificationRecordsRepository;
            private readonly ILogger<Handler> _logger;

            public Handler(IVerificationLimit verificationLimit, IVerificationRecordsRepository verificationRecordsRepository, ILogger<Handler> logger)
            {
                _verificationLimit = verificationLimit;
                _verificationRecordsRepository = verificationRecordsRepository;
                _logger = logger;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var cutoff = DateTime.Now - _verificationLimit.Config.MaxLimitDuration;
                var deleteCount = await _verificationRecordsRepository.DeleteExpiredRecords(cutoff);
                _logger.LogInformation("Deleted {deleteCount} expired records", deleteCount);
                return Unit.Value;
            }
        }
    }
}
