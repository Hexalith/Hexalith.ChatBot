---
title: 'Restore ChatBot Aspire Startup with Platform Owner Service Access'
type: 'bugfix'
created: '2026-10-25'
status: 'in-review'
baseline_commit: '6e76a59fc87ef1f0fdb051264d48d8572100b310'
platform_baseline_commit: 'b9cf0e78c42ca8bfe1aa5488cc4340c238887acb'
route: 'dispatch'
review_loop_iteration: 0
context:
  - 'AGENTS.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '.editorconfig'
  - 'Directory.Build.props'
  - 'Directory.Packages.props'
  - 'references/Hexalith.Platform/Directory.Build.props'
  - 'references/Hexalith.Platform/.gitattributes'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The existing local Projects and Memories resources cannot supply ChatBot's owner-service bearer configuration. Platform has no suitable binding; default ChatBot startup aborts.

**Approach:** Add reusable Platform enrollment/binding for dedicated Keycloak owner clients and consume it from the retained ChatBot AppHost. Use the existing issuer and resources, and reuse the EventStore SDK's refreshing client-credentials implementation in ChatBot's owner adapters.

**Approved decisions:** The user selected existing services, identified Platform, and explicitly approved extending this fix into Platform. This overrides the earlier sibling-edit exclusion for this integration. Preserve the already implemented Development launch profile and local mailbox credential. No additional permission checkpoint is required for this approved scope.

## Boundaries & Constraints

**Always:** Separate Projects and Memories identities, secrets, exact audiences/operations and one explicit development tenant (`tenant-alpha`). Keep static external configuration supported and reject incomplete overrides. Preserve receiver authorization and tenant provisioning gates. Reuse SDK OAuth acquisition/cache/expiry behavior; authority mode only, no signing-key fallback. Reject insecure/foreign credential destinations and cross-tenant Project-context requests before network access.

**Never:** Reuse human/operator/workload credentials, widen SDK caller ACLs, forge owner grants or persisted tenant records, add a new topology, initialize nested submodules, load skills under `references`, commit/push/stage, or claim A6/native isolation/story completion. Use source project references for the existing local AppHost helper; do not introduce an unavailable runtime NuGet package or change shared dependency versions.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior |
| --- | --- | --- |
| Local startup | Development, no explicit external owner settings | Existing Projects endpoint and separate secret client credentials are bound; existing Keycloak issues real scoped tokens |
| Explicit external setup | Complete static endpoint/tokens, or partial override | Complete setup keeps existing behavior; partial setup fails closed without local fallback |
| Token lifecycle | Repeated/concurrent calls, expiry, malformed or failed issuer response | SDK caching/refresh is reused; failed acquisition sends no owner request or secret diagnostic |
| Scope and transport | Foreign tenant, audience/operation, origin, or non-loopback HTTP | Deny without granting authority or leaking credentials |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Platform/src/Hexalith.Platform.Composition/` — existing Aspire helper assembly; add generic owner-client/realm/environment bindings here, without composing the G4 topology. Its project already references EventStore.Aspire and ServiceDefaults.
- `src/Hexalith.ChatBot.AppHost/Program.cs` — generate per-run secrets before realm rendering; append dedicated clients to the generated realm after current strict placeholder checks. Local receiver Memories needs explicit Keycloak Authority/Issuer/Audience; Projects already uses the issuer. Use resource endpoint references.
- `src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj`, `Directory.Build.props` — root-declared Platform helper reference and root detection; mark helper non-resource and forward source roots where necessary.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Authentication/JwtWorkloadAssertionIssuer.cs` — existing authority-mode acquisition, exact audience/operation validation, bounded responses, cache and refresh. Reuse unchanged with isolated owner options and dedicated `platform-owner-audience.`/`platform-owner-operation.` scopes. Dedicated tokens also carry REST sub/tenant/permission claims; SDK ACLs stay unchanged.
- `src/Hexalith.ChatBot.Server/Projections/DerivedStores/MemoriesDerivedStoreServiceCollectionExtensions.cs` — retain static-token path; support owner client-credentials configuration and attach handler after Memories' existing handler with its static token unset.
- `src/Hexalith.ChatBot.Server/Adapters/Projects/ProjectsMemoriesCaseResolver.cs` — current client sends no tenant selector; guard mismatches against the explicitly bound owner tenant.

## Tasks & Acceptance

**Execution:**

