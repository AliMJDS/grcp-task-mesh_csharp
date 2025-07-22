using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<LoggingInterceptor>();
    options.Interceptors.Add<MetadataPropagator>();
});

var app = builder.Build();

app.MapGrpcService<Aggregator.Services.AggregatorService>();
app.MapGet("/", () => "OK");

app.Run();
