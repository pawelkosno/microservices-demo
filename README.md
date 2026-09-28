# Microservices Demo (.NET 10)

A minimal online-shop backend split into small, independent services.
The goal is to show **how microservices fit together**, not to build a real shop.

## Architecture

```
                  ┌──────────────┐
  Client ───────► │   Gateway    │  :5000  (single entry point, YARP reverse proxy)
                  └──────┬───────┘
             /products   │   /orders
          ┌──────────────┴──────────────┐
          ▼                             ▼
 ┌─────────────────┐   HTTP     ┌─────────────────┐
 │ ProductService  │ ◄───────── │  OrderService   │
 │  (catalog)      │  "does     │  (orders)       │
 └─────────────────┘  product   └────────┬────────┘
                      exist?"            │ publishes "OrderCreated"
                                         ▼
                                 ┌───────────────┐
                                 │   RabbitMQ    │  (message broker)
                                 └───────┬───────┘
                                         │ consumes
                                         ▼
                              ┌──────────────────────┐
                              │ NotificationService  │  (logs a "notification")
                              └──────────────────────┘
```

## The pieces

| Project | Role |
|---|---|
| **Gateway** | The only public door. Routes `/products/*` and `/orders/*` to the right service. Clients never talk to services directly. |
| **ProductService** | Owns the product catalog. |
| **OrderService** | Creates orders. Asks ProductService if the product exists (sync HTTP), then announces the new order via RabbitMQ (async event). |
| **NotificationService** | Background worker with no API. Reacts to `OrderCreated` events. |
| **Contracts** | The shared event definition (`OrderCreated`) — the only code services share. |

## Key ideas it demonstrates

- **Each service is independent** – its own process, its own container, its own data (in-memory here).
- **API Gateway** – one entry point hiding the internal layout.
- **Synchronous communication** – HTTP call when you need an answer *now* (Order → Product).
- **Asynchronous communication** – events through a broker when you just want to say "this happened". OrderService doesn't know who listens; you could add more listeners without touching it.
- **Configuration per environment** – the same code runs locally (`localhost`) or in Docker (service names) by overriding config via environment variables.

## Run it

**Option A – everything in Docker** (easiest)

```bash
docker compose up --build
```

**Option B – services locally, broker in Docker** (good for debugging)

```bash
docker compose up rabbitmq
dotnet run --project src/ProductService
dotnet run --project src/OrderService
dotnet run --project src/NotificationService
dotnet run --project src/Gateway
```

## Try it

Use `requests.http`, or:

```bash
curl http://localhost:5000/products
curl -X POST http://localhost:5000/orders -H "Content-Type: application/json" -d '{"productId":1,"quantity":2}'
curl http://localhost:5000/orders
```

After placing an order, check the **NotificationService** logs – you'll see the notification it received.
RabbitMQ's UI is at http://localhost:15672 (guest / guest).

## What a real system would add

Real databases per service, retries/circuit breakers (Polly), health checks, auth at the gateway,
centralized logging/tracing (OpenTelemetry), and possibly .NET Aspire for local orchestration.
