using FluentValidation;
using MediatR;
using PersonalLedger.Application.Auth.Services;
using PersonalLedger.Domain.Entities;
using PersonalLedger.Domain.Repositories;

namespace PersonalLedger.Application.Auth.Queries
{
    public record LoginQuery(string Email, string Password) : IRequest<AuthResult>;

    public class LoginQueryValidator : AbstractValidator<LoginQuery>
    {
        public LoginQueryValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty();
        }
    }

    public class LoginHandler : IRequestHandler<LoginQuery, AuthResult>
    {
        private readonly IRepository<User> _userRepository;
        private readonly ITokenService _tokenService;

        public LoginHandler(IRepository<User> userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<AuthResult> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetFirstAsync(u => u.Email == request.Email, cancellationToken);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

            var token = _tokenService.GenerateToken(user.Id, user.Email, user.Name);

            return new AuthResult(token, user.Email, user.Name);
        }
    }
}
