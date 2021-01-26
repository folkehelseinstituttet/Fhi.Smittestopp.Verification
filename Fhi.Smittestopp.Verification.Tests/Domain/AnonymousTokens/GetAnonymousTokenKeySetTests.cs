using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnonymousTokens.Core.Services.InMemory;
using Fhi.Smittestopp.Verification.Domain.AnonymousTokens;
using FluentAssertions;
using Moq;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Domain.AnonymousTokens
{
    [TestFixture]
    public class GetAnonymousTokenKeySetTests
    {
        [Test]
        public async Task Handle_ReturnsKeySetDtoFromKeyStore()
        {
            var curveName = "P-256";

            var publicKey1 = (await new InMemoryPublicKeyStore().GetAsync()).Q;
            var publicKey2 = (await new InMemoryPublicKeyStore().GetAsync()).Q;

            var validationKeys = new[]
            {
                new AnonymousTokenValidationKey("k1", curveName, publicKey1),
                new AnonymousTokenValidationKey("k2", curveName, publicKey2)
            };

            var automocker = new AutoMocker();

            automocker
                .Setup<IAnonymousTokensKeyStore, Task<IEnumerable<AnonymousTokenValidationKey>>>(x => x.GetActiveValidationKeys())
                .ReturnsAsync(validationKeys);

            var target = automocker.CreateInstance<GetAnonymousTokenKeySet.Handler>();

            var result = await target.Handle(new GetAnonymousTokenKeySet.Query(), new CancellationToken());
            result.Keys
                .Should().Contain(k => 
                    k.Kid == "k1" && k.Crv == curveName && k.Kty == "EC" &&
                    k.X == Convert.ToBase64String(publicKey1.AffineXCoord.ToBigInteger().ToByteArray()) &&
                    k.Y == Convert.ToBase64String(publicKey1.AffineYCoord.ToBigInteger().ToByteArray()))
                .And.Contain(k =>
                    k.Kid == "k2" && k.Crv == curveName && k.Kty == "EC" &&
                    k.X == Convert.ToBase64String(publicKey2.AffineXCoord.ToBigInteger().ToByteArray()) &&
                    k.Y == Convert.ToBase64String(publicKey2.AffineYCoord.ToBigInteger().ToByteArray()));
        }
    }
}
