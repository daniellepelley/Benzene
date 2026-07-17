# Benzene.CloudService.Probe

## What this package does
The **external live-probe checker** for `docs/specification/cloud-service-profile.md`, referenced
by its §5 ("Conformance testing") as the future-work item that closes the gap left by
`Benzene.CloudService`'s wiring-time self-check. Where `CloudServiceProfileReport` trusts what a
service's own setup code says it provisioned, `CloudServiceProbe` hits a running service over real
HTTP from outside and independently verifies what it can observe - exactly like an operator or a CI
job auditing an arbitrary service, .NET or not.

## Key types
- `CloudServiceProbe.RunAsync(HttpClient, CloudServiceProbeOptions?, CancellationToken)` - the
  entry point. `httpClient.BaseAddress` is the target service; runs R1-R8's checks and returns a
  `CloudServiceProbeReport`. Never throws for an unreachable or non-conformant service - failures
  become verdicts, not exceptions.
- `CloudServiceProbeOptions` - overrides the 3 default `/benzene/*` paths and toggles the R8
  bonus traceparent probe.
- `CloudServiceProbeReport` / `CloudServiceProbeRequirement` / `CloudServiceProbeVerdict` - the
  **tri-state** result: `Satisfied` / `NotSatisfied` / `Inconclusive`, each requirement carrying an
  always-populated `Reason` (never optional, unlike the self-check's `Note` - an outside observer
  explaining itself matters even more when it has no service-side word to fall back on).
- `CloudServiceProbePaths` - this package's own copy of the 3 `/benzene/*` default path constants.

## Important conventions
- **The tri-state honesty rule is non-negotiable.** A black-box HTTP probe genuinely cannot verify
  everything: R8 (trace propagation) is never observable from one service in isolation, and R6's
  registration/heartbeat half isn't either - both stay `Inconclusive` by design, never silently
  upgraded to `Satisfied`. R7 goes `Inconclusive` (not a guess) the moment the caller points the
  probe at non-default paths, since the probe then has no way to know what the service's *own*
  defaults are. Don't "simplify" any of this into a bool - that's exactly the overclaiming this
  package exists to avoid (see `CloudServiceProbeVerdict`'s doc comment).
- **Deliberately not dependent on `Benzene.CloudService`.** No `ProjectReference` to it, no NuGet
  packages beyond the BCL (`System.Net.Http`, `System.Text.Json`) - see
  `CloudServiceProbePaths`'s doc comment for why. The profile is language-neutral; a Go or Node
  port claiming it should be checkable by this same tool. Adding a reference back to
  `Benzene.CloudService` to "avoid duplicating" the path constants would defeat the point.
- R1/R2/R7 are inferential (derived from whether R3-R6 succeeded), not probed directly - there's no
  standalone HTTP surface for "is there a hosted pipeline" or "are handlers registry-backed"; see
  `CloudServiceProbe`'s per-requirement comments for exactly what evidence each verdict rests on.
- Wire shapes checked against are the same ones `Benzene.Mesh.Wire`/`Benzene.CloudService` produce
  (wire-contracts.md §1.2/§5, mesh.md §2), but parsed independently here with `System.Text.Json`
  rather than shared model types, to keep the no-dependency guarantee above.

## Dependencies on other Benzene packages
None. BCL only (`System.Net.Http`, `System.Text.Json`).
