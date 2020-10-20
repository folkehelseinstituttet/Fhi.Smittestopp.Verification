using System;
using Fhi.Smittestopp.Verification.Domain.Models;
using FluentAssertions;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Models
{
    [TestFixture]
    public class NonPositiveUserTests
    {
        [Test]
        public void Constructor_CreatesUserWithTempId()
        {
            var target = new NonPositiveUser();

            Guid.TryParse(target.Id, out var id).Should().BeTrue();
            id.Should().NotBe(Guid.Empty);
        }

        [Test]
        public void HasVerifiedPostiveTest_ForNonPositiveUser_ShouldBeFalse()
        {
            var target = new NonPositiveUser();

            target.HasVerifiedPostiveTest.Should().BeFalse();
        }

        [Test]
        public void GetCustomClaims_ForNonPositiveUser_ShouldBeEmpty()
        {
            var target = new NonPositiveUser();

            target.GetCustomClaims().Should().BeEmpty();
        }
    }
}
