<!--
  NOTE: Sections marked [FILL IN] need real content from the actual
  Benzene codebase — I don't have direct read access to your source,
  so these are structural placeholders, not invented API details.
-->

# Benzene

[![Build Status](https://github.com/daniellepelley/Benzene/actions/workflows/[WORKFLOW_FILE].yml/badge.svg)](https://github.com/daniellepelley/Benzene/actions)
[![NuGet](https://img.shields.io/nuget/v/Benzene.svg)](https://www.nuget.org/packages/Benzene/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

<!-- [FILL IN — one sentence: what problem does this solve that plain
ASP.NET Core middleware or a generic mediator library doesn't?] -->
**Benzene is a lightweight middleware pipeline for C# that makes it
easy to build hexagonal (ports-and-adapters) applications — the same
core logic runs behind HTTP, AWS Lambda, queues, or any other
adapter, without duplicating cross-cutting concerns like logging,
retries, or validation.**

## Why Benzene?

<!-- [FILL IN — 2-4 bullets on the actual pain point this solves.
Draft bullets below — replace with the real motivation] -->

- Write your application logic once, against a port interface — swap
  the adapter (HTTP, Lambda, SQS, etc.) without touching business logic
- Cross-cutting concerns (logging, retries, validation, auth) live in
  composable middleware, not scattered across handlers
- Minimal dependencies, designed for serverless cold-start performance

## Quickstart

```bash
dotnet add package Benzene
```

<!-- [FILL IN — a real, runnable 10-15 line example. This is the most
important part of the README. Someone should be able to copy this,
paste it into a new console app, and see it work in under a minute.
Suggested shape below: -->

```csharp
// [FILL IN with real Benzene API — example shape only]
var pipeline = new MiddlewarePipelineBuilder<MyContext>()
    .UseLogging()
    .UseRetry(maxRetries: 3)
    .Build(myPort);

var result = await pipeline.HandleAsync(context);
```

See [`examples/`](./examples) for complete, runnable sample projects.

## How it fits into hexagonal architecture

<!-- [FILL IN — short explanation, or a simple diagram, of how ports,
adapters, and the middleware pipeline relate in Benzene specifically] -->

## Documentation

Full documentation is available in [`docs/`](./docs), including:

- <!-- [FILL IN — list actual docs pages, e.g. "Getting started",
  "Writing custom middleware", "AWS Lambda adapter guide"] -->

## Installation

```bash
dotnet add package Benzene
```

Requires <!-- [FILL IN — target .NET version(s)] -->.

## Contributing

Contributions are welcome. Please see [CONTRIBUTING.md](./CONTRIBUTING.md)
<!-- create this file if it doesn't exist yet — see note below --> for:

- How to set up a local dev environment
- Coding conventions
- How to run the test suite (`dotnet test`)
- How to open a good pull request

Good first issues are labeled
[`good first issue`](https://github.com/daniellepelley/Benzene/labels/good%20first%20issue).
<!-- Create a few of these — they're one of the highest-converting
things for a project trying to attract first-time contributors -->

## License

MIT — see [LICENSE](./LICENSE) for details.
