using Mapster;
using MapsterMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PersonalLedger.Infrastructure.ExternalServices;
using PersonalLedger.Infrastructure.Persistence;
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
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
            services.AddRefitClient<IPluggyApi>()
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri("https://api.pluggy.ai");
                    c.DefaultRequestHeaders.Add("X-API-KEY", configuration["Pluggy:ApiKey"]);
                })
                .AddPolicyHandler(retryPolicy);

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
                x.AddConsumers(typeof(ApplicationDependencyInjection).Assembly);

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                    {
                        h.Username(configuration["RabbitMQ:Username"]);
                        h.Password(configuration["RabbitMQ:Password"]);
                    });

                    cfg.ConfigureEndpoints(context);
                    cfg.UseMessageRetry(r => r.Exponential(
                        5,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(5)));
                });
            });

            return services;
        }
    }
}
