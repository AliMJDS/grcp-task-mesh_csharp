using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

public class LoggingInterceptor : Interceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;
    public LoggingInterceptor(ILogger<LoggingInterceptor> logger) => _logger = logger;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Unary call: {Method}", context.Method);
            var response = await continuation(request, context);
            return response;
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("Unary call {Method} took {Elapsed} ms", context.Method, sw.ElapsedMilliseconds);
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        _logger.LogInformation("Duplex stream started: {Method}", context.Method);
        await base.DuplexStreamingServerHandler(requestStream, responseStream, context, continuation);
    }
} 