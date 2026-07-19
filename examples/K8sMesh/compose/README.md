# Local Mesh demo — Docker Compose (no cloud)

The fastest way to see Benzene's service-mesh self-discovery working: three real Benzene Cloud
Services plus the mesh dashboard, all on your machine, no AWS/Azure/Kubernetes required.

```bash
docker compose -f examples/K8sMesh/compose/docker-compose.yml up --build
```

Then open **<http://localhost:8090/mesh-ui>**.

You'll see `orders`, `payments`, and `shipping` discovered and **healthy**, plus the cross-service
**Topics** catalog (`order:create`, `payment:take`, `shipment:book`) with the service that handles
each. The mesh re-polls every 15s.

## What's in the box

| Container | Image | What it is |
|---|---|---|
| `orders` / `payments` / `shipping` | `examples/K8sMesh/Service` | one Benzene Cloud Service image, run three times — `MESH_SERVICE` selects the domain. Each serves the Cloud Service Profile over HTTP: `/benzene/spec`, `/benzene/health`, `/benzene/invoke`, `/benzene/spec-ui`. |
| `mesh` | `deploy/Mesh/Benzene.Mesh.Host` | the config-driven mesh aggregator + UI. Polls each service listed in [`mesh.json`](./mesh.json) on a timer, writes `manifest.json` / `services/*.json` / `topics.json` / `topology.json`, and serves the Mesh UI. |

This is the same `Benzene.Mesh.Host` you'd run in your own `docker-compose.yml` alongside your real
services — here it's just pointed at the three demo services via a static `mesh.json` instead of a
live Kubernetes/AWS discovery provider (the `examples/K8sMesh` and `examples/AwsMesh` deploys show
those). See [`deploy/Mesh/README.md`](../../../deploy/Mesh/README.md) for the full config shape.

## Useful URLs

- Mesh UI — <http://localhost:8090/mesh-ui>
- Raw catalog artifacts — <http://localhost:8090/artifacts/manifest.json>, `/artifacts/topics.json`,
  `/artifacts/services/orders.json`
- Force a discovery pass immediately — `curl -XPOST http://localhost:8090/mesh/refresh`

## Notes

- Both images build from `mcr.microsoft.com/dotnet` base images; the build **context is the repo
  root** (`../../..`) because the projects reference `src/` directly — that's why the compose file
  sets `context: ../../..` with an explicit `dockerfile:` path.
- Only the `orders` service declares the `build:`; `payments`/`shipping` reuse the same
  `benzene-mesh-service:compose` image with a different `MESH_SERVICE`.
- No AWS/Azure credentials are needed. (`Benzene.Mesh.Host` registers an AWS-Lambda-Invoke source
  for completeness, but it's built lazily — a pure-HTTP mesh like this one never constructs an AWS
  client.)
- Tear down with `docker compose -f examples/K8sMesh/compose/docker-compose.yml down`.
