using Mapster;
using MapsterMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PersonalLedger.Application.Common.Interfaces;
using PersonalLedger.Domain.Repositories;
using PersonalLedger.Infrastructure.ExternalServices;
using PersonalLedger.Infrastructure.Messaging;
using PersonalLedger.Infrastructure.Persistence;
using PersonalLedger.Infrastructure.Repositories;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InfrastructureDependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddPersistence(configuration);
            services.AddMappings();
            services.AddExternalServices(configuration);
            services.AddRedisConfig(configuration);
            services.AddMessaging(configuration);

            return services;
        }

        private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        private static IServiceCollection AddMappings(this IServiceCollection services)
        {
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(typeof(InfrastructureDependencyInjection).Assembly);

            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();

            return services;
        }

        private static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30)
                );

            services.AddRefitClient<IPluggyApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri("https://api.pluggy.ai");
                })
                .AddPolicyHandler(retryPolicy)
                .AddPolicyHandler(circuitBreakerPolicy);

            services.AddScoped<IPluggyService, PluggyService>();

            return services;
        }

        private static IServiceCollection AddRedisConfig(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "PersonalLedger_"; // Prefixo para as chaves no Redis
            });

            return services;
        }

        private static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMassTransit(x =>
            {
                // -- PARTE 1 DO PLANO: OUTBOX GLOBAL NO SQL SERVER --
                x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
                {
                    o.UseSqlServer();
                    o.UseBusOutbox(); 
                });

                x.AddConsumers(typeof(InfrastructureDependencyInjection).Assembly);

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"]);
                        h.Password(configuration["RabbitMQ:Password"]);
                    });

                    cfg.UseMessageRetry(r => r.Exponential(
                        5,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(5)));

                    cfg.ConfigureEndpoints(context, new CustomEndpointNameFormatter());
                });
            });

            return services;
        }
    }
}
