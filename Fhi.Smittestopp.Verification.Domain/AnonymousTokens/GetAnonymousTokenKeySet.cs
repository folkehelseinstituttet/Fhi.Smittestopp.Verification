using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Dtos;
using MediatR;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.X509;

namespace Fhi.Smittestopp.Verification.Domain.AnonymousTokens
{
    public class GetAnonymousTokenKeySet
    {
        public class Query : IRequest<AnonymousTokenKeySet>
        {

        }

        public class Handler : IRequestHandler<Query, AnonymousTokenKeySet>
        {
            private readonly IAnonymousTokensKeyStore _anonymousTokensKeyStore;

            public Handler(IAnonymousTokensKeyStore anonymousTokensKeyStore)
            {
                _anonymousTokensKeyStore = anonymousTokensKeyStore;
            }

            public async Task<AnonymousTokenKeySet> Handle(Query request, CancellationToken cancellationToken)
            {
                var validCerts = await _anonymousTokensKeyStore.GetActiveValidationKeys();

                return new AnonymousTokenKeySet
                {
                    Keys = validCerts.Select(c => new AnonymousTokenKey
                    {
                        Kid = c.Kid,
                        PublicKeyAsHex = Hex.ToHexString(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(c.PublicKey).GetEncoded())
                    }).ToList()
                };
            }
        }
    }
}
