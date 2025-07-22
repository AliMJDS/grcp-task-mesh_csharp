using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Threading.Tasks;

public class MetadataPropagator : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        // Example: read incoming metadata and attach to context Items for downstream services
        var traceId = context.RequestHeaders.GetValue("x-trace-id") ?? System.Guid.NewGuid().ToString();
        context.UserState["trace-id"] = traceId;
        return await continuation(request, context);
    }
    // For streaming handlers, similar logic can be applied.
} 