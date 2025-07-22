using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using GrpcTaskMesh.Protos;

class Program
{
    static async Task Main()
    {
        var httpHandler = new System.Net.Http.HttpClientHandler();
        httpHandler.ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        using var channel = Grpc.Net.Client.GrpcChannel.ForAddress("https://localhost:5001", new Grpc.Net.Client.GrpcChannelOptions { HttpHandler = httpHandler });
        var client = new TaskOrchestrator.TaskOrchestratorClient(channel);

        using var call = client.StreamTasks();

        // Writer task
        _ = Task.Run(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                await call.RequestStream.WriteAsync(new TaskInput
                {
                    TaskId = Guid.NewGuid().ToString(),
                    Payload = $"Sample payload {i}"
                });
                await Task.Delay(100);
            }
            await call.RequestStream.CompleteAsync();
        });

        // Reader loop
        while (await call.ResponseStream.MoveNext(System.Threading.CancellationToken.None))
        {
            var result = call.ResponseStream.Current;
            Console.WriteLine($"[{result.TaskId}] Decision: {result.Decision} | Notes: {result.Notes}");
        }
    }
}
