#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

orders_pid=""
payments_pid=""
aggregator_pid=""
cleanup() {
  kill "$orders_pid" "$payments_pid" "$aggregator_pid" 2>/dev/null || true
}
trap cleanup EXIT

# Poll a URL until it responds (HTTP 2xx), up to 60 tries. Returns non-zero on timeout.
wait_for() {
  local url="$1"
  for _ in $(seq 1 60); do
    if curl -sf "$url" >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  return 1
}

# Poll a URL until it STOPS responding, up to 60 tries. Returns non-zero if still up.
wait_until_down() {
  local url="$1"
  for _ in $(seq 1 60); do
    if ! curl -sf "$url" >/dev/null 2>&1; then
      return 0
    fi
    sleep 1
  done
  return 1
}

dotnet run --project Benzene.Examples.Mesh.OrdersService --urls http://localhost:5310 &
orders_pid="$!"
dotnet run --project Benzene.Examples.Mesh.PaymentsService --urls http://localhost:5311 &
payments_pid="$!"
dotnet run --project Benzene.Examples.Mesh.Aggregator --urls http://localhost:5300 &
aggregator_pid="$!"

echo "Waiting for services to start..."
# Poll /spec, not /healthcheck: payments is unhealthy by default and returns HTTP 503, which
# `curl -f` treats as failure. /spec returns 200 regardless of health.
wait_for "http://localhost:5310/spec?type=benzene" || true
wait_for "http://localhost:5311/spec?type=benzene" || true
wait_for "http://localhost:5300/mesh-ui" || true

echo "Running first mesh aggregation (baseline)..."
curl -sf -X POST http://localhost:5300/mesh/aggregate >/dev/null || true

echo "Restarting payments-api with a changed spec to demonstrate contract drift..."
kill "$payments_pid" 2>/dev/null || true
wait_until_down "http://localhost:5311/spec?type=benzene" || true
DEMO_ADD_ENDPOINT=true dotnet run --project Benzene.Examples.Mesh.PaymentsService --urls http://localhost:5311 &
payments_pid="$!"
wait_for "http://localhost:5311/spec?type=benzene" || true

echo "Generating meshed traffic (checkout calls payments-api with a propagated trace)..."
for i in 1 2 3 4 5; do
  curl -sf -X POST http://localhost:5310/invoke \
    -d "{\"topic\":\"orders:checkout\",\"headers\":{},\"body\":\"{\\\"orderId\\\":\\\"ord-$i\\\"}\"}" >/dev/null || true
done
curl -sf -X POST http://localhost:5310/invoke \
  -d '{"topic":"orders:get-all","headers":{},"body":"{}"}' >/dev/null || true

echo "Running second mesh aggregation (payments spec now differs -> drift)..."
curl -sf -X POST http://localhost:5300/mesh/aggregate >/dev/null || true

echo "Running mesh topology query (mocked Tempo/Prometheus service-graph data)..."
curl -sf -X POST http://localhost:5300/mesh/topology >/dev/null || true

cat <<'EOF'

Mesh Explorer:   http://localhost:5300/mesh-ui
Fleet view NEW:  http://localhost:5300/fleet-ui
Manifest JSON:   http://localhost:5300/artifacts/manifest.json
Topology JSON:   http://localhost:5300/artifacts/topology.json
Orders spec:     http://localhost:5310/spec?type=benzene
Payments spec:   http://localhost:5311/spec?type=benzene

The NEW Fleet view (http://localhost:5300/fleet-ui) is the live collector - everything on it
is derived from what the services themselves emit (docs/specification/mesh.md), nothing declared:
  - orders-api and payments-api registered their derived descriptors and heartbeat every 10s
    (payments-api heartbeats unhealthy -> "degraded"; restart it with DEMO_PAYMENTS_HEALTHY=true
    to watch it flip)
  - the payments:get topic shows "consumers: orders-api" - derived purely from the traceparent
    the checkout handler propagated, and the recent flows join both services' events
  - shipping-api has NO row: it never started, so it never registered - start it and watch it
    appear
  - payments-api's restart with DEMO_ADD_ENDPOINT=true changed its descriptor hash - the same
    contract-drift story the aggregation dashboard tells, from live data

The classic dashboard (http://localhost:5300/mesh-ui) shows every state at once:
  - orders-api    healthy    (Postgres/Redis/SQS checks, each with a dependency chip)
  - payments-api  unhealthy + drift (failed gateway, warning fraud-engine, ok database)
  - shipping-api  unreachable (nothing is listening on port 5312)

The dashboard's Topology table (mocked Tempo/Prometheus data) shows an orders-api -> shipping-api
edge with clean traffic even though shipping-api is currently unreachable - topology data is an
independent signal from live health, not a replacement for it.

shipping-api is intentionally NOT started. Start it yourself in another terminal to watch it
flip to healthy on the next aggregation run:

  cd examples/Mesh
  dotnet run --project Benzene.Examples.Mesh.ShippingService --urls http://localhost:5312
  curl -X POST http://localhost:5300/mesh/aggregate

To see payments-api go healthy, restart it with DEMO_PAYMENTS_HEALTHY=true and re-aggregate:

  DEMO_PAYMENTS_HEALTHY=true dotnet run --project Benzene.Examples.Mesh.PaymentsService --urls http://localhost:5311
  curl -X POST http://localhost:5300/mesh/aggregate

Press Ctrl+C to stop.
EOF

wait
