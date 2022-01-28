namespace Fhi.Smittestopp.Verification.Domain.Constants
{
    /// <summary>
    /// 
    /// https://docs.digdir.no/oidc_protocol_id_token.html
    ///
    /// “Personidentifikator” - the Norwegian national ID number (fødselsnummer/d-nummer) of the autenticated end user.
    /// This claim is not included if no_pid scope was requested or pre-registered on the client.
    /// 
    /// </summary>
    public static class IdPortenClaims
    {
        public const string NationalIdentifier = "pid";
    }
}
