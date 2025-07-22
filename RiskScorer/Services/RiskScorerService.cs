using Grpc.Core;
using System.Threading.Tasks;
using GrpcTaskMesh.Protos;

namespace RiskScorer.Services
{
    public class RiskScorerService : GrpcTaskMesh.Protos.RiskScorer.RiskScorerBase
    {
        public override async Task Score(
            IAsyncStreamReader<NlpResult> requestStream,
            IServerStreamWriter<RiskScore> responseStream,
            ServerCallContext context)
        {
            await foreach (var nlp in requestStream.ReadAllAsync())
            {
                float score = nlp.Sentiment == "negative" ? 0.9f : 0.1f;
                await responseStream.WriteAsync(new RiskScore
                {
                    TaskId = nlp.TaskId,
                    Score = score
                });
            }
        }
    }
} 