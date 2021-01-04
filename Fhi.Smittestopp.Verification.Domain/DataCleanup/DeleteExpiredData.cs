using System;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fhi.Smittestopp.Verification.Domain.DataCleanup
{
    public class DeleteExpiredData
    {
        public class Command : IRequest
        {

        }

        public class Handler : IRequestHandler<Command>
        {
            private readonly IVerificationLimit _verificationLimit;
            private readonly IVerificationRecordsRepository _verificationRecordsRepository;
            private readonly IAnonymousTokenIssueRecordRepository _anonymousTokenIssueRecordRepository;
            private readonly ILogger<Handler> _logger;

            public Handler(IVerificationLimit verificationLimit, IVerificationRecordsRepository verificationRecordsRepository, ILogger<Handler> logger, IAnonymousTokenIssueRecordRepository anonymousTokenIssueRecordRepository)
            {
                _verificationLimit = verificationLimit;
                _verificationRecordsRepository = verificationRecordsRepository;
                _logger = logger;
                _anonymousTokenIssueRecordRepository = anonymousTokenIssueRecordRepository;
            }

            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                await DeleteExpiredAnonymousTokenIssueRecords();
                await DeleteExpiredVerificationRecords();

                return Unit.Value;
            }

            private async Task DeleteExpiredAnonymousTokenIssueRecords()
            {
                var deleteCount = await _anonymousTokenIssueRecordRepository.DeleteExpiredRecords();
                _logger.LogInformation("Deleted {deleteCount} expired anonymous token issue records", deleteCount);
            }

            private async Task DeleteExpiredVerificationRecords()
            {
                var cutoff = DateTime.Now - _verificationLimit.Config.MaxLimitDuration;
                var deleteCount = await _verificationRecordsRepository.DeleteExpiredRecords(cutoff);
                _logger.LogInformation("Deleted {deleteCount} expired verification records", deleteCount);
            }
        }
    }
}
