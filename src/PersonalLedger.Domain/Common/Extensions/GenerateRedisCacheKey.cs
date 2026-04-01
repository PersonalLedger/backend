namespace PersonalLedger.Domain.Common.Extensions
{
    public static class GenerateRedisCacheKey
    {
        public static string GenerateKey<T>(this T baseEntityValue) where T : BaseEntity
        {
            var className = baseEntityValue.GetType().Name;

            // Pega o Id da instância
            var id = baseEntityValue.Id.ToString()?.ToLower();

            return $"{className}:{id}";
        }
    }
}
