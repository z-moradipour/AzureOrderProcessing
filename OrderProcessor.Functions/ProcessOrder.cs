using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderApi.Functions.Messaging;
using System.Text.Json;

namespace OrderProcessor.Functions;

public class ProcessOrder
{
    private readonly ILogger<ProcessOrder> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ProcessOrder(ILogger<ProcessOrder> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessOrder))]
    public async Task Run(
        [ServiceBusTrigger("orders", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation(
            "MessageId={MessageId} CorrelationId={CorrelationId} DeliveryCount={DeliveryCount}",
            message.MessageId,
            message.CorrelationId,
            message.DeliveryCount);

        CreateOrderMessage order;

        try
        {
            order = JsonSerializer.Deserialize<CreateOrderMessage>(
                message.Body.ToString(),
                JsonOptions)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deserialization failed. Dead-lettering message.");

            await messageActions.DeadLetterMessageAsync(
                message,
                new Dictionary<string, object>
                {
                    ["DeadLetterReason"] = "DeserializationFailed",
                    ["DeadLetterErrorDescription"] = ex.Message
                });

            return;
        }

        if (string.IsNullOrWhiteSpace(order.OrderId) ||
            string.IsNullOrWhiteSpace(order.CustomerId))
        {
            await messageActions.DeadLetterMessageAsync(
                message,
                new Dictionary<string, object>
                {
                    ["DeadLetterReason"] = "InvalidPayload",
                    ["DeadLetterErrorDescription"] = "OrderId or CustomerId is missing"
                });

            return;
        }

        // testing "retry" and "dlq" mechanism with transient failure simulation
        var simulate = Environment.GetEnvironmentVariable("SimulateFailures") == "true";
        if (simulate && order.CustomerId == "FAIL")
        {
            _logger.LogWarning("Simulating transient failure for OrderId={OrderId}", order.OrderId);
            throw new Exception("Simulated processing failure");
        }

        await messageActions.CompleteMessageAsync(message);

        _logger.LogInformation(
            "Processing order {OrderId} for customer {CustomerId}",
            order.OrderId,
            order.CustomerId);

    }
}