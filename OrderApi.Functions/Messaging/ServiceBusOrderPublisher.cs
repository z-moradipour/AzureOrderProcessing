using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace OrderApi.Functions.Messaging
{
    public interface IOrderPublisher
    {
        Task PublishAsync(CreateOrderMessage message, CancellationToken ct = default);
    }

    public sealed class ServiceBusOrderPublisher : IOrderPublisher
    {
        private readonly ServiceBusSender _sender;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ServiceBusOrderPublisher(ServiceBusClient client)
        {
            _sender = client.CreateSender("orders"); // queue name: "orders"
        }

        public async Task PublishAsync(CreateOrderMessage message, CancellationToken ct = default)
        {
            var body = JsonSerializer.Serialize(message, JsonOptions);

            var sbMessage = new ServiceBusMessage(body)
            {
                ContentType = "application/json",
                MessageId = message.OrderId,              // help with duplicate detection in the future
                CorrelationId = message.CorrelationId     // for end-to-end tracing
            };          

            sbMessage.ApplicationProperties["customerId"] = message.CustomerId;

            await _sender.SendMessageAsync(sbMessage, ct);
        }
    }

    public sealed record CreateOrderMessage(
        string OrderId,
        string CustomerId,
        decimal Amount,
        string CorrelationId,
        DateTimeOffset CreatedAtUtc
    );
}
