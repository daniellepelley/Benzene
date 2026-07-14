# DynamoDB Streams Integration Plan

## Why

`work/aws-roadmap-1.0.md`'s medium-term "New Event Sources" list puts **DynamoDB Streams first**
(ahead of Kinesis, ALB, and AppSync). It is the standard AWS change-data-capture story: a table's
stream invokes a Lambda with batches of INSERT/MODIFY/REMOVE records, and those records are
events a Benzene service wants routed to handlers — event sourcing, projections, cache
invalidation, outbox patterns.

Scope: **inbound only** (DynamoDB Streams → Lambda → Benzene handlers), plus test helpers,
tests, and docs. There is no outbound client to build — you cause stream events by writing to
the table with the AWS SDK; the stream itself is read-only (decision DS7).

## Verified facts this plan relies on

- The Lambda event shape (verbatim from AWS docs): top-level `Records` array; each record has
  **camelCase** envelope keys (`eventID`, `eventName` = `INSERT|MODIFY|REMOVE`, `eventVersion`,
  `eventSource` = `"aws:dynamodb"`, `eventSourceARN`, `awsRegion`) and a `dynamodb` object with
  **PascalCase** keys (`Keys`, `NewImage`, `OldImage`, `SequenceNumber`, `SizeBytes`,
  `StreamViewType`, `ApproximateCreationDateTime`). Images/keys are in DynamoDB
  **AttributeValue JSON** (`{"Id": {"N": "101"}, "Message": {"S": "hi"}}`).
- `eventSourceARN` is a *stream* ARN whose resource segment contains slashes AND colons
  (`arn:aws:dynamodb:region:acct:table/Name/stream/2015-06-27T00:48:05.899` — the timestamp has
  colons), so table-name parsing must split the ARN on `':'` with a **max count of 6**, then on
  `'/'`.
- DynamoDB Streams event source mappings support `ReportBatchItemFailures`: the function returns
  `{"batchItemFailures": [{"itemIdentifier": "<SequenceNumber>"}]}` and Lambda **checkpoints at
  the lowest reported sequence number**, retrying from there. Records within a shard arrive in
  order, and one Lambda invocation receives records from a single shard.
- Batch adapter blueprint: `src/Benzene.Aws.Lambda.Sqs/` — `SqsApplication :
  IMiddlewareApplication<SQSEvent, SQSBatchResponse>` (scope per record, `ISetCurrentTransport`,
  failures collected from `context.IsSuccessful` or exceptions), `SqsLambdaHandler :
  AwsLambdaMiddlewareRouter<SQSEvent>` (`CanHandle` on `Records[0].EventSource`, `MapResponse`
  writes the batch response), getters + `IMessageHandlerResultSetter` setting `IsSuccessful`,
  `UseSqs`/`AddSqs`/`SqsRegistrations`.
- Own-POCO precedent: `Benzene.Aws.Lambda.EventBridge` models the envelope itself with
  `[JsonPropertyName]` + raw `JsonElement` payloads rather than depending on an
  `Amazon.Lambda.*Events` package. `AwsLambdaMiddlewareRouter` probes with
  `DefaultLambdaJsonSerializer`; explicit `[JsonPropertyName]` on every property makes the POCOs
  immune to its naming policy (needed here anyway because of the mixed camel/Pascal wire format).
- Topic-verbatim precedent: S3 routes on the record's `EventName` (`ObjectCreated:Put`) — `':'`
  in topic ids is established.

## ⚠️ FLAGS — approved by approving this plan

**Solution structure**: 2 new projects in `Benzene.sln` — `src/Benzene.Aws.Lambda.DynamoDb`
and `src/Benzene.Aws.Lambda.DynamoDb.TestHelpers` (nested under the AWS solution folder), plus
two `ProjectReference`s from `test/Benzene.Core.Test/Benzene.Test.csproj`.

**NuGet dependencies**: **none.** The event envelope is modeled as Benzene's own POCOs (DS1), so
the inbound package references only `Benzene.Aws.Lambda.Core` — no `Amazon.Lambda.DynamoDBEvents`,
no `AWSSDK.DynamoDBv2`.

## Design decisions (final)

- **DS1 — Own event model.** `DynamoDbEvent` / `DynamoDbStreamRecord` / `DynamoDbStreamData`
  POCOs with explicit `[JsonPropertyName]` matching the wire exactly (camelCase envelope,
  PascalCase `dynamodb` object); `Keys`/`NewImage`/`OldImage` kept as raw `JsonElement`.
  Rationale: same as EventBridge — the envelope is stable, and this keeps the package
  dependency-free. (`Amazon.Lambda.DynamoDBEvents` 2.x would drag in `AWSSDK.DynamoDBv2`.)
