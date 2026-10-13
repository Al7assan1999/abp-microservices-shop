// Single entry point for clients: /catalog/** and /ordering/** are forwarded to the services.
// Routes and service addresses live in configuration (appsettings.json / environment variables).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("healthy"));
app.MapReverseProxy();

app.Run();
