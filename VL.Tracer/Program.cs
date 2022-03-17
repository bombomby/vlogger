using System.Collections;
using VL.Tracer.Services;
using VL.Transport;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<ETWSession>();
builder.Services.AddSingleton<ConnectionManager>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.WebHost.UseKestrel().UseUrls($"http://localhost:{ConnectionSettings.Port}", $"https://localhost:{ConnectionSettings.Port+1}");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ETWService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.Services.GetService<ConnectionManager>()?.StartServer();

app.Run();
