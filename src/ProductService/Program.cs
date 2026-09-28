// ProductService: owns the product catalog. Other services ask it about products over HTTP.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory "database" - each microservice owns its own data.
var products = new List<Product>
{
    new(1, "Keyboard", 49.99m),
    new(2, "Mouse", 19.99m),
    new(3, "Monitor", 199.00m),
};

app.MapGet("/products", () => products);

app.MapGet("/products/{id:int}", (int id) =>
    products.FirstOrDefault(p => p.Id == id) is { } product
        ? Results.Ok(product)
        : Results.NotFound());

app.Run();

record Product(int Id, string Name, decimal Price);
