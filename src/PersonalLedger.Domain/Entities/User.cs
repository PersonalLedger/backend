using PersonalLedger.Domain.Common;

namespace PersonalLedger.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; private set; } = string.Empty;
        public string Cpf { get; private set; } =string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string Name { get; private set; } = string.Empty;

        private User() { }

        public static User Create(string name, string email, string cpf, string passwordHash)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Name = name,
                Email = email.ToLowerInvariant(),
                Cpf = cpf,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
