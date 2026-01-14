namespace NGA.UI.Options
{
    public class JwtSettings
    {
        public required string Audience { get; set; }

        public required string Issuer { get; set; }

        public required string SecretKey { get; set; }

        public int ExpireMinutes { get; set; }
    }
}
