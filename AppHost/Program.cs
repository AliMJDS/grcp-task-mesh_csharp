using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Add all microservices as projects
var nlpAnalyzer = builder.AddProject<Projects.NlpAnalyzer>("nlp-analyzer");
var riskScorer = builder.AddProject<Projects.RiskScorer>("risk-scorer");
var validator = builder.AddProject<Projects.Validator>("validator");
var aggregator = builder.AddProject<Projects.Aggregator>("aggregator");
var preprocessor = builder.AddProject<Projects.Preprocessor>("preprocessor");
var orchestrator = builder.AddProject<Projects.TaskOrchestrator>("orchestrator");

builder.Build().Run();
