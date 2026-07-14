#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

pids=()
cleanup() {
  kill "${pids[@]}" 2>/dev/null || true
}
trap cleanup EXIT

dotnet run --project Benzene.Examples.Mesh.OrdersService --urls http://localhost:5310 &
pids+=("$!")
dotnet run --project Benzene.Examples.Mesh.PaymentsService --urls http://localhost:5311 &
pids+=("$!")
dotnet run --project Benzene.Examples.Mesh.Aggregator --urls http://localhost:5300 &
pids+=("$!")

echo "Waiting for services to start..."
for i in $(seq 1 60); do
  if curl -sf http://localhost:5300/mesh-ui >/dev/null 2>&1 \
    && curl -sf http://localhost:5310/healthcheck >/dev/null 2>&1 \
    && curl -sf http://localhost:5311/healthcheck >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

echo "Running first mesh aggregation..."
curl -sf -X POST http://localhost:5300/mesh/aggregate >/dev/null

cat <<'EOF'

Mesh Explorer:   http://localhost:5300/mesh-ui
Manifest JSON:   http://localhost:5300/artifacts/manifest.json
Orders spec:     http://localhost:5310/spec?type=benzene
Payments spec:   http://localhost:5311/spec?type=benzene

shipping-api is intentionally NOT started, so the dashboard shows it as
"unreachable". Start it yourself in another terminal to watch it flip to
healthy on the next aggregation run:

  cd examples/Mesh
  dotnet run --project Benzene.Examples.Mesh.ShippingService --urls http://localhost:5312
  curl -X POST http://localhost:5300/mesh/aggregate

See README.md for how to trigger the unhealthy and contract-drift states too.

Press Ctrl+C to stop.
EOF

wait
