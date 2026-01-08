namespace OrderApi.Functions.Models
{
    public sealed record CreateOrderAcceptedResponse(
        string Message,
        string CorrelationId
    );
}
