using Grpc.Core;
using System.Threading.Tasks;
using GrpcTaskMesh.Protos;
using System.Collections.Concurrent;

namespace Aggregator.Services
{
    public class AggregatorService : GrpcTaskMesh.Protos.Aggregator.AggregatorBase
    {
        public override async Task<FinalResult> Aggregate(
            IAsyncStreamReader<AggregationInput> requestStream,
            ServerCallContext context)
        {
            // Store partial results per task_id
            var state = new ConcurrentDictionary<string, (RiskScore? risk, ValidationResult? validation)>();
            string lastTaskId = null;
            await foreach (var input in requestStream.ReadAllAsync())
            {
                var taskId = input.TaskId;
                lastTaskId = taskId;
                state.TryGetValue(taskId, out var tuple);
                if (input.RiskScore != null)
                    tuple.risk = input.RiskScore;
                if (input.Validation != null)
                    tuple.validation = input.Validation;
                state[taskId] = tuple;
            }
            // After stream ends, emit a FinalResult for the last task (or aggregate as needed)
            if (lastTaskId != null && state.TryGetValue(lastTaskId, out var finalTuple) && finalTuple.risk != null && finalTuple.validation != null)
            {
                var decision = (finalTuple.risk.Score < 0.5f && finalTuple.validation.IsValid) ? "APPROVED" : "REJECTED";
                var notes = $"Risk: {finalTuple.risk.Score:0.00}, Valid: {finalTuple.validation.IsValid}";
                return new FinalResult
                {
                    TaskId = lastTaskId,
                    Decision = decision,
                    Notes = notes
                };
            }
            // If nothing to aggregate, return a default result
            return new FinalResult { TaskId = lastTaskId ?? "", Decision = "REJECTED", Notes = "No data" };
        }
    }
} 