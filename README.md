# GrpcTaskMesh

## Project Explanation

GrpcTaskMesh is a modular, distributed task processing system built with .NET 8 and gRPC. It demonstrates a microservices-style architecture where each service is responsible for a specific stage in a data processing pipeline. The system is designed for extensibility, observability, and clear separation of concerns.

### Purpose
- **Orchestration:** Coordinate complex, multi-step processing of tasks across independent services.
- **Demonstration:** Showcase gRPC streaming, service chaining, and middleware in .NET.
- **Extensibility:** Easily add or modify processing stages by updating or adding services.

### Architecture Overview
- **Client.Console:** Sends tasks to the orchestrator and receives final results.
- **TaskOrchestrator:** Central coordinator that streams tasks through the pipeline.
- **Preprocessor:** Prepares and normalizes input data.
- **NlpAnalyzer:** Performs NLP analysis on preprocessed data.
- **RiskScorer:** Assesses risk based on NLP results.
- **Validator:** Validates the NLP results.
- **Aggregator:** Combines risk and validation results into a final decision.
- **Middleware:** Provides logging and metadata propagation across services.
- **Shared:** Contains common models and the gRPC proto definitions.

### High-Level Flow
1. **Client submits tasks** to the orchestrator via a gRPC streaming call.
2. **Orchestrator streams tasks** to the Preprocessor service.
3. **Preprocessor outputs chunks** to the NlpAnalyzer.
4. **NlpAnalyzer results** are sent in parallel to RiskScorer and Validator.
5. **Risk and validation results** are merged and sent to the Aggregator.
6. **Aggregator produces a final result** for each task, which is streamed back to the client.

This architecture allows for scalable, observable, and maintainable distributed task processing, and serves as a reference for building gRPC-based microservices in .NET.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Solution Structure

- **Aggregator/**: Aggregates results from downstream services
- **AppHost/**: Host for Aspire/AspNet, launches all microservices together
- **Client.Console/**: Console client to interact with the orchestrator
- **Middleware/**: gRPC middleware (logging, metadata)
- **NlpAnalyzer/**: NLP analysis service
- **Preprocessor/**: Preprocessing service
- **RiskScorer/**: Risk scoring service
- **Shared/**: Shared models and proto definitions
- **TaskOrchestrator/**: Main orchestrator service
- **Validator/**: Validation service

## Getting Started

### 1. Clone the repository

```
git clone <your-repo-url>
cd mesh-proto/GrpcTaskMesh
```

### 2. Build the solution

```
dotnet build GrpcTaskMesh.sln
```

### 3. Run all services using AppHost

AppHost will launch all microservices together using Aspire. In your terminal:

```
dotnet run --project AppHost
```

This will start all required services (Aggregator, NlpAnalyzer, Preprocessor, RiskScorer, TaskOrchestrator, Validator) in a coordinated way.

### 4. Run the client

After AppHost has started all services, open a new terminal and run:

```
dotnet run --project Client.Console
```

You should see output for each task processed by the orchestrator pipeline.

## Development Notes

- All gRPC services use HTTP/2 and run on localhost with different ports (see each service's `appsettings.json`).
- For local development, the client accepts self-signed certificates.
- Server reflection is enabled for the orchestrator in development mode for easier debugging with tools like `grpcurl`.

## Troubleshooting

- Ensure AppHost is running and all services are healthy before starting the client.
- If you see certificate errors, make sure you are using the latest .NET SDK and the client is configured to accept self-signed certs (already set up).
- Check each service's logs for errors if something isn't working.

## License

MIT (or your license here) 
