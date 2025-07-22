using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<LoggingInterceptor>();
    options.Interceptors.Add<MetadataPropagator>();
});

builder.Services.AddGrpcReflection();

var app = builder.Build();

app.MapGrpcService<TaskOrchestrator.Services.OrchestratorService>();
app.MapGet("/", () => "OK");

//if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.Run();
