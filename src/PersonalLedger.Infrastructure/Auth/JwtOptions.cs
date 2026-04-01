namespace PersonalLedger.Infrastructure.Auth
{
    public class JwtOptions
    {
        public const string SectionName = "JWT";
        public string Secret { get; set; } = string.Empty;
        public int ExpirationInHours { get; set; } = 8;
    }
}
