# Azure Order Processing (Sample)

An end-to-end event-driven sample using Azure Functions, Service Bus, and API Management concepts.

## Architecture (high level)

Client -> API (HTTP Function) -> Service Bus -> Processor (ServiceBus Function) -> Storage/Logs

## Components
- Order API (Azure Function - HTTP Trigger)
- Order Processor (Azure Function - Service Bus Trigger)
- Infrastructure as Code (Terraform)
- CI/CD (Azure DevOps YAML)
- Observability (Application Insights)

## Status
Work in progress.
