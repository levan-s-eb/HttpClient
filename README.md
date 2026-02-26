# HttpClient

A configuration-driven library for registering resilient HTTP service clients in .NET applications. It combines [Refit](https://github.com/reactiveui/refit) typed clients with [Polly](https://github.com/App-vNext/Polly) resilience pipelines and validates all settings at startup so misconfigurations are caught before traffic is served.

## Features

| Capability | Description |
|---|---|
| **Typed clients** | Define a Refit interface and the library wires up the `HttpClient` automatically. |
| **Concurrency limiting** | Caps in-flight requests per service to protect downstream systems and the caller. |
| **Outer timeout** | A total timeout that covers the full request lifecycle including retries. |
| **Retry** | Configurable retry policy with backoff strategy, jitter, per-attempt timeout, and a customisable list of retryable HTTP status codes. |
| **Circuit breaker** | Opens the circuit on sustained failure rates to shed load and allow recovery. Failure status codes are fully configurable. |

## Quick start

### 1. Define a Refit interface

```csharp
using Refit;

public interface IOrdersApi
{
    [Get("/api/orders/{id}")]
    Task<Order> GetOrderAsync(int id);
}
```

### 2. Register the client

```csharp
// Program.cs
builder.Services.AddServiceClient<IOrdersApi>("OrdersApi");
```

### 3. Add configuration

```jsonc
// appsettings.json
{
  "ServiceClients": {
    "OrdersApi": {
      "BaseUrl": "https://orders.internal.example.com",
      "TimeoutSeconds": 15,
      "ConcurrencyLimitOptions": {
        "Limit": 200,
        "QueueSize": 50
      },
      "Retry": {
        "Enabled": true,
        "MaxAttempts": 3,
        "BaseDelayMilliseconds": 500,
        "DelayBackoffType": "Exponential",
        "RetryableStatusCodes": [ 429, 502, 503 ],
        "TimeoutSecondsPerRetry": 5
      },
      "CircuitBreaker": {
        "Enabled": true,
        "FailureRatio": 0.25,
        "MinimumThroughput": 100,
        "SamplingDurationSeconds": 30,
        "BreakDurationSeconds": 30,
        "FailureStatusCodes": [ 429, 500, 502, 503 ]
      }
    }
  }
}
```

### 4. Inject and use

```csharp
public class OrderService(IOrdersApi ordersApi)
{
    public Task<Order> GetAsync(int id) => ordersApi.GetOrderAsync(id);
}
```

## Configuration reference

### `ServiceClientOptions`

Bind from `ServiceClients:{ServiceName}`.

| Property | Type | Default | Description |
|---|---|---|---|
| `BaseUrl` | `string` | *(required)* | Absolute URL with `http` or `https` scheme. |
| `TimeoutSeconds` | `int` | `30` | Outer timeout for the entire request (including retries). |
| `ConcurrencyLimitOptions` | object | see below | Concurrency limiter settings. |
| `Retry` | object | see below | Retry policy settings. |
| `CircuitBreaker` | object | see below | Circuit breaker settings. |

### `ConcurrencyLimitOptions`

Always active.

| Property | Type | Default | Constraint |
|---|---|---|---|
| `Limit` | `int` | `1000` | > 0 |
| `QueueSize` | `int` | `0` | >= 0 (0 disables queuing) |

### `RetryOptions`

Validated only when `Enabled` is `true`.

| Property | Type | Default | Constraint |
|---|---|---|---|
| `Enabled` | `bool` | `false` | -- |
| `MaxAttempts` | `int` | `3` | 1-10 inclusive |
| `BaseDelayMilliseconds` | `int` | `500` | > 0 |
| `DelayBackoffType` | `DelayBackoffType` | `Constant` | `Constant`, `Linear`, or `Exponential` |
| `RetryableStatusCodes` | `List<HttpStatusCode>` | `[429]` | Must not be empty |
| `TimeoutSecondsPerRetry` | `int` | `5` | > 0, must be less than `TimeoutSeconds` |

### `CircuitBreakerOptions`

Validated only when `Enabled` is `true`.

| Property | Type | Default | Constraint |
|---|---|---|---|
| `Enabled` | `bool` | `false` | -- |
| `FailureRatio` | `double` | `0.1` | Exclusively between 0 and 1 |
| `MinimumThroughput` | `int` | `100` | >= 10 |
| `SamplingDurationSeconds` | `int` | `30` | > 10 |
| `BreakDurationSeconds` | `int` | `30` | >= 5 |
| `FailureStatusCodes` | `List<HttpStatusCode>` | `[429, 500, 502, 503]` | Must not be empty |

## Resilience pipeline order

The Polly pipeline is constructed in a specific order for each registered client:

```
Request
  -> Concurrency Limiter
    -> Outer Timeout (TimeoutSeconds)
      -> Retry (if enabled)
        -> Circuit Breaker (if enabled)
          -> Per-Attempt Timeout (TimeoutSecondsPerRetry, only when retry is enabled)
            -> HttpClient -> Downstream Service
```

> **Important:** `TimeoutSecondsPerRetry` must be less than `TimeoutSeconds`. The outer timeout covers the entire pipeline including all retries, while the per-attempt timeout cancels individual attempts. If the per-attempt timeout is equal to or greater than the outer timeout, the outer timeout will always fire first, preventing retries from working as expected.

## Validation

All configuration is validated at startup via the [Options validation pattern](https://learn.microsoft.com/dotnet/core/extensions/options#options-validation). The library bridges FluentValidation into `IValidateOptions<T>` through `FluentValidateOptions<T>`, so the app will fail to start if any rule is violated.

Error messages include the configuration path to make troubleshooting straightforward:

```
[OrdersApi] BaseUrl: 'BaseUrl' is required. Set it in 'ServiceClients:<service>:BaseUrl' in appsettings
```

## Registering multiple services

Call `AddServiceClient` once per downstream service. Each registration gets its own named options instance and resilience pipeline:

```csharp
builder.Services
    .AddServiceClient<IOrdersApi>("OrdersApi")
    .AddServiceClient<IPaymentsApi>("PaymentsApi")
    .AddServiceClient<IInventoryApi>("InventoryApi");
```

```jsonc
{
  "ServiceClients": {
    "OrdersApi":    { "BaseUrl": "https://orders.internal.example.com"    },
    "PaymentsApi":  { "BaseUrl": "https://payments.internal.example.com"  },
    "InventoryApi": { "BaseUrl": "https://inventory.internal.example.com" }
  }
}
```

## Dependencies

| Package | Purpose |
|---|---|
| [Refit.HttpClientFactory](https://www.nuget.org/packages/Refit.HttpClientFactory) | Typed HTTP client generation from interfaces |
| [Microsoft.Extensions.Http.Resilience](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience) | Polly resilience pipelines for `IHttpClientBuilder` |
| [FluentValidation.DependencyInjectionExtensions](https://www.nuget.org/packages/FluentValidation.DependencyInjectionExtensions) | Declarative options validation |

## Running tests

```bash
dotnet test
```


