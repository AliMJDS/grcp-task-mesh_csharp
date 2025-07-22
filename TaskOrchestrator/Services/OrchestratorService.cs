using Grpc.Core;
using System.Threading.Tasks;
using GrpcTaskMesh.Protos;
using System.Collections.Generic;
using System.Linq;
using Grpc.Net.Client;

namespace TaskOrchestrator.Services
{
    public class OrchestratorService : GrpcTaskMesh.Protos.TaskOrchestrator.TaskOrchestratorBase
    {
        private readonly Preprocessor.PreprocessorClient _preprocessor;
        private readonly NlpAnalyzer.NlpAnalyzerClient _nlp;
        private readonly RiskScorer.RiskScorerClient _risk;
        private readonly Validator.ValidatorClient _validator;
        private readonly Aggregator.AggregatorClient _aggregator;

        public OrchestratorService()
        {
            // Use correct ports for each downstream service
            _preprocessor = new Preprocessor.PreprocessorClient(GrpcChannel.ForAddress("https://localhost:5002"));
            _nlp = new NlpAnalyzer.NlpAnalyzerClient(GrpcChannel.ForAddress("https://localhost:5003"));
            _risk = new RiskScorer.RiskScorerClient(GrpcChannel.ForAddress("https://localhost:5004"));
            _validator = new Validator.ValidatorClient(GrpcChannel.ForAddress("https://localhost:5005"));
            _aggregator = new Aggregator.AggregatorClient(GrpcChannel.ForAddress("https://localhost:5006"));
        }

        public override async Task StreamTasks(
            IAsyncStreamReader<TaskInput> requestStream,
            IServerStreamWriter<FinalResult> responseStream,
            ServerCallContext context)
        {
            // 1. Pipe TaskInput to Preprocessor
            using var preprocCall = _preprocessor.Process();
            var preprocWriter = Task.Run(async () =>
            {
                await foreach (var input in requestStream.ReadAllAsync())
                {
                    await preprocCall.RequestStream.WriteAsync(input);
                }
                await preprocCall.RequestStream.CompleteAsync();
            });

            // 2. Pipe PreprocessedChunk to NlpAnalyzer
            using var nlpCall = _nlp.Analyze();
            var nlpWriter = Task.Run(async () =>
            {
                await foreach (var chunk in preprocCall.ResponseStream.ReadAllAsync())
                {
                    await nlpCall.RequestStream.WriteAsync(chunk);
                }
                await nlpCall.RequestStream.CompleteAsync();
            });

            // 3. Fan-out NlpResult to RiskScorer and Validator
            using var riskCall = _risk.Score();
            using var validCall = _validator.Validate();
            var fanoutWriter = Task.Run(async () =>
            {
                await foreach (var nlp in nlpCall.ResponseStream.ReadAllAsync())
                {
                    await Task.WhenAll(
                        riskCall.RequestStream.WriteAsync(nlp),
                        validCall.RequestStream.WriteAsync(nlp)
                    );
                }
                await Task.WhenAll(
                    riskCall.RequestStream.CompleteAsync(),
                    validCall.RequestStream.CompleteAsync()
                );
            });

            // 4. Read RiskScore and ValidationResult, send to Aggregator
            using var aggCall = _aggregator.Aggregate();
            var aggWriter = Task.Run(async () =>
            {
                var riskScores = ReadAllAsyncWithTaskId(riskCall.ResponseStream);
                var validations = ReadAllAsyncWithTaskId(validCall.ResponseStream);
                await foreach (var (type, msg) in MergeStreams(riskScores, validations))
                {
                    string taskId = type == "risk" ? ((RiskScore)msg).TaskId : ((ValidationResult)msg).TaskId;
                    var input = new AggregationInput { TaskId = taskId };
                    if (type == "risk") input.RiskScore = (RiskScore)msg;
                    else input.Validation = (ValidationResult)msg;
                    await aggCall.RequestStream.WriteAsync(input);
                }
                await aggCall.RequestStream.CompleteAsync();
            });

            // 5. Read FinalResult from Aggregator and stream to client
            var finalResult = await aggCall.ResponseAsync;
            await responseStream.WriteAsync(finalResult);

            await Task.WhenAll(preprocWriter, nlpWriter, fanoutWriter, aggWriter);
        }

        // Helper: merge two streams by task_id
        private async IAsyncEnumerable<(string, object)> MergeStreams(
            IAsyncEnumerable<(string, object)> a,
            IAsyncEnumerable<(string, object)> b)
        {
            var aEnum = a.GetAsyncEnumerator();
            var bEnum = b.GetAsyncEnumerator();
            try
            {
                while (await aEnum.MoveNextAsync())
                    yield return aEnum.Current;
                while (await bEnum.MoveNextAsync())
                    yield return bEnum.Current;
            }
            finally
            {
                await aEnum.DisposeAsync();
                await bEnum.DisposeAsync();
            }
        }

        // Helper: wrap stream with type label
        private async IAsyncEnumerable<(string, object)> ReadAllAsyncWithTaskId<T>(IAsyncStreamReader<T> stream) where T : class
        {
            await foreach (var msg in stream.ReadAllAsync())
            {
                if (msg is RiskScore) yield return ("risk", msg);
                else if (msg is ValidationResult) yield return ("valid", msg);
            }
        }
    }
} 