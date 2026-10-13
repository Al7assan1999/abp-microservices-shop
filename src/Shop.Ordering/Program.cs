using Shop.Ordering;

// ABP stores DateTime values without a UTC kind; keep Npgsql's pre-6.0 timestamp handling (as ABP's own templates do).
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();
await builder.AddApplicationAsync<OrderingModule>();

var app = builder.Build();
await app.InitializeApplicationAsync();
app.MapGet("/health", () => Results.Ok("healthy"));
await app.RunAsync();
