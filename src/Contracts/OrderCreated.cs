namespace Contracts;

// Event published by OrderService and consumed by NotificationService.
// Shared "contract" = the only thing both services need to agree on.
public record OrderCreated(Guid OrderId, int ProductId, string ProductName, int Quantity, decimal Total);

public static class Queues
{
    public const string OrderCreated = "order-created";
}
