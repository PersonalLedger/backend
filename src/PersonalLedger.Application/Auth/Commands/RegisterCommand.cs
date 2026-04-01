using FluentValidation;
using MediatR;
using PersonalLedger.Application.Auth.Queries;
using PersonalLedger.Domain.Entities;
using PersonalLedger.Domain.Repositories;
using PersonalLedger.Domain.Services;

namespace PersonalLedger.Application.Auth.Commands
{
    public record RegisterCommand(string Name, string Email, string Cpf, string Password) : IRequest<AuthResult>;

    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        private readonly IRepository<User> _userRepository;

        public RegisterCommandValidator(IRepository<User> userRepository)
        {
            _userRepository = userRepository;

            RuleFor(x => x)
                .CustomAsync(async (command, context, cancellation) =>
                {
                    var existingUser = await _userRepository.GetFirstAsync(
                        u => u.Email == command.Email || u.Cpf == command.Cpf,
                        cancellation);

                    if (existingUser != null)
                    {
                        if (existingUser.Email == command.Email)
                            context.AddFailure("Email", "Este e-mail já está em uso.");

                        if (existingUser.Cpf == command.Cpf)
                            context.AddFailure("Cpf", "Este CPF já está registado.");
                    }
                });

            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(120);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Cpf)
                .NotEmpty()
                .Length(11);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);
        }
    }

    public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResult>
    {
        private readonly IRepository<User> _userRepository;
        private readonly IMediator _mediator;
        private readonly IRedisCacheService _cache;


        public RegisterHandler(IRepository<User> userRepository, IMediator mediator, IRedisCacheService cache)
        {
            _userRepository = userRepository;
            _mediator = mediator;
            _cache = cache;
        }

        public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
        { 
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = User.Create(request.Name, request.Email, request.Cpf, passwordHash);
            await _userRepository.AddAsync(user, cancellationToken);
            var response = await _mediator.Send(new LoginQuery(user.Email, user.PasswordHash), cancellationToken);
            await _cache.SetAsync(user);
            return response;
        }
    }
}
