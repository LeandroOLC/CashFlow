using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace CashFlow.Shared.Extensions
{
    public static class RabbitMqExtensions
    {
        public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(sp =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMq:Host"] ?? "localhost",
                    Port = int.Parse(configuration["RabbitMq:Port"] ?? "5672"),
                    UserName = configuration["RabbitMq:User"] ?? "guest",
                    Password = configuration["RabbitMq:Password"] ?? "guest",
                    VirtualHost = configuration["RabbitMq:VHost"] ?? "/",
                    ConsumerDispatchConcurrency = 1, // Importante para .NET 10: usar async
                };
                return factory;
            });

            services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
            {
                var factory = sp.GetRequiredService<RabbitMQ.Client.IConnectionFactory>();
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            return services;
        }
    }
}
