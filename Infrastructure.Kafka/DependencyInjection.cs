using Application.Interfaces;
using Confluent.Kafka;
using Infrastructure.Kafka.Fallbacks;
using Infrastructure.Kafka.Services;
using Infrastructure.Kafka.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Kafka;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureKafka(this IServiceCollection services, IConfiguration configuration)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"];
        bool isKafkaAlive = false;

        if (!string.IsNullOrEmpty(bootstrapServers))
        {
            try
            {
                // Ping Kafka 3 giây để kiểm tra sinh tử
                using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
                adminClient.GetMetadata(TimeSpan.FromSeconds(3));

                isKafkaAlive = true;
                Console.WriteLine("✅ Kafka Server OK!");
            }
            catch(Exception ex)
            {
                Console.WriteLine("⚠️ Kafka sập! Bật chế độ Fallback...");
            }
        }

        if (isKafkaAlive)
        {
            // Kafka sống -> Dùng đồ thật + Bật Worker
            services.AddSingleton<IKafkaPublisher, KafkaPublisher>();
            services.AddHostedService<AuditLogWorker>();
        }
        else
        {
            // Kafka chết -> Dùng đồ giả + Tắt Worker để đỡ lỗi
            services.AddSingleton<IKafkaPublisher, FallbackKafkaPublisher>();
        }

        return services;
    }
}