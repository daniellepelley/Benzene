// Thin Worker that proxies every request into the Benzene.Example.Cloudflare container - see
// ../wrangler.toml and docs/getting-started-cloudflare.md. Not deployed/validated from this
// environment; review before production use.

import { Container, getContainer } from "@cloudflare/containers";

export class BenzeneContainer extends Container {
  defaultPort = 8080;
}

interface Env {
  BENZENE_CONTAINER: DurableObjectNamespace<BenzeneContainer>;
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const container = getContainer(env.BENZENE_CONTAINER);
    return container.fetch(request);
  },
};
