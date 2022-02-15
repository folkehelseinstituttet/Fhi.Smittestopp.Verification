using System;
using Fhi.Smittestopp.Verification.Domain.Utilities.NationalIdentifiers;
using NUnit.Framework;

namespace Fhi.Smittestopp.Verification.Tests.Domain.Utilities
{
    [TestFixture]
    public class NationalIdentifierValidationTests
    {
        const string ValidFNummerBorn1994 = "08089409382";
        const string ValidDnummerBorn1989 = "44098923540";
        const string ValidFNummerBorn2020 = "01012068359";
        
        [TestCase(ValidFNummerBorn1994,ExpectedResult = true)]
        [TestCase(ValidDnummerBorn1989,ExpectedResult = false)]
        [TestCase(null,ExpectedResult = false)]
        public bool IsValidFnummer(string fnummer)
        {
            return fnummer.IsValidFNummer();
        }
        
        [TestCase(ValidDnummerBorn1989,ExpectedResult = true)]
        [TestCase(ValidFNummerBorn1994,ExpectedResult = false)]
        [TestCase(null,ExpectedResult = false)]
        [TestCase("",ExpectedResult = false)]
        [TestCase("1234",ExpectedResult = false)]
        public bool IsValidDnummer(string dnummer)
        {
            return dnummer.IsValidDNummer();
        }
        
        [TestCase(ValidFNummerBorn1994,ExpectedResult = true)]
        [TestCase(ValidDnummerBorn1989,ExpectedResult = true)]
        [TestCase("nan",ExpectedResult = false)]
        [TestCase(null,ExpectedResult = false)]
        [TestCase("08089412345",ExpectedResult = false)]
        public bool CanDetermineAge(string fOrDnummer)
        {
            return fOrDnummer.CanDetermineAge();
        }

        [TestCase(ValidFNummerBorn1994,16,ExpectedResult = false)]
        [TestCase(ValidDnummerBorn1989,16,ExpectedResult = false)]
        [TestCase(ValidDnummerBorn1989,16,ExpectedResult = false)]
        [TestCase(ValidFNummerBorn2020, 16, ExpectedResult = true)]
        [TestCase(ValidFNummerBorn1994,100,ExpectedResult = true)]
        [TestCase(null,16,ExpectedResult = true)]
        public bool IsPersonYoungerThanAgeLimit(string nationalIdentifier, int ageLimit)
        {
            return nationalIdentifier.IsPersonYoungerThanAgeLimit(ageLimit);
        }
    }
}