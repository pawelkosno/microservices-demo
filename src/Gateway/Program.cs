// Gateway: the single entry point for clients. It just forwards requests to the right service
// (routes are defined in appsettings.json under "ReverseProxy").
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.MapReverseProxy();
app.Run();
