using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderApi.Functions.Messaging;

var serviceBusConnection = Environment.GetEnvironmentVariable("ServiceBusConnection")
    ?? throw new InvalidOperationException("ServiceBusConnection setting is missing.");

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<OrderApi.Functions.Idempotency.IIdempotencyStore, OrderApi.Functions.Idempotency.InMemoryIdempotencyStore>();
        services.AddSingleton(_ => new ServiceBusClient(serviceBusConnection));
        services.AddSingleton<IOrderPublisher, ServiceBusOrderPublisher>();
    })
    .Build();

host.Run();


