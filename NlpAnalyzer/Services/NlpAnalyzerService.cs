using Grpc.Core;
using System.Threading.Tasks;
using GrpcTaskMesh.Protos;
using System.Linq;
using System.Collections.Generic;

namespace NlpAnalyzer.Services
{
    public class NlpAnalyzerService : GrpcTaskMesh.Protos.NlpAnalyzer.NlpAnalyzerBase
    {
        public override async Task Analyze(
            IAsyncStreamReader<PreprocessedChunk> requestStream,
            IServerStreamWriter<NlpResult> responseStream,
            ServerCallContext context)
        {
            await foreach (var chunk in requestStream.ReadAllAsync())
            {
                // Dummy keyword extraction: pick unique words longer than 4 chars
                var keywords = chunk.Content
                    .Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 4)
                    .Distinct()
                    .Take(5)
                    .ToList();

                // Dummy sentiment: negative if "fail" or "error" present, else positive
                var sentiment = keywords.Any(k => k.Contains("fail") || k.Contains("error")) ? "negative" : "positive";

                await responseStream.WriteAsync(new NlpResult
                {
                    TaskId = chunk.TaskId,
                    Keywords = { keywords },
                    Sentiment = sentiment
                });
            }
        }
    }
} 