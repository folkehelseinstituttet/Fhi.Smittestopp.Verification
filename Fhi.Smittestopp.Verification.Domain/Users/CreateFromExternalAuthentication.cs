using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Constans;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;
using IdentityModel;
using MediatR;
using Microsoft.Extensions.Logging;
using Optional;
using Optional.Async.Extensions;
using Optional.Collections;

namespace Fhi.Smittestopp.Verification.Domain.Users
{
    public class CreateFromExternalAuthentication
    {
        public class Command : IRequest<User>
        {
            public ICollection<Claim> ExternalClaims { get; set; }
            public string Provider { get; set; }

            public Command(string provider, IEnumerable<Claim> claims)
            {
                Provider = provider;
                ExternalClaims = claims.ToList();
            }
        }

        public class Handler : IRequestHandler<Command, User>
        {
            private readonly IMsisLookupService _msisLookupService;
            private readonly ILogger<CreateFromExternalAuthentication> _logger;
            private readonly IVerificationLimit _verificationLimit;
            private readonly IVerificationRecordsRepository _verificationRecordsRepository;
            private readonly IPseudonymFactory _pseudonymFactory;

            public Handler(IMsisLookupService msisLookupService,
                ILogger<CreateFromExternalAuthentication> logger,
                IVerificationLimit verificationLimit,
                IVerificationRecordsRepository verificationRecordsRepository,
                IPseudonymFactory pseudonymFactory)
            {
                _msisLookupService = msisLookupService;
                _logger = logger;
                _verificationLimit = verificationLimit;
                _verificationRecordsRepository = verificationRecordsRepository;
                _pseudonymFactory = pseudonymFactory;
            }

            public async Task<User> Handle(Command request, CancellationToken cancellationToken)
            {
                var positiveTest = await FindTestresultForExternalUser(request.Provider, request.ExternalClaims);

                return await positiveTest.MatchAsync(
                    none: CreateNonPositiveUser,
                    some: pt => CreatePositiveUser(pt, request));
            }

            private Task<User> CreateNonPositiveUser()
            {
                _logger.LogInformation("Creating non-positive user after ID-porten login and MSIS lookup");
                return Task.FromResult<User>(new NonPositiveUser());
            }

            private async Task<User> CreatePositiveUser(PositiveTestResult testResult, Command request)
            {
                var userIdClaim = FindUserIdClaim(request.ExternalClaims).ValueOr(() =>
                    throw new Exception("Unable to determine user-ID from external claims from provider: " + request.Provider));

                var pseudonym = _pseudonymFactory.Create(request.Provider + ":" + userIdClaim.Value);
                var existingRecords =
                    await _verificationRecordsRepository.RetrieveRecordsForPseudonym(pseudonym);
                var newRecord = new VerificationRecord(pseudonym);

                var verificationRecords = existingRecords.Concat(new[] { newRecord });

                _logger.LogInformation("Verified positive user created after ID-porten login and MSIS lookup");
                var postiveUser = new PositiveUser(request.Provider, userIdClaim.Value, testResult, verificationRecords, _verificationLimit);

                await _verificationRecordsRepository.SaveNewRecord(newRecord);

                return postiveUser;
            }

            private Option<Claim> FindUserIdClaim(ICollection<Claim> claims)
            {
                return claims.FirstOrNone(c => c.Type == JwtClaimTypes.Subject)
                    .Else(() => claims.FirstOrNone(c => c.Type == ClaimTypes.NameIdentifier));
            }

            private Task<Option<PositiveTestResult>> FindTestresultForExternalUser(string provider, ICollection<Claim> claims)
            {
                var nationalIdentifierClaim = FindNationalIdentifierClaim(provider, claims);
                nationalIdentifierClaim.MatchNone(() => _logger.LogWarning("Unable to locate national identifier for external user from provider: " + provider));
                return nationalIdentifierClaim.FlatMapAsync(natIdClaim => _msisLookupService.FindPositiveTestResult(natIdClaim.Value));
            }

            private Option<Claim> FindNationalIdentifierClaim(string provider, ICollection<Claim> claims)
            {
                switch (provider)
                {
                    case ExternalProviders.IdPorten:
                        return claims.FirstOrNone(c => c.Type == IdPortenClaims.NationalIdentifier);
                    default:
                        return Option.None<Claim>();
                }
            }
        }
    }
}
