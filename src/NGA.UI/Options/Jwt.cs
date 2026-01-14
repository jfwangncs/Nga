namespace NGA.UI.Options
{
    /// <summary>
    /// Jwt
    /// </summary>
    public class Jwt
    {
        /// <summary>
        /// Audience
        /// </summary>
        public required string Audience { get; set; }

        /// <summary>
        /// Issuer
        /// </summary>
        public required string Issuer { get; set; }

        /// <summary>
        /// SecurityKey
        /// </summary>
        public required string SecurityKey { get; set; }
    }
}