- **DS2 — Topic = `"{tableName}:{eventName}"`** (e.g. `orders:INSERT`), table name parsed from
  `eventSourceARN`. A handler declares `[Message("orders:INSERT")]`. This routes on the two
  things that identify a CDC event: which table and what happened. If the ARN is missing or
  unparseable, the topic falls back to the bare event name. `source`-style metadata (region,
  ARN) is headers, not topic.
- **DS3 — Body = plain JSON, unmarshalled from AttributeValue format.** Handlers receive
  ordinary POCOs, not `{"S": ...}` noise. `DynamoDbAttributeValueConverter` converts
  `S`→string, `N`→JSON number, `BOOL`→bool, `NULL`→null, `M`→object (recursive), `L`→array
  (recursive), `SS`/`BS`→string array, `NS`→number array, `B`→base64 string; an unknown
  descriptor is passed through raw rather than throwing (forward compatibility). Image
  precedence: `NewImage`, else `OldImage` (REMOVE), else `Keys` (KEYS_ONLY view) — "the most
  complete state available". Handlers needing old-vs-new diffing belong on a
  `NEW_AND_OLD_IMAGES` stream and can be added later via a context-typed middleware; out of
  scope now.
- **DS4 — Headers = `dynamodb-`-prefixed envelope metadata** (`event-name`, `event-id`,
  `table`, `sequence-number`, `stream-view-type`, `event-source-arn`, `aws-region`). **No
  `_benzeneHeaders` convention here** — unlike EventBridge, these events originate from table
  writes, not from a Benzene publisher, so there is no wire-header producer side.
- **DS5 — Sequential processing, stop at first failure.** This is the deliberate divergence
  from `SqsApplication`'s `Task.WhenAll`: stream records within a shard are *ordered* CDC, and
  processing them concurrently (or continuing past a failure) breaks the per-key ordering the
  stream guarantees. On the first failed record (unsuccessful result or exception), processing
  stops and its `SequenceNumber` is reported as the single `batchItemFailure` — Lambda
  checkpoints there and redelivers from that record. Success = empty failure list. Transport
  name: `"dynamodb"`.
- **DS6 — `CanHandle`**: `Records` non-empty and `Records[0].EventSource == "aws:dynamodb"` —
  same shape as SQS/SNS, ensures SQS/SNS/S3 events fall through to their own adapters.
- **DS7 — No outbound client.** Writing to the table *is* the publish operation and belongs to
  the AWS SDK / application data layer.
- **DS8 — TestHelpers marshal the other way.** `AsDynamoDb<T>()` builds a realistic
  `DynamoDbEvent` from an `IMessageBuilder<T>`: topic parsed as `"table:EVENT"` (bare table name
  defaults to `INSERT`), the message serialized then marshalled *into* AttributeValue JSON
  (string→`S`, number→`N`, bool→`BOOL`, null→`NULL`, object→`M`, array→`L`) and placed in
  `NewImage` (or `OldImage` for `REMOVE`), with a well-formed stream ARN, sequential
  `SequenceNumber`s, and `"aws:dynamodb"` source. Plus `SendDynamoDbAsync` host extensions
  mirroring `SendSqsAsync`.

## Work items

1. `src/Benzene.Aws.Lambda.DynamoDb/` (refs `Benzene.Aws.Lambda.Core` only, XML docs on):
   `DynamoDbEvent`/`DynamoDbStreamRecord`/`DynamoDbStreamData`, `DynamoDbAttributeValueConverter`,
   `DynamoDbUtils.GetTableName` (colon-safe ARN parse), `DynamoDbRecordContext`,
   `DynamoDbApplication` + `DynamoDbBatchResponse`, `DynamoDbLambdaHandler`, the four
   getters/setter, `UseDynamoDb`/`AddDynamoDb`/`DynamoDbRegistrations`, CLAUDE.md.
2. `src/Benzene.Aws.Lambda.DynamoDb.TestHelpers/`: `DynamoDbAttributeValueMarshaller`,
   `MessageBuilderExtensions.AsDynamoDb`, `BenzeneTestHostExtensions.SendDynamoDbAsync`.
3. `Benzene.sln` + `test/Benzene.Core.Test/Benzene.Test.csproj` registration.
4. Tests in `test/Benzene.Core.Test/Aws/DynamoDb/`: converter (every descriptor, nesting,
   unknown-descriptor pass-through, marshaller round-trip), getters (topic incl. colon-in-ARN
   parse, body precedence chain, headers), pipeline round-trip via `InlineAwsLambdaStartUp` +
   `AsDynamoDb` (success + unknown-topic failure reporting the sequence number), application
   ordering semantics (sequential, stop-at-first-failure, correct `itemIdentifier`), handler
   `CanHandle`/fall-through.
5. Docs: `docs/getting-started-aws.md` event-source bullet + section;
   `docs/specification/transport-bindings.md` catalog entry; CHANGELOG entry; roadmap tick in
   `work/aws-roadmap-1.0.md`.
