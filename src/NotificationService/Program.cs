// NotificationService: no HTTP API at all - it only reacts to events from the message broker.
using NotificationService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Build().Run();
