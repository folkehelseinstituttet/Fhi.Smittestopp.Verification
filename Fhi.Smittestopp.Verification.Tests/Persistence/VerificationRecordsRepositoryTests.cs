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
    public class VerificationRecordsRepositoryTests
    {
        [Test]
        public async Task SaveNewRecord_SavesNewRecordToDatabase()
        {
            var dbContextBuilder = new InMemoryDbContextBuilder<VerificationDbContext>(o => new VerificationDbContext(o));

            using (var db = dbContextBuilder.Build())
            {
                var automocker = new AutoMocker();

                automocker.Use(db);

                var target = automocker.CreateInstance<VerificationRecordsRepository>();

                await target.SaveNewRecord(new VerificationRecord("pseudo-1"));
            }

            using (var db = dbContextBuilder.Build())
            {
                var records = await db.VerificationRecords.ToListAsync();
                records.Count.Should().Be(1);
                records.Should().Contain(x => x.Pseudonym == "pseudo-1");
            }
        }

        [Test]
        public async Task RetrieveRecordsForPseudonym_GivenPseudonymAndCutoff_ReturnsRecordsForPseudnymAndAfterCutoff()
        {
            var dbContextBuilder = new InMemoryDbContextBuilder<VerificationDbContext>(o => new VerificationDbContext(o));

            using (var db = dbContextBuilder.Build())
            {
                db.VerificationRecords.AddRange(
                    new VerificationRecordEntity
                    {
                        Id = 1,
                        Pseudonym = "pseudo-1",
                        VerifiedAtTime = DateTimeOffset.Now.AddHours(-25)
                    },
                    new VerificationRecordEntity
                    {
                        Id = 2,
                        Pseudonym = "pseudo-1",
                        VerifiedAtTime = DateTimeOffset.Now.AddHours(-23)
                    },
                    new VerificationRecordEntity
                    {
                        Id = 3,
                        Pseudonym = "pseudo-2",
                        VerifiedAtTime = DateTimeOffset.Now.AddHours(-22)
                    });

                await db.SaveChangesAsync();
            }

            using (var db = dbContextBuilder.Build())
            {
                var automocker = new AutoMocker();

                automocker.Use(db);

                var target = automocker.CreateInstance<VerificationRecordsRepository>();

                var records = (await target.RetrieveRecordsForPseudonym("pseudo-1", DateTime.Now.AddHours(-24))).ToList();

                records.Count.Should().Be(1);
                records.Should().Contain(x => x.Pseudonym == "pseudo-1");
            }
        }

        [Test]
        public async Task DeleteExpiredRecords_GivenPseudonymAndCutoff_DeletesRecordsCreatedUpToIncCutoff()
        {
            var dbContextBuilder = new InMemoryDbContextBuilder<VerificationDbContext>(o => new VerificationDbContext(o));

            var cutoff = DateTime.UtcNow;

            using (var db = dbContextBuilder.Build())
            {
                db.VerificationRecords.AddRange(
                    new VerificationRecordEntity
                    {
                        Id = 1,
                        Pseudonym = "pseudo-1",
                        VerifiedAtTime = cutoff.AddHours(-2)
                    },
                    new VerificationRecordEntity
                    {
                        Id = 2,
                        Pseudonym = "pseudo-2",
                        VerifiedAtTime = DateTimeOffset.Now.AddHours(-1)
                    },
                    new VerificationRecordEntity
                    {
                        Id = 3,
                        Pseudonym = "pseudo-1",
                        VerifiedAtTime = cutoff
                    },
                    new VerificationRecordEntity
                    {
                        Id = 4,
                        Pseudonym = "pseudo-3",
                        VerifiedAtTime = cutoff.AddHours(1)
                    });

                await db.SaveChangesAsync();
            }

            using (var db = dbContextBuilder.Build())
            {
                var automocker = new AutoMocker();

                automocker.Use(db);

                var target = automocker.CreateInstance<VerificationRecordsRepository>();

                var deletedCount = await target.DeleteExpiredRecords(cutoff);

                deletedCount.Should().Be(3);
            }

            using (var db = dbContextBuilder.Build())
            {
                var records = await db.VerificationRecords.ToListAsync();
                records.Count.Should().Be(1);
                records.Should().Contain(x => x.Pseudonym == "pseudo-3");
            }
        }
    }
}
