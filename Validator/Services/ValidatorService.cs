using Grpc.Core;
using System.Threading.Tasks;
using GrpcTaskMesh.Protos;

namespace Validator.Services
{
    public class ValidatorService : GrpcTaskMesh.Protos.Validator.ValidatorBase
    {
        public override async Task Validate(
            IAsyncStreamReader<NlpResult> requestStream,
            IServerStreamWriter<ValidationResult> responseStream,
            ServerCallContext context)
        {
            await foreach (var nlp in requestStream.ReadAllAsync())
            {
                bool isValid = nlp.Sentiment != "negative";
                await responseStream.WriteAsync(new ValidationResult
                {
                    TaskId = nlp.TaskId,
                    IsValid = isValid
                });
            }
        }
    }
} 