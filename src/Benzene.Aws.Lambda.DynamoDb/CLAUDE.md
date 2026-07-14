# Benzene.Aws.Lambda.DynamoDb

## What this package does
Inbound DynamoDB Streams adapter: routes change-data-capture records delivered to a Lambda
function into Benzene message handlers. The topic is `"{tableName}:{eventName}"` (e.g.
`orders:INSERT`) and the body is the record's image unmarshalled from DynamoDB AttributeValue
format into plain JSON, so handlers receive ordinary POCOs. See
`docs/plans/dynamodb-streams-plan.md` for the design decisions (DS1–DS8).

## Key types/interfaces
- `DynamoDbEvent` / `DynamoDbStreamRecord` / `DynamoDbStreamData` — Benzene's own model of the
  stream batch (camelCase envelope, PascalCase `dynamodb` object, both pinned with
  `[JsonPropertyName]`; images kept as raw `JsonElement`). Deliberately not a dependency on
  `Amazon.Lambda.DynamoDBEvents` — keeps the package dependency-free beyond
  `Benzene.Aws.Lambda.Core`.
- `DynamoDbAttributeValueConverter` — unmarshals AttributeValue JSON (`{"Id":{"N":"101"}}`) to
  plain JSON (`{"Id":101}`); unknown descriptors pass through raw instead of throwing.
- `DynamoDbApplication : IMiddlewareApplication<DynamoDbEvent, DynamoDbBatchResponse>` —
  **sequential, stop-at-first-failure** batch processing. This deliberately diverges from
  `SqsApplication`'s `Task.WhenAll`: stream records within a shard are ordered CDC, so
  concurrent processing (or continuing past a failure) breaks per-key ordering. The first failed
  record's `SequenceNumber` is reported as the single `batchItemFailure`; Lambda checkpoints
  there and redelivers from that record (`ReportBatchItemFailures`).
- `DynamoDbLambdaHandler : AwsLambdaMiddlewareRouter<DynamoDbEvent>` — claims payloads whose
  first record has `eventSource == "aws:dynamodb"`; everything else falls through.
- Getters: topic = table (parsed from the stream ARN — split `':'` max 6, the stream timestamp
  contains colons) + `:` + event name; body = `NewImage` → `OldImage` → `Keys` precedence
  ("most complete state available"); headers = `dynamodb-`-prefixed envelope metadata
  (event-name, event-id, table, sequence-number, stream-view-type, event-source-arn,
  aws-region). No `_benzeneHeaders` convention — these events originate from table writes, not
  a Benzene publisher.
- `UseDynamoDb(action)` / `AddDynamoDb()` / `DynamoDbRegistrations` — standard adapter wiring,
  mirrors `Benzene.Aws.Lambda.Sqs`.

## When to use this package
- Handling table change streams (event sourcing, projections, cache invalidation, outbox) with
  `[Message("table:INSERT")]` / `[Message("table:MODIFY")]` / `[Message("table:REMOVE")]`
  handlers, alongside the other `UseXxx` event sources in one Lambda.

## Dependencies on other Benzene packages
- **Benzene.Aws.Lambda.Core** — `AwsLambdaMiddlewareRouter`, `AwsEventStreamContext`

## Important conventions
- Transport name: `"dynamodb"`.
- There is no outbound client — writing to the table *is* the publish operation (AWS SDK /
  application data layer); the stream is read-only.
- `Benzene.Aws.Lambda.DynamoDb.TestHelpers` has the reverse marshaller
  (`DynamoDbAttributeValueMarshaller`) and `AsDynamoDb()` (topic parsed as `"table:EVENT"`,
  bare table defaults to `INSERT`).
