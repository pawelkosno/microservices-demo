// OrderService: creates orders.
//  - talks to ProductService synchronously (HTTP) to validate the product
//  - publishes an OrderCreated event asynchronously (RabbitMQ) for anyone interested
using System.Collections.Concurrent;
using Contracts;
using OrderService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("products", client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:ProductService"]!));
builder.Services.AddSingleton<EventPublisher>();

var app = builder.Build();

// In-memory "database" owned by this service only.
var orders = new ConcurrentDictionary<Guid, Order>();

app.MapGet("/orders", () => orders.Values);

app.MapGet("/orders/{id:guid}", (Guid id) =>
    orders.TryGetValue(id, out var order) ? Results.Ok(order) : Results.NotFound());

app.MapPost("/orders", async (CreateOrderRequest request, IHttpClientFactory httpFactory, EventPublisher publisher) =>
{
    // 1. Synchronous call to another service.
    var http = httpFactory.CreateClient("products");
    var response = await http.GetAsync($"/products/{request.ProductId}");
    if (!response.IsSuccessStatusCode)
        return Results.BadRequest($"Product {request.ProductId} not found.");

    var product = (await response.Content.ReadFromJsonAsync<ProductDto>())!;

    // 2. Save the order locally.
    var order = new Order(Guid.NewGuid(), product.Id, product.Name, request.Quantity, product.Price * request.Quantity);
    orders[order.Id] = order;

    // 3. Asynchronous notification - OrderService doesn't know or care who listens.
    await publisher.PublishAsync(Queues.OrderCreated,
        new OrderCreated(order.Id, order.ProductId, order.ProductName, order.Quantity, order.Total));

    return Results.Created($"/orders/{order.Id}", order);
});

app.Run();

record CreateOrderRequest(int ProductId, int Quantity);
record ProductDto(int Id, string Name, decimal Price);
record Order(Guid Id, int ProductId, string ProductName, int Quantity, decimal Total);
