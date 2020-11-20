using System;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace Fhi.Smittestopp.Verification.Tests.TestUtils
{
    public class InMemoryDbContextBuilder<T> where T : DbContext
    {
        private readonly Func<DbContextOptions<T>, T> _factory;

        private readonly string _dbName;

        public InMemoryDbContextBuilder(Func<DbContextOptions<T>, T> factory, [CallerMemberName] string dbNavn = null)
        {
            _factory = factory;
            _dbName = dbNavn ?? Guid.NewGuid().ToString();
        }

        public T Build()
        {
            var options = new DbContextOptionsBuilder<T>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;

            return _factory(options);
        }
    }
}
