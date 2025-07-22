using Grpc.Core;
using System.Threading.Tasks;
using GrpcTaskMesh.Protos;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Preprocessor.Services
{
    public class PreprocessorService : GrpcTaskMesh.Protos.Preprocessor.PreprocessorBase
    {
        public override async Task Process(
            IAsyncStreamReader<TaskInput> requestStream,
            IServerStreamWriter<PreprocessedChunk> responseStream,
            ServerCallContext context)
        {
            await foreach (var input in requestStream.ReadAllAsync())
            {
                // Lowercase and remove punctuation
                var cleaned = Regex.Replace(input.Payload.ToLowerInvariant(), @"[\p{P}-[.]]+", "");
                // Split into chunks (e.g., by sentence or every 20 words)
                var words = cleaned.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                int chunkSize = 20;
                for (int i = 0; i < words.Length; i += chunkSize)
                {
                    var chunk = string.Join(' ', words, i, System.Math.Min(chunkSize, words.Length - i));
                    await responseStream.WriteAsync(new PreprocessedChunk
                    {
                        TaskId = input.TaskId,
                        Content = chunk
                    });
                }
            }
        }
    }
} 