- [x] Add focused reusable Platform owner-client enrollment/binding types and tests in `tests/Hexalith.Platform.OwnerServices.Tests`; validate distinct identities, secret parameters, exact audience/operation scopes and tenant claims. Add only dedicated clients/scopes to generated realms.
- [x] Wire AppHost local defaults through Platform, resource references and receiver OIDC configuration; preserve existing complete external/static topology fixtures and partial-configuration denials.
- [x] Add internal owner-authentication options/DI/handler glue under `src/Hexalith.ChatBot.Server/Adapters/OwnerServices/`, reusing separate SDK issuer instances and framework options monitors rather than implementing another OAuth provider. Register the SDK HTTP client without redirects. Preserve disposal, cancellation and safe errors.
- [x] Update the Projects resolver's tenant guard and add meaningful Server tests for configuration, valid token attachment/reuse/refresh, denial/transport and tenant isolation. Token response tests may be explicitly synthetic; live authentication must use Keycloak.
- [x] Update existing affected topology tests and README, then build focused Debug projects and run relevant tests individually. Resolve directly exposed build-graph defects without weakening gates or changing versions.
- [x] Start the exact ChatBot AppHost, wait/inspect its resources, and exercise real Keycloak token acquisition plus owner authentication/negative cases. Report unhealthy resources and remaining owner-provisioning limits precisely; leave Story 1.6 in `review`.
- [ ] Complete the final post-review live token probe after the runtime container-removal interruption is resolved; retain the earlier successful live evidence as historical evidence.

**Acceptance Criteria:**

- Given the normal local profile and existing service resources, when Aspire starts, then missing operator-supplied Projects/Memories tokens no longer abort composition and real issuer credentials are used.
- Given a failed, stale or mis-scoped credential, when an owner call is attempted, then no unauthenticated or broader request substitutes for it.
- Given tenant-beta and a tenant-alpha owner binding, when Project context is requested, then no tenant-alpha context can be returned for tenant-beta.

## Implementation Notes

- Existing configuration repair and historical evidence remain in `spec-1-6-fix-aspire-startup.md`; preserve prior root changes. Platform checkout was clean on `main` at the recorded full baseline.
- Baseline AppHost build passed; normal startup now loads user secrets but stops at missing Projects endpoint. Docker/Dapr prerequisites are running; Aspire doctor reports no failed checks.
- Separate focused Integration build currently reports duplicate Commons.UniqueIds source/package assemblies. Do not hide the error or weaken its gate.

- Validation refinement: existing broad Platform.Composition.Tests hits 36 pre-existing CA2007 diagnostics under the umbrella editor configuration. Keep that broad blocker explicit and place new owner-client tests in a focused Platform.OwnerServices.Tests project, rather than editing unrelated legacy awaits or suppressing diagnostics. The actual Composition library and AppHost build passed.


- Parent verification completed the scoped acceptance audit: normal startup binds real owner client credentials, all resource readiness waits pass, and the formerly colliding Memories proxy now has its own allocated port. All four matrix rows are covered by the 52 focused Server cases, 39 topology/realm cases, 2 Platform helper cases and the live checks below; none were skipped.
- Independent live evidence `/tmp/chatbot-owner-live-probe.json`: both dedicated tokens have verified RS256 signatures and exact issuer/audience/operation/tenant claims. Keycloak refuses cross-owner audience grants with HTTP 400. Memories returns 401 for anonymous/wrong-audience requests, 403 for tenant-beta under tenant-alpha credentials and 404 for the unprovisioned matching tenant. Missing Project context returns existence-neutral 404; that negative response alone is not receiver-authentication proof. No owner grants/tenant records were created and no token values were printed.
- The initial probe needed correction to select Project/Container resource URLs rather than Dapr sidecar HTTP URLs; no application defect or passing authentication claim is inferred from those incomplete attempts.
- Final full Server command `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh tests/Hexalith.ChatBot.Server.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Server.Tests /tmp/chatbot-owner-final-full-server.ctrf.json -result-trx /tmp/chatbot-owner-final-full-server.trx` exited 1: 3303 tests, 3296 passed, 7 failed, zero skipped. All failures are unchanged `FoldersFolderStoreTests`: `FileUpload.BuildInlineFileMutation` now requires caller-observed expectedFileVersion/lock-fence values that the unchanged ChatBot Folders adapter omits. Both adapter/test paths and the Folders checkout are unmodified. The separate broad Platform test-project CA2007 blocker remains recorded above; no gate was suppressed.
- Published API inspection confirmed the existing `Hexalith.EventStore.ServiceDefaults` 3.127.0 package already includes the required SDK issuer and security-clock constructors; no new runtime package is needed.

## Verification

