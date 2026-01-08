using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using OrderApi.Functions.Models;
using System.Text.Json;
using OrderApi.Functions.Idempotency;
using OrderApi.Functions.Messaging;

namespace OrderApi.Functions;

public class CreateOrderFunction
{
    private readonly ILogger<CreateOrderFunction> _logger;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IOrderPublisher _publisher;

    private static readonly JsonSerializerOptions JsonOptions =
    new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CreateOrderFunction(ILogger<CreateOrderFunction> logger, IIdempotencyStore idempotencyStore, IOrderPublisher publisher)
    {
        _logger = logger;
        _idempotencyStore = idempotencyStore;
        _publisher = publisher;
    }

    [Function("CreateOrder")]
    [OpenApiOperation(operationId: "CreateOrder", tags: new[] { "Orders" }, Summary = "Create an order", Description = "Accepts an order and returns 202 Accepted with a correlation id.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(CreateOrderRequest), Required = true, Description = "Order payload")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(CreateOrderAcceptedResponse), Description = "Order accepted")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "Invalid request payload")]
    [OpenApiParameter(name: "Idempotency-Key", In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Idempotency key", Description = "If provided, repeated requests with the same key will return the same result without re-processing.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders")]
        HttpRequestData req)
    {
        _logger.LogInformation("Received create order request");

        var idempotencyKey = req.Headers.TryGetValues("Idempotency-Key", out var values)
            ? values.FirstOrDefault()
            : null;

        if (!string.IsNullOrWhiteSpace(idempotencyKey) &&
            _idempotencyStore.TryGet(idempotencyKey, out var existing))
        {
            _logger.LogInformation("Idempotency hit. Key={Key}", idempotencyKey);

            var cached = req.CreateResponse(HttpStatusCode.Accepted);
            await cached.WriteAsJsonAsync(existing); // put the stored response back into the http response body
            return cached;
        }


        var body = await new StreamReader(req.Body).ReadToEndAsync(); // convert json to text
        var order = JsonSerializer.Deserialize<CreateOrderRequest>(body, JsonOptions); // convert text to object

        if (order is null ||
            string.IsNullOrWhiteSpace(order.OrderId) ||
            string.IsNullOrWhiteSpace(order.CustomerId))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Invalid order payload");
            return badRequest;
        }

        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation( // structured logging
            "Order received. OrderId={OrderId}, CorrelationId={CorrelationId}",
            order.OrderId,
            correlationId);

        var result = new CreateOrderAcceptedResponse("Order accepted", correlationId); 

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _idempotencyStore.Set(idempotencyKey, result);
        }

        await _publisher.PublishAsync(new CreateOrderMessage( // send to service bus queue
                order.OrderId,
                order.CustomerId,
                order.Amount,
                correlationId,
                DateTimeOffset.UtcNow
            ));

        var response = req.CreateResponse(HttpStatusCode.Accepted); // 202: request has been accepted for processing, but the processing has not been finished yet
        await response.WriteAsJsonAsync(result);

        return response;
    }
}
