# Azure Order Processing (Sample)

An end-to-end event-driven sample built with **Azure Functions (.NET 8 isolated)** and **Azure Service Bus**.  
The goal of this repo is to practice common integration patterns used in Azure-based backends: HTTP ingestion, asynchronous messaging, retries/DLQ, IaC with Terraform, and CI/CD with YAML.

---

## Architecture (high level)

**Create Order (ingestion):**
Client → HTTP Function (Order API) → Service Bus Queue (`orders`) → Processor Function

**Processing:**
Service Bus Trigger Function → business logic (placeholder) → logs/telemetry (future)

---

## What’s in this repo

### 1) Order API (Azure Functions - HTTP Trigger, .NET 8 isolated)
- Accepts `POST /api/orders`
- Parses JSON request body
- Adds basic validation and correlation id
- Demonstrates **idempotency** using an in-memory store:
  - If the same `Idempotency-Key` is sent again, returns the cached response
- Returns `202 Accepted` as “accepted for processing” (async pattern)

> Note: idempotency store is **in-memory** for learning purposes. It resets on app restart.

### 2) Order Processor (Azure Functions - Service Bus Trigger, .NET 8 isolated)
- Consumes messages from Service Bus queue `orders`
- Demonstrates Service Bus processing behaviors:
  - retry (delivery count)
  - DLQ (dead-lettering after max delivery count)
  - basic error handling pattern

### 3) Infrastructure as Code (Terraform)
Terraform code is located at:
- `Infra/Terraform`

Provisioned resources:
- Azure Resource Group
- Azure Service Bus Namespace (Basic)
- Azure Service Bus Queue (`orders`)
- **Least-privilege authorization rules**:
  - Publisher: `Send` only
  - Consumer: `Listen` only

Important repo hygiene:
- `.terraform/` and `*.tfstate*` are **ignored** (never committed)
- `.terraform.lock.hcl` **is committed** for reproducible provider versions

### 4) CI (YAML) with GitHub Actions
Even though the job description referenced Azure DevOps, this repo uses GitHub Actions (same concepts: YAML pipeline, jobs, steps, hosted runners).

Implemented CI:
- Build & test on pushes/PRs to `main` (.NET 8)
- Terraform checks:
  - `terraform fmt -check`
  - `terraform init`
  - `terraform validate`

### 5) Azure authentication for CI (OIDC, secretless)
Terraform planning against Azure is authenticated using **OIDC / Workload Identity Federation**:
- No client secret stored in the repo
- GitHub Actions obtains an OIDC token at runtime
- Azure trusts the workflow identity (federated credential)
- Terraform uses `ARM_*` environment variables with OIDC to access Azure

---

## Local development

### Prerequisites
- Windows
- Visual Studio 2022
- .NET 8 SDK
- Azure Functions Core Tools (optional; VS can run functions without it)
- An Azure Service Bus namespace/queue (provisioned via Terraform in this repo)

### Run locally
1. Start `OrderApi.Functions`
2. Start `OrderProcessor.Functions`
3. Ensure local settings contain a Service Bus connection string:
   - `OrderApi.Functions/local.settings.json`: **publisher** connection string
   - `OrderProcessor.Functions/local.settings.json`: **consumer** connection string

> `local.settings.json` is intentionally ignored (contains secrets).

---

## Testing the API

### Endpoint
- `POST http://localhost:7071/api/orders`

### Sample request
```json
{
  "orderId": "123",
  "customerId": "C1",
  "amount": 100
}
