using System;
using System.Linq;
using System.Threading.Tasks;
using Fhi.Smittestopp.Verification.Domain.Models;
using Fhi.Smittestopp.Verification.Persistence;
using Fhi.Smittestopp.Verification.Persistence.Entities;
using Fhi.Smittestopp.Verification.Persistence.Repositories;
using Fhi.Smittestopp.Verification.Tests.TestUtils;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq.AutoMock;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Persistence
{
    [TestFixture]
    public class AnonymousTokenIssueRecordRepositoryTest
    {
        [Test]
        public async Task SaveNewRecord_SavesNewRecordToDatabase()
        {
            var dbContextBuilder = new InMemoryDbContextBuilder<VerificationDbContext>(o => new VerificationDbContext(o));

            using (var db = dbContextBuilder.Build())
            {
                var automocker = new AutoMocker();

                automocker.Use(db);

                var target = automocker.CreateInstance<AnonymousTokenIssueRecordRepository>();

                await target.SaveNewRecord(new AnonymousTokenIssueRecord("token-A", DateTime.Now.AddMinutes(10)));
            }

            using (var db = dbContextBuilder.Build())
            {
                var records = await db.AnonymousTokenIssueRecords.ToListAsync();
                records.Count.Should().Be(1);
                records.Should().Contain(x => x.JwtTokenId == "token-A");
            }
        }

        [Test]
        public async Task RetrieveRecordsForPseudonym_GivenPseudonymAndCutoff_ReturnsRecordsForPseudnymAndAfterCutoff()
        {
            var dbContextBuilder = new InMemoryDbContextBuilder<VerificationDbContext>(o => new VerificationDbContext(o));

            using (var db = dbContextBuilder.Build())
            {
                db.AnonymousTokenIssueRecords.AddRange(
                    new AnonymousTokenIssueRecordEntity
                    {
                        Id = 1,
                        JwtTokenId = "token-A",
                        JwtTokenExpiry = DateTimeOffset.Now.AddMinutes(-10)
                    },
                    new AnonymousTokenIssueRecordEntity
                    {
                        Id = 2,
                        JwtTokenId = "token-B",
                        JwtTokenExpiry = DateTimeOffset.Now.AddMinutes(5)
                    },
                    new AnonymousTokenIssueRecordEntity
                    {
                        Id = 3,
                        JwtTokenId = "token-C",
                        JwtTokenExpiry = DateTimeOffset.Now.AddMinutes(10)
                    });

                await db.SaveChangesAsync();
            }

            using (var db = dbContextBuilder.Build())
            {
                var automocker = new AutoMocker();

                automocker.Use(db);

                var target = automocker.CreateInstance<AnonymousTokenIssueRecordRepository>();

                var records = (await target.RetrieveRecordsJwtToken("token-B")).ToList();

                records.Count.Should().Be(1);
                records.Should().Contain(x => x.JwtTokenId == "token-B");
            }
        }

        [Test]
        public async Task DeleteExpiredRecords_GivenPseudonymAndCutoff_DeletesRecordsCreatedUpToIncCutoff()
        {
            var dbContextBuilder = new InMemoryDbContextBuilder<VerificationDbContext>(o => new VerificationDbContext(o));

            using (var db = dbContextBuilder.Build())
            {
                db.AnonymousTokenIssueRecords.AddRange(
                    new AnonymousTokenIssueRecordEntity
                    {
                        Id = 1,
                        JwtTokenId = "token-A",
                        JwtTokenExpiry = DateTimeOffset.Now.AddMinutes(-10)
                    },
                    new AnonymousTokenIssueRecordEntity
                    {
                        Id = 2,
                        JwtTokenId = "token-B",
                        JwtTokenExpiry = DateTimeOffset.Now.AddMinutes(5)
                    },
                    new AnonymousTokenIssueRecordEntity
                    {
                        Id = 3,
                        JwtTokenId = "token-C",
                        JwtTokenExpiry = DateTimeOffset.Now.AddMinutes(10)
                    });

                await db.SaveChangesAsync();
            }

            using (var db = dbContextBuilder.Build())
            {
                var automocker = new AutoMocker();

                automocker.Use(db);

                var target = automocker.CreateInstance<AnonymousTokenIssueRecordRepository>();

                var deletedCount = await target.DeleteExpiredRecords();

                deletedCount.Should().Be(1);
            }

            using (var db = dbContextBuilder.Build())
            {
                var records = await db.AnonymousTokenIssueRecords.ToListAsync();
                records.Count.Should().Be(2);
                records.Should().NotContain(x => x.JwtTokenId == "token-A");
            }
        }
    }
}
