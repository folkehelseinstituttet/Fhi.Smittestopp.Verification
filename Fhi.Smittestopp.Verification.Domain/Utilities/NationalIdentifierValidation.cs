using System;
using System.Globalization;

namespace Fhi.Smittestopp.Verification.Domain.Utilities.NationalIdentifiers
{

    /// <summary>
    /// Extention methods for validation of national identifiers (Fødselsnummer and direktoratnummer).
    /// </summary>
    public static class NationalIdentifierValidation
    {
        /// <summary>
        ///     Validates a given fødselsnummer.
        /// </summary>
        /// <param name="fnr">The fødselsnummer to validate.</param>
        /// <returns>Whether the fødselsnummer was valid or not.</returns>
        public static bool IsValidFNummer(this string fnr)
        {
            if (string.IsNullOrEmpty(fnr) || fnr.Length != 11 || fnr.Contains(" "))
            {
                return false;
            }

            try
            {
                long.Parse(fnr);
                DateTime.ParseExact(fnr.Substring(0, 6), "ddMMyy", new CultureInfo("nb-NO"));
            }
            catch (FormatException)
            {
                return false;
            }

            return CheckControlDigits(fnr);
        }

        /// <summary>
        ///     Validates a given d-nummer.
        /// </summary>
        /// <param name="dnr">D-nummer to validate.</param>
        /// <returns>Whether the provided d-nummer was valid or not</returns>
        /// <remarks>
        ///     Et D-nummer er ellevesifret, som ordinære fødselsnummer, og består av en
        ///     modifisert sekssifret fødselsdato og et femsifret personnummer.
        ///     Fødselsdatoen modifiseres ved at det legges til 4 på det første sifferet:
        ///     en person født 1. januar 1980 får dermed fødselsdato 410180, mens en som er født 31. januar
        ///     1980 får 710180.
        /// </remarks>
        public static bool IsValidDNummer(this string dnr)
        {
            if (string.IsNullOrEmpty(dnr) || dnr.Length != 11 || dnr.Contains(" "))
            {
                return false;
            }

            try
            {
                long.Parse(dnr);
            }
            catch (FormatException)
            {
                return false;
            }

            var firstDigit = int.Parse(dnr.Substring(0, 1));

            if (firstDigit > 7 || (firstDigit - 4) < 0)
            {
                return false;
            }

            firstDigit = firstDigit - 4;
            try
            {
                DateTime.ParseExact(firstDigit + dnr.Substring(1, 5), "ddMMyy", new CultureInfo("nb-NO"));
            }
            catch (FormatException)
            {
                return false;
            }

            return CheckControlDigits(dnr);
        }
        
        public static DateTime BirthdateFromDNummer(this string dnr)
        {
            var firstDigit = int.Parse(dnr.Substring(0, 1));
            firstDigit -= 4;
            return DateTime.ParseExact(firstDigit + dnr.Substring(1, 5), "ddMMyy", new CultureInfo("nb-NO"));
        }
        
        /// <summary>
        ///     Get the birth date from the given 'Fødselsnummer'
        /// </summary>
        /// <returns></returns>
        public static DateTime BirthDateFromFNummer(this string fnr)
        {
            return DateTime.ParseExact(fnr.Substring(0, 6), "ddMMyy", new CultureInfo("nb-NO"));
        }

        /// <summary>
        /// Determines if it is possible to determine age of a given number
        /// </summary>
        /// <param name="fnrOrDnr"></param>
        /// <returns></returns>
        public static bool CanDetermineAge(this string fnrOrDnr)
        {
            return !string.IsNullOrEmpty(fnrOrDnr) && (fnrOrDnr.IsValidDNummer() || fnrOrDnr.IsValidFNummer());  
        } 
        
        /// <summary>
        /// Determine if a person is younger than a given age limit
        /// </summary>
        /// <param name="nationalIdentifier"></param>
        /// <param name="ageLimitInYears"></param>
        /// <returns></returns>
        public static bool IsPersonYoungerThanAgeLimit(this string nationalIdentifier, int ageLimitInYears)
        {
            if (!nationalIdentifier.CanDetermineAge())
            {
                // We cannot determine age, and must assume that the person is younger than age limit 
                return true;
            }
            DateTime birthdate = DateTime.Now.Date;
            if (nationalIdentifier.IsValidFNummer())
                birthdate = nationalIdentifier.BirthDateFromFNummer();
            else if (nationalIdentifier.IsValidDNummer())
                birthdate = nationalIdentifier.BirthdateFromDNummer();
            
            return birthdate.Date >= DateTime.Now.Date.AddYears(-ageLimitInYears);
        }
        
        /// <summary>
        /// Routine for validating control digits
        /// https://no.wikipedia.org/wiki/F%C3%B8dselsnummer 
        /// </summary>
        /// <param name="fNummerOrDNummer"></param>
        /// <returns></returns>
        private static bool CheckControlDigits(string fNummerOrDNummer)
        {
            var v1 = new [] { 3, 7, 6, 1, 8, 9, 4, 5, 2 } ;
            var v2 = new [] { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 } ;

            var s1 = 0;
            var s2 = 0;

            for (var i = 0; i < 9; i++)
            {
                s1 += Convert.ToInt16(fNummerOrDNummer.Substring(i, 1)) * v1[i];
            }

            var r1 = s1 % 11;
            var k1 = 11 - r1;

            if (k1 == 11)
            {
                k1 = 0;
            }
            else if (k1 == 10)
            {
                return false;
            }

            for (var i = 0; i < 10; i++)
            {
                s2 += Convert.ToInt16(fNummerOrDNummer.Substring(i, 1)) * v2[i];
            }

            var r2 = s2 % 11;
            var k2 = 11 - r2;

            if (k2 == 11)
            {
                k2 = 0;
            }
            else if (k2 == 10)
            {
                return false;
            }

            if ((Convert.ToInt16(fNummerOrDNummer.Substring(9, 1)) == k1 && Convert.ToInt16(fNummerOrDNummer.Substring(10, 1)) == k2))
            {
                return true;
            }

            return false;
        }
    }
}