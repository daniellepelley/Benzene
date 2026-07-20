# Benzene.HealthChecks.DynamoDb

## What this package does
A single `IHealthCheck` (`DynamoDbHealthCheck`) that verifies a DynamoDB table is reachable with a
read-only `DescribeTable` call (metadata only, non-destructive). A dedicated check-only package (there
is no outbound `Benzene.Clients.Aws.DynamoDb` client package) referencing `AWSSDK.DynamoDBv2` +
`Benzene.HealthChecks.Core`.

## Key types
- `DynamoDbHealthCheck` - `DescribeTableAsync(tableName)`; healthy on a 200, unhealthy on any error
  (reports the exception **type name**, never its message). `Type => "DynamoDb"`; `Data` =
  TableName/TableStatus (+ Error on failure); `Dependencies` = one `HealthCheckDependency("Table", tableName)`.
- `DynamoDbHealthCheckFactory` - builds the check for a fixed table, resolving `IAmazonDynamoDB` from DI.
- `Extensions.AddDynamoDbHealthCheck(builder, tableName)` - registration helper.

## Conventions
- `IAmazonDynamoDB` is resolved from DI; the **consumer** registers it (Benzene does not register AWS
  SDK clients).
- No independent timeout - relies on the aggregator's `TimeOutHealthCheck` wrapper.