- Build AppHost, Server.Tests and Platform.OwnerServices.Tests individually in Debug with serialized traversal; run actual targeted test assemblies with single-dash selectors and zero skips.
- Use exact-target `aspire start`, `aspire wait`, `aspire describe`, and logs; acquire real dedicated owner tokens without printing values. Static/synthetic checks are not native policy-isolation evidence.


## Review Triage Log

| ID | Finding | Verdict | Route and evidence |
| --- | --- | --- | --- |
| B1 | Projects authoritative current-tenant selector is omitted | high | Patch: the receiver accessor prioritizes `eventstore:current-tenant`; reject conflicts in static and acquired credentials before owner access. |
| B2 | Memories operation labels do not constrain its native tenant API authorization | medium | Patch documentation: receiver authentication/tenant checks are intentionally preserved; explicitly state the native tenant-wide grant and distinguish token request/cache scopes from receiver permissions. No new receiver authorization model is inferred. |
| B3 | Opaque Projects credentials can compose without a usable tenant binding | low | Rejected: native Projects requires JWTs; opaque configuration is retained for existing topology/transport fixtures and the resolver already denies every unbound read. README states the explicit binding requirement. Adding a new startup branch would change compatibility for an invalid native credential, without preventing an allowed effect. |
| B4 | Generic helper rejects repeated audience/operation scopes for distinct clients | low | Rejected: the only current caller creates one client per owner with distinct scopes, and collision denial is explicit. Supporting a second consumer requires scope-equivalence/reuse logic beyond a simple correction for a future caller. |
| B5 | Rejected REST identity can remain in the SDK cache | medium | Patch: malformed REST identity is demonstrated by existing tenant/identity response tests; invalidate the isolated SDK issuer after rejection and verify recovery on a corrected response without adding an OAuth implementation. |
| B6 | Missing/nonpositive exp and future nbf can be attached | medium | Patch: SDK fallback lifetime does not establish token validity; require current bounded JWT NumericDate fields before attachment. |
| B7 | Trailing slash is removed from exact issuer identifier | medium | Patch: preserve the configured issuer string while normalizing transport URIs independently; exact OIDC issuer matching must not rewrite identifiers. |
| B8 | Non-root endpoint without trailing slash loses its final path segment | medium | Patch: the generated Projects client uses relative API paths with HttpClient.BaseAddress; make directory-base normalization explicit and verify a non-root typed-client request. |
| B9 | Redirect behavior is untested through registered clients | medium | Patch; same verification root cause as V2. Existing direct-handler transports do not exercise real HttpClientHandler redirects. |
| B10 | No new authenticated SDK receiver-denial test | low | Rejected: enrolled REST audiences differ from SDK audiences; current helper tests bind exact audience values and prohibit SDK scope grants, and no SDK caller ACL changed. Making a genuinely SDK-eligible owner fixture adds substantial synthetic authority machinery for an unchanged boundary; live grant-scope negatives remain available. |
| E1 | Acquired Memories token can carry extra tenant aliases | high | Patch with B1: validate every receiver-recognized tenant alias against the sole enrolled tenant; external static Memories multi-tenant support remains unchanged. |
| E2 | Acquired token lacks current lifetime validation | medium | Patch with B6; missing/nonpositive exp and future nbf are explicit invalid response states. |
| E3 | Out-of-range exp escapes SDK timestamp conversion | medium | Patch with lifetime handling: SDK FromUnixTimeSeconds can throw ArgumentOutOfRangeException outside its catch filter; convert this into bounded unavailable-token denial. |
| V1 | DI-resolved owner client credentials never send a request in tests | medium | Patch: pre-verified mutation removes the registered bearer handler while existing direct-handler tests stay green. Exercise a DI-resolved adapter against an authenticated local test receiver and assert its result/state. |
| V2 | Issuer redirect registration has no behavioral test | medium | Patch: pre-verified mutation enables redirects in the final named-client registration; a real 307 can resend client_secret to a second listener. Assert no destination request using registered factory transports. |
| V3 | Required permission mapper is not asserted by helper tests | medium | Patch: pre-verified mutation deletes eventstore:permission while helper assertions and independently built synthetic tokens remain green. Assert exact permission mapper values and access-token inclusion. |
| V4 | Existing AppHost source guard still expects removed variable | medium | Patch: parent full AppHost lane reproduced 18/19 passing, one failure on the obsolete memoriesApiTokenParameter assertion. Preserve secret-binding coverage while updating the changed wiring assertion. |

All three independent layers completed before triage. No intent gap or code re-derivation is required: surviving groups are corrections inside the approved existing-service authentication scope. B1/E1, B6/E2/E3 and B9/V2 share roots; other patch entries remain distinct. The two existing broad build/test blockers remain separately disclosed and unchanged.

