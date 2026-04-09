using System.ComponentModel.DataAnnotations;

namespace ASB.POC.Infrastructure.Messaging.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    [Required(ErrorMessage = "Kafka:BootstrapServers is required")]
    public string BootstrapServers { get; init; } = string.Empty;

    [Required(ErrorMessage = "Kafka:Topic is required")]
    public string Topic { get; init; } = string.Empty;

    public string GroupId { get; init; } = "asb-poc-consumer";
}
