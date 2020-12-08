
using AnonymousTokens.Core.Services;
using AnonymousTokens.Server.Protocol;

using Fhi.Smittestopp.Verification.Domain.Dtos;
using Fhi.Smittestopp.Verification.Domain.Interfaces;
using Fhi.Smittestopp.Verification.Domain.Models;

using MediatR;

using Microsoft.Extensions.Options;

using Optional;

using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Utilities.Encoders;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class IssueAnonymousToken
    {
        public class Command : IRequest<Option<AnonymousTokenResponse, string>>
        {
            public string JwtTokenId { get; set; }
            public DateTime JwtTokenExpiry { get; set; }
            public AnonymousTokenRequest RequestData { get; set; }
        }

        public class Handler : IRequestHandler<Command, Option<AnonymousTokenResponse, string>>
        {
            private readonly IAnonymousTokenIssueRecordRepository _anonymousTokenIssueRecordRepository;
            private readonly IAnonymousTokensCertLocator _certLocator;
            private readonly IPublicKeyStore _publicKeyStore;
            private readonly IPrivateKeyStore _privateKeyStore;
            private readonly ITokenGenerator _tokenGenerator;
            private readonly AnonymousTokensConfig _config;

            public Handler(
                IAnonymousTokenIssueRecordRepository anonymousTokenIssueRecordRepository,
                IAnonymousTokensCertLocator certLocator,
                IPublicKeyStore publicKeyStore,
                IPrivateKeyStore privateKeyStore,
                ITokenGenerator tokenGenerator,
                IOptions<AnonymousTokensConfig> config)
            {
                _anonymousTokenIssueRecordRepository = anonymousTokenIssueRecordRepository;
                _certLocator = certLocator;
                _publicKeyStore = publicKeyStore;
                _privateKeyStore = privateKeyStore;
                _tokenGenerator = tokenGenerator;
                _config = config.Value;
            }

            public async Task<Option<AnonymousTokenResponse, string>> Handle(Command request, CancellationToken cancellationToken)
            {
                if (!_config.Enabled)
                {
                    return Option.None<AnonymousTokenResponse, string>("Anonymous tokens are not enabled for this environment.");
                }

                var existingRecord =
                    await _anonymousTokenIssueRecordRepository.RetrieveRecordsJwtToken(request.JwtTokenId);

                if (existingRecord.Any())
                {
                    return Option.None<AnonymousTokenResponse, string>("Anonymous token already issued for the provided JWT-token ID.");
                }

                var tokenResult = await CreateAnonymousTokenForRequestAsync(request.RequestData);

                await _anonymousTokenIssueRecordRepository.SaveNewRecord(new AnonymousTokenIssueRecord(request.JwtTokenId, request.JwtTokenExpiry));

                return tokenResult.Some<AnonymousTokenResponse, string>();
            }

            private async Task<AnonymousTokenResponse> CreateAnonymousTokenForRequestAsync(AnonymousTokenRequest request)
            {
                var ecParameters = CustomNamedCurves.GetByOid(X9ObjectIdentifiers.Prime256v1);

                var k = await _privateKeyStore.GetAsync();
                var K = await _publicKeyStore.GetAsync();
                var P = ecParameters.Curve.DecodePoint(Hex.Decode(request.PAsHex));

                var token = _tokenGenerator.GenerateToken(k, K.Q, ecParameters, P);
                var Q = token.Q;
                var c = token.c;
                var z = token.z;

                return new AnonymousTokenResponse(Q, c, z);
            }
        }
    }
}