- Primary verification follow-ups P1/P2 are deferred as pre-existing: the unchanged Folders version/fence contract mismatch (medium, seven final full-Server failures) and broad Platform test CA2007 incompatibilities (medium developer validation blocker). Both were appended to deferred-work.md; no old entry was edited or duplicate search performed.

## Review Patch Results and Final Recheck (2026-10-25)

- All 10 surviving patch groups are implemented: every receiver-recognized tenant selector must agree; acquired JWTs need a current bounded numeric expiry and no future nbf; out-of-range SDK date conversion becomes unavailable-token denial; rejected REST identities reset only their own SDK issuer under acquisition/disposal coordination. Exact issuer identifiers and non-root owner directory bases are preserved. Future iat is not treated as token activation.
- New real loopback tests resolve the registered adapters through DI, validate signed bearer tokens at the receiver, and verify a persisted/retrieved derived-store entry plus a Project case result. Separate two-listener 307/308 tests verify the registered issuer/owner transports do not replay credentials. Enrollment assertions cover exact permission values and access-token inclusion; the obsolete AppHost wiring assertion now retains secret-parameter coverage. README describes Memories' existing tenant-wide native grant explicitly.
- Parent Debug AppHost build passed with zero warnings/errors: `dotnet build src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --configuration Debug --no-restore -m:1 -nodeReuse:false -p:NuGetAudit=false` (`/tmp/chatbot-owner-postreview-apphost-build.log`). Server.Tests, AppHost.Tests, IntegrationTests and Platform.OwnerServices.Tests each then passed individual output-refresh builds with the same options plus `-p:BuildProjectReferences=false`; logs are `/tmp/chatbot-owner-postreview-{server,apphost-tests,integration,platform}-build.log`. Earlier full project traversals are recorded above; no failed build gate was suppressed.
- Parent verification used `XUNIT_REQUIRE_ZERO_SKIPS=1 bash .github/scripts/run-xunit-v4.sh <built-runner> <ctrf-path> -result-trx <trx-path>`. Full Server receipt: **3328 executed, 3321 passed, 7 unchanged Folders failures, zero skips**. Its affected owner-authentication, actual-factory/adapter, Projects resolver and Memories DI classes all passed: **77/77**. Full AppHost: **19/19**; focused Integration owner/channel-token/recovery/scaffold classes: **39/39**; focused Platform enrollment: **2/2**. Thus **137 affected cases passed**, zero failed/skipped. CTRF/TRX outcome counts and xUnit 4.0.2 runner identity were independently reconciled in `/tmp/chatbot-owner-postreview-verification.json`; lane files use `/tmp/chatbot-owner-postreview-{full-server,apphost,integration,platform}.{ctrf.json,trx,log}`. The two already-deferred broad blockers remain unchanged; they do not make the full Server suite green.
- Post-review ordinary starts both exited 0 but Docker killed/deleted the run-owned Keycloak and Memories vector/graph containers before readiness completed. Logs: `/home/administrator/.aspire/logs/cli_20261025T122004386_detach-child_e238e603b0f24aada3967607b03918aa.log` and `/home/administrator/.aspire/logs/cli_20261025T122157096_detach-child_70982ce9c926440088b17731f3a9de9e.log`. Docker events corroborate kill/die/destroy events; the caller is unknown. No source defect is inferred from external resource deletion.
- An exact-target `aspire start --non-interactive --no-build --isolated --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --format Json` also exited 0. Its isolated secret store was supplied the existing mailbox credential through the child environment without printing or changing it. All four `aspire wait <security|memories|projects|chatbot> --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj --timeout 45 --non-interactive` checks passed. The containers were subsequently deleted again and the final `python3 /tmp/chatbot-owner-live-probe.py` exited 1 with a TimeoutError before writing a new authentication receipt. Latest log: `/home/administrator/.aspire/logs/cli_20261025T122406546_detach-child_59da4abd9d30404d9bfc86b73f72e71b.log`; sanitized state snapshot: `/tmp/chatbot-owner-postreview-resources.json`. Earlier successful live receipt remains historical; the final real-token/SDK-scope negative recheck is pending, not passing.
- Exact-target stop succeeded after each interrupted run, including the isolated run; no force stop or unrelated AppHost stop was issued. Requested that the user pause any concurrent container cleanup before another final live probe. Workflow remains at verification in `in-review`, with no completion/commit transition while this runtime check is blocked. Story 1.6 and sprint remain `review`; accepted A6/native persisted-state isolation is still absent. All changes remain uncommitted as approved.
