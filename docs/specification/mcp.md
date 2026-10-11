# MCP Binding

**Module — Provisional: .NET** (`Benzene.Mcp`). A transport binding that satisfies
[transport-bindings.md](transport-bindings.md) §1, so that AI clients — assistants, IDEs, agents —
can call a service's handlers as MCP **tools**, over JSON-RPC 2.0 on Streamable HTTP (hosted by any
HTTP binding, default path `/benzene/mcp`) or on stdio (hosted by a worker).

## 1. Tools, not topics

The binding advertises a curated list of tools the application declares. It MUST NOT advertise every
topic as a tool: a hundred schemas in front of a model is a worse interface than none. A tool is
either **topic-bound** — a name mapped to a topic, whose arguments object is the request body — or
**custom**, a body the application wrote, which asks the pipeline through the same dispatcher.

A start-up check MUST fail when a topic-bound tool names a topic no handler answers.

## 2. The mapping

| | Rule |
|---|---|
| Topic | The tool's declared topic, plus an optional version sent as `benzene-version` |
| Headers | `benzene-mcp-tool` (the tool name) and `benzene-mcp-session` (the session id, where the transport has one) — [wire-contracts.md](wire-contracts.md) §2. There is no native metadata channel for the caller to set headers on |
| Body | The `tools/call` `arguments` object, verbatim. Arguments are read strictly; a value the model got wrong is refused with a sentence naming what was expected |
| Scope | One pipeline invocation and one scope per tool call; a custom tool that asks the pipeline twice makes two. Cancellation: the HTTP request's abort signal; stdio has none |

## 3. Results

- A success is a tool result whose text is the response body.
- A failure (`isSuccessful: false`) MUST be a tool result marked `isError`, whose text is
  `"{status}: {detail}"` with the problem document's `detail` ([wire-contracts.md](wire-contracts.md)
  §1.3) — something the model reads. It MUST NOT be a JSON-RPC error, which a model is not shown.
- JSON-RPC errors are reserved for the protocol itself (parse error, invalid request or params,
  unknown method) and for an unexpected exception, which carries a short reference and never the
  detail.

## 4. Sessions and annotations

- `Mcp-Session-Id` is issued on `initialize` and echoed on later requests. The binding keeps no
  session state; the id is passed to tools, and is the application's to interpret.
- `readOnlyHint` and `idempotentHint` are the inverse of the tool's declared `writes`, and
  `destructiveHint` is its declared `destructive`, so a client can gate the writers.
- Not offered: the server-to-client stream (`GET` → `405`), resources, prompts.
