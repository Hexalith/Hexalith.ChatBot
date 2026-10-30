# Policy-snapshot A6 acceptance packet

**Unaccepted source-candidate packet — 2026-10-30.** This is a request for
data-class and provider evidence, with an inspected source binding. It is not an
approval, an installed protection profile, a runtime observation, a fresh test
receipt, or a TE-2 completion contract. A6 remains open, live policy persistence
remains denied, and Story 1.6 and sprint status remain `review`.

This packet covers only the policy-snapshot portion of A6. Every other required
M0 data class and the exact-candidate A6 gate record remain separate mandatory
obligations; accepting this portion alone cannot close A6.

The [policy ADR](../../../../../docs/adrs/isolated-policy-snapshots.md),
[PRD A6 decision requirement](../../../../planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#open-assumptions-and-decisions),
[gate-record contract](../../../../planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#increment-gate-record-contract),
and [release metadata](../../../../../release-metadata.json) remain authoritative.
No legal basis, retention duration, residency choice, key profile, evidence
freshness window, expiry, approver identity, or approval has been supplied here.

## Exact inspected source candidate

[source-binding.json](source-binding.json) records canonical full revisions,
Git blob IDs, modes, byte lengths, and SHA-256 hashes of committed source bytes
and the separately hashed worktree representations. At inspection, 45 of 46
listed files matched their committed bytes exactly, and each owner checkout HEAD
matched the root HEAD gitlink below. One declared checkout conversion is recorded
explicitly; it is not reported as byte equality.

| Repository | Inspected revision |
| --- | --- |
| ChatBot root HEAD | `90cbaa11ae7bb05fefc44f5a4dd97c0224162920` |
| Root gitlink `references/Hexalith.EventStore` | `50221cf7dbe425becc333f6757ba37b93a22ab4e` |
| Root gitlink `references/Hexalith.Parties` | `9239c531f3b0cbb4a998c419e537463b597ff14b` |
| Root gitlink `references/Hexalith.Platform` | `8de90958bc41b7fd01b7ed22b8ab364447a800e4` |
| Root gitlink `references/Hexalith.Builds` | `81ab049208678bd6616828824729c0ddac02f7d1` |

The Builds catalog `Props/Directory.Packages.props` has an LF committed blob
(30,713 bytes; SHA-256
`76c78725f68a82642c0e26c8aab0256dea1a335b77218841451590d7070328a0`)
and a CRLF worktree representation (31,077 bytes; SHA-256
`f9c97805fe540f17277f0d96f16a92b5f595f24c0f059c7a2c864abe8a6c1fb5`).
Builds' tracked [.gitattributes](../../../../../references/Hexalith.Builds/.gitattributes)
sets this file to `text eol=crlf`, and `git diff --quiet HEAD --
Props/Directory.Packages.props` reports no tracked change. The binding retains
both exact hashes and that attribute source. No sibling file was rewritten or
normalized. This representation difference is not runtime/provider evidence.

This is the source candidate inspected before adding this packet. The binding
excludes this packet, its own JSON, the mutable Story 1.6 spec/appendix, sprint
tracking, generated files, runtime configuration values, and secrets. It does not
hash itself or bind future commits. Root HEAD binds the root tree; the four owner
gitlinks above identify the inspected owner sources, not an exhaustive deployed
dependency or image inventory. A future acceptance record must bind every
consumed dependency and its actual deployment profile. Source drift requires a
new inspection and successor evidence; these hashes cannot approve a successor.

## One source-defined local profile

The inspected profile is ChatBot's non-publishable local Aspire composition:
[AppHost Program](../../../../../src/Hexalith.ChatBot.AppHost/Program.cs),
[resource wiring](../../../../../src/Hexalith.ChatBot.AppHost/Aspire/ChatBotAspireModule.cs),
and [project configuration](../../../../../src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj).
It configures three Dapr `state.redis` components against the same resolved
`Dapr:RedisHost` value:

| Component | Source-defined metadata | Source-defined consumers/purpose |
| --- | --- | --- |
| `statestore` | `actorStateStore: true`, `keyPrefix: none` | EventStore actor/status/archive/checkpoint store; attached to the local EventStore, Tenants, Parties and Projects sidecars and EventStore Admin |
| `chatbot-statestore` | `keyPrefix: name`; no actor flag set here | ChatBot read model and coarse idempotency; attached to ChatBot |
| `chatbot-workflow-statestore` | `actorStateStore: true`, `keyPrefix: name` | ChatBot Dapr Workflow actor state; attached to ChatBot |

`ResolveRedisHost` defaults to `127.0.0.1:6379` when `Dapr:RedisHost` is unset
and requires a host/port endpoint without credentials or a path. This describes
the source branch, not the effective endpoint of an observed run. The source
also configures `chatbot-pubsub` as `pubsub.redis`; it is not a fourth state store.
Component prefixes are logical component configuration. Neither `name` nor
`none`, tenant-shaped keys, filters, cursor checks, or application authorization
constitute independently witnessed per-tenant physical partitioning or ACL proof.

The AppHost selects
[accesscontrol.local.yaml](../../../../../src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.local.yaml):
its local self-hosted profile is described as mTLS-off, with `defaultAction:
allow`, trust domain `public`, and HotReload disabled. The file expressly
requires the separate
[deny-by-default configuration](../../../../../src/Hexalith.ChatBot.AppHost/DaprComponents/accesscontrol.yaml)
with mTLS/Sentry for production. That reference is not proof of deployed ACLs.
The local profile is not an approved production provider, and no topology was
started or inspected for this packet.

Missing provider evidence includes effective sanitized component configuration
and its digest, actual storage identity and region, server-derived physical
tenant partition/namespace and native ACLs, all alternate access paths, Dapr and
storage provider/image versions and immutable image digests, deployed application
and dependency identities, deployment/run identity and timestamps, and an
independent witness. Source SDK/package pins in the bound AppHost and Builds
catalog are not observations of installed runtimes or container images.

## Data-class decision and retained fields

The [PRD data-governance inventory](../../../../planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#data-governance-surface-hexalithchatbot-first-class-durable-records)
describes policy snapshots as medium sensitivity, carrying policy values without
PII, under audit retention with A6 export/erasure treatment and first-use native
isolation proof. This planning classification is not a completed A6 assessment.
The retained authorizing actor and provenance can identify people or link to
restricted evidence; Compliance/Data Protection must explicitly classify these
fields and their combinations before relying on the inventory's no-PII label.
The [inventory/retention ADR](../../../../../docs/adrs/data-class-inventory-and-retention-policy.md)
defines classification contracts, while its live enforcement and approval remain
separate obligations.

The proposed purpose is policy-governed actions and reproducible decisions.
Its legal basis and data-class acceptance remain unaccepted. The existing
[schema](../../../../../src/Hexalith.ChatBot.Contracts/Commands/TenantPolicySchema.cs),
[knob IDs](../../../../../src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyKnobIds.cs),
[value record](../../../../../src/Hexalith.ChatBot.Contracts/Commands/TenantPolicyValue.cs)
and [schema versions](../../../../../src/Hexalith.ChatBot.Contracts/Commands/TenantPolicySchemaVersions.cs)
define these five M0 knobs under `tenant-policy-schema.m0.v1`; their committed
bytes and actual worktree hashes are bound in the source manifest:

| M0 knob ID | Value shape requiring data-class treatment |
| --- | --- |
| `association.t-high` | Finite `NumberValue` double within the schema bounds |
| `association.t-low` | Finite `NumberValue` double within the schema bounds and below the high threshold |
| `attachments.unsafe-handling` | `StringValue` from the schema's closed unsafe-handling enum |
| `ai-action.low-risk-allowed` | `AiActionLowRiskAllowed` map with a Boolean for each of the six action classes declared by the schema |
| `mailbox.routing-rules` | `StringListValue` of free-form routing entries subject to the schema's count/token checks; permitted content and classification require approval |

Structural validation does not approve mailbox routing content or establish that
it contains no personal or restricted data. Compliance/Data Protection must
approve permitted routing content and its classification before persistence.
This packet retains only knob identifiers and value shapes, never actual tenant
policy values or routing entries, and selects no defaults or approvals.

The [synthetic snapshot model](../../../../../src/Hexalith.ChatBot.Server/Governance/Policy/ImmutableTenantPolicySnapshot.cs)
retains the following fields. Its `EvidenceKind` is `synthetic-unaccepted`; its
reference strings do not supply accepted controls or durable evidence.

| Retained fields/control | Required missing decision and evidence | Accountable roles |
| --- | --- | --- |
| `TenantId`, `SnapshotId`, `PolicyVersion`, `SchemaVersion`, `EffectiveAtUtc`, `Values` | Enumerate each M0 bootstrap knob and any later schema rows; approve purpose, lawful basis, minimization, sensitivity, permitted processing and residency. Prove immutable, tenant-bound storage and schema/version validation. | ChatBot policy owner supplies inventory; Compliance/Data Protection and Architecture accept |
| `ActorId`, `ProvenanceReference` | Define minimal retained actor identity, authority source, provenance/reference contents, disclosure and erasure treatment. Prove server-derived tenant/actor, current owner authority, and independent approval where required, including distinct current TenantOwners and required co-approvals for initial bootstrap. Caller fields cannot confer authority. | Tenants/identity and ChatBot policy owners; Security verifies binding; Compliance/Data Protection accepts classification |
| `RedactionReference` | Approve field/view classifications and existence-neutral access for policy, history, audit, diagnostics, telemetry, exports and test artifacts; prove restricted values and references do not escape. | Compliance/Data Protection, Security and ChatBot owner |
| `RetentionReference`, `HoldReference` | Approve class-specific duration, start trigger, maximum/minimum obligations, hold placement/release authority and precedence. Prove hold prevents disposal and expiry cannot silently remove held material; missing or stale authority fails closed. No duration is selected here. | Compliance/Data Protection and data-rights owner; storage/owner implementers supply proof |
| `ExportDeleteReference` | Approve validated request authority, scope, independent approval, recipient delivery, refusal/appeal, partial completion and owner acknowledgements; demonstrate export and deletion across all copies without direct source-owner mutation by ChatBot. | Compliance/Data Protection, ChatBot data-rights and affected source owners |
| `BackupReference` | Enumerate backups, replicas, archives and recovery copies, with approved retention/hold propagation, restoration behavior and deletion/crypto-erasure duties. Prove restore cannot revive erased access; retain owner-by-owner proof. | Storage/backup operations, EventStore and custody owners; Compliance/Data Protection accepts |
| `EncryptionReference` | Approve covered fields and copies, encryption in transit/at rest, tenant/record/target key granularity, production custody backend, access separation, key issuance/rotation/revocation/destruction and outage behavior. Independently demonstrate policy-specific protection and erasure with no usable key material in artifacts. | EventStore, Platform custody and Parties owners supply evidence; Security verifies; Compliance/Data Protection and Architecture accept |
| `SupersedesSnapshotId`, `RollbackSnapshotId` | Prove replacement/rollback append a prospective immutable version; retain historical values, effective order and original decision references through replay/rebuild. Define how retained history survives or is lawfully rendered unreadable after erasure. | ChatBot policy and EventStore owners; Architecture and independent Test Architect |
| Historical decision's exact tenant/snapshot ID/version tuple; surviving metadata | Specify decision/audit propagation and authorize exactly which metadata, hashes, identities and references survive disposal, for what purpose/legal basis, for how long, and with what visibility. Prove no plaintext protected values or reconstructable identifiers survive beyond the accepted contract. | ChatBot and EventStore audit owners; Compliance/Data Protection and Security |

Policy target mapping is unresolved. Owners must supply an approved mapping from
each policy event/snapshot type and protected field to its tenant, owner aggregate,
data class, protection target/key alias, occurrence identity, key profile and
completion/erasure authority. It must cover event history, whole snapshots,
projections, caches, exports, backups and restoration. A mapping for another domain
or registration of an interface cannot stand in for this policy-specific mapping.

## Available owner code and installation gap

| Owner | Inspected capability | Remaining installation/evidence gap |
| --- | --- | --- |
| EventStore | The default [host](../../../../../references/Hexalith.EventStore/src/Hexalith.EventStore/Program.cs) calls `AddEventStoreServer`; its [registration](../../../../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs) defaults `IEventPayloadProtectionService` to `NoOpEventPayloadProtectionService`. Explicit [engine opt-in](../../../../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/InteractionPayloadProtectionServiceCollectionExtensions.cs) installs the real [interaction engine](../../../../../references/Hexalith.EventStore/src/Hexalith.EventStore.PayloadProtection/InteractionPayloadProtectionService.cs) and occurrence/deletion actors. | The engine exists. It protects event `/ProtectedContent/Value` when a valid target is present; events without such a target can remain unprotected. Whole-snapshot protection also needs authenticated occurrence/binding and key authority. [Binding registration](../../../../../references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/InteractionProtectionBindingServiceCollectionExtensions.cs) supplies unavailable adapters unless independent sources are installed. None establishes a policy target, installed custody or acceptance by itself. |
| Platform | [Fr34ProductionComposition](../../../../../references/Hexalith.Platform/src/Hexalith.Platform.Protection/Fr34ProductionComposition.cs) constructs interaction protection ports using custody, a real registry, inventory/authority, explicit binding and material profiles. [Custody registration](../../../../../references/Hexalith.Platform/src/Hexalith.Platform.Custody/PlatformCustodyServiceCollectionExtensions.cs) has unavailable default HMAC/signing providers. [Secrets runtime registration](../../../../../references/Hexalith.Platform/src/Hexalith.Platform.Secrets.Runtime/SecretsRuntimeServiceCollectionExtensions.cs) requires explicit configured authorities and credentials. | Production-capable composition source requires installed originals, current authority and native holder profiles; it neither enrolls targets nor proves an installed ChatBot policy custody path. Approved key custody, actual backend/profile and independently witnessed lifecycle evidence are missing. |
| Parties | [AddParties](../../../../../references/Hexalith.Parties/src/Hexalith.Parties/Extensions/PartiesServiceCollectionExtensions.cs) registers its [EventStore adapter](../../../../../references/Hexalith.Parties/src/Hexalith.Parties.Security/EventStorePartyPayloadProtectionAdapter.cs), [Party protection service](../../../../../references/Hexalith.Parties/src/Hexalith.Parties.Security/PartyPayloadProtectionService.cs) and [LocalDevKeyStorageBackend](../../../../../references/Hexalith.Parties/src/Hexalith.Parties.Security/LocalDevKeyStorageBackend.cs). The latter stores key material in process-memory dictionaries. | This is Parties' own domain adapter and local development backend, not an approved production custody provider or generic policy-snapshot protector. Missing identity-history custody is not registered by `AddParties`. Required Parties integration, regulated-data suitability, owner lifecycle and policy-specific evidence remain for the owners to establish. |

The inspected ChatBot AppHost/Server and default EventStore host do not invoke
`AddInteractionPayloadProtection`, `AddInteractionProtectionBindings`, or
`Fr34ProductionComposition`. ChatBot's ASP.NET Data Protection configuration for
admission markers/query cursors is a separate purpose; it does not install policy
payload protection. The
[policy persistence gate](../../../../../src/Hexalith.ChatBot.Server/Governance/Policy/TenantPolicyPersistenceGate.cs)
still denies the eight policy/configuration commands before mutation as described
by the ADR. The [gateway registration](../../../../../src/Hexalith.ChatBot.Server/Gateway/CommandGatewayServiceCollectionExtensions.cs)
still selects the
[unavailable policy provider](../../../../../src/Hexalith.ChatBot.Server/Governance/AiMediation/UnavailableTenantAiPolicySnapshotProvider.cs).
No registration, runtime file, dependency, owner repository or gate was changed.

The ADR distinguishes the transport outcomes. After authentication, tenant
binding (including payload targets) and grant authorization, a gateway policy
denial is retryable HTTP 503 `dependency_degraded`, with `retryable=true` and
`clientAction=retry-later`; earlier authentication/binding/authorization failures
retain their existing outcomes. An unresolved-policy AI decision returns
non-retryable HTTP 403 `refusal_blocked_action`. SDK `/process` checks
authentication and envelope subject/tenant binding before its policy gate, which
precedes payload validation, payload target binding and grant authorization. Its
`policy-snapshot-unavailable` denial includes only the metadata rejection
`ChatBotDomainServiceAdmissionRejected`: EventStore persists and publishes that
terminal rejection and returns its cached result for a retry with the same
message ID. Persisting this metadata rejection is not a policy mutation and
carries no protected policy values; it is not the gateway's retryable outcome.
Future native refusal evidence must verify these distinctions and precedence.

## Epic Story 1.6 acceptance evidence still required

AC1–AC7 below number the seven Given/When/Then cases in
[epics.md](../../../../planning-artifacts/epics.md#story-16-persist-the-first-isolated-policy-snapshot)
by their existing order. The approved synthetic implementation is retained; all
live/native evidence described below remains absent from this packet.

| Epic AC | Exact remaining evidence | Owner roles and acceptance |
| --- | --- | --- |
| AC1 — Missing A6 denies persistence/onboarding; synthetic validation labelled | Candidate-bound, independently witnessed checks of every enabled policy/onboarding transport showing safe refusal before policy persistence when the contract/runtime evidence is missing, with persisted end-state and no readiness claim. Bind any retained synthetic results separately; this packet executes none. | ChatBot admission owner and independent Test Architect; Compliance/Data Protection and Architecture determine A6 acceptability |
| AC2 — Approved complete metadata; immutable security-sensitive versions | Approved data-class contract covering every field/control above, installed policy target mapping and owner protection/custody; native persisted contents prove complete metadata, trusted actor/tenant and immutable versioning. | ChatBot policy, Tenants, EventStore, Parties and Platform custody owners; Compliance/Data Protection plus Architecture accept |
| AC3 — Exact ID/version resolution; unresolved policy denies | Native decision/pipeline and persisted audit evidence carries exact tenant/ID/version, with missing/stale/malformed/unauthorized/ambiguous and foreign-tenant cases denied without restricted metadata. Explicit native tests must prove that new decisions reject caller-selected superseded snapshots while authorized audit/replay can resolve historical exact references. Owners must define accepted evidence validity/revocation rules; no new freshness window is invented here. | ChatBot policy/gateway and EventStore audit owners; Security and independent Test Architect verify |
| AC4 — Original historical references survive; prospective successors | Native replace/rollback/replay/rebuild evidence retains original immutable values and decision references, introduces only prospective successor versions, and implements the accepted historical retention/erasure treatment. | ChatBot policy and EventStore owners; Architecture and independent Test Architect |
| AC5 — First-store native two-tenant isolation | Exact provider/configuration/ACL/deployment binding and independent native persisted-end-state proof for two tenants using adversarial keys, cursors, caches and direct APIs; cross-tenant reads/writes impossible by trusted server-derived physical partitioning. Include replay, projections, exports, backup/recovery and alternate credentials/write paths. Prefixes or application filters cannot satisfy this case. | Storage/provider operations and EventStore own native controls; ChatBot owns boundary coverage; Security and independent Test Architect witness; Architecture accepts |
| AC6 — Unavailable store or missing/stale/failing isolation proof disables use | Independently witnessed failure injection for store outage and absent/stale/failing proof across policy-backed commands, machine surfaces and multi-tenant use; verify no policy mutation and no enabled increment under invalid evidence. Future proof cannot legitimize earlier unproven writes. | ChatBot gateway, EventStore and operations owners; Security and independent Test Architect |
| AC7 — Protection/separation across persisted state and artifacts | Native byte-level protection and key lifecycle proof; scans of diagnostics, telemetry, exports, backups and test artifacts; hold/retention, export/delete, backup restore and crypto-erasure evidence, including surviving-metadata inventory. Every receipt identifies exact store, sanitized effective configuration/hash, run/deployment, policy contract and independent witness. | EventStore, Platform custody, Parties, ChatBot and backup/export owners; Security and independent Test Architect verify; Compliance/Data Protection plus Architecture accept |

## Evidence limits and approval handoff

| Material | Classification and permitted conclusion |
| --- | --- |
| This README and source binding | Unaccepted documentation/source inspection only; no machine test result or current-run native evidence |
| Existing synthetic snapshot/admission fixtures | Synthetic development coverage, not A6 protection, physical isolation, production configuration or TE-2 completion |
| [Historical owner-service receipt](../owner-service-provider-final-2026-10-25.json), [provenance](../owner-service-provider-final-2026-10-25.provenance.json) and [verification summary](../owner-service-verification-final-2026-10-25.json) | Retained 2026-10-25 authentication evidence with its own older source/assembly binding: actual ChatBot DI owner providers acquired distinct scoped Keycloak identities and the receipt records specified negative access results. No new execution or current-candidate qualification is claimed. Unprovisioned Projects 404 is not positive Projects provisioning/authorization evidence. These receipts prove no policy protection, native tenant isolation, A6 approval or completed Story 1.6. |
| Required future acceptance bundle | Not supplied: approved contract, installed provider/custody/target mapping, exact deployment and effective configuration, native persisted-end-state and adversarial receipts, independent witness, and signed authority decision |

Evidence must retain only approved metadata, locators and integrity hashes in this
repository. Keep raw policy values, personal data, plaintext keys, passwords,
credentials, bearer material, generated realms and unredacted runtime snapshots
out of exported artifacts. Store any necessary protected proof under approved
access/retention controls and reference it safely. Source hashing is not a scan
of runtime state. The packet is outside a TE-2 report and confers no exception to
[story-evidence integrity](../../../../../docs/story-evidence-integrity.md) or
[its policy](../../../../../story-evidence-policy.json).

Compliance/Data Protection and Architecture must accept this policy-snapshot
portion and issue the actual A6 decision only when all required M0 data classes
and the exact-candidate gate obligations are also satisfied;
Security and an independent Test Architect must verify the control and native
isolation evidence independently of its producers. Named human authorities and
their independent signatures remain missing. The immutable gate record must
carry the decision, schema/record identity, exact candidate and every consumed
dependency revision, approved environment/profile, evidence locators/hashes,
owner and independent approver identities, signer-independence result, approval
and explicit expiry timestamps, reopen predicates, and supersession/revocation
links. Retention/hold, key/backend, backup/restore/erasure, adapter, policy target,
provider, configuration or candidate drift must be evaluated under that record's
approved validity/reopen rules. No `approved-current` record is created here.
Owner work, a later authorized runtime lane and independent acceptance remain
dependencies. Story 1.6 cannot become `done` on this packet.

## Reproduce source verification

Run the following read-only check from the ChatBot root. It checks the original
candidate, root gitlinks, owner checkout revisions, exact blob hashes and current
worktree hashes, and reports the declared Builds checkout conversion. Any
unrecorded difference fails; it does not fetch, initialize submodules, change
files or weaken a gate. A later HEAD needs a new candidate binding.

```bash
python3 -I - <<'PY'
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

if sys.flags.optimize:
    raise SystemExit('Refusing optimized Python: source checks require assertions')

root = Path.cwd()
packet = root / '_bmad-output/implementation-artifacts/evidence/story-1-6/policy-snapshot-a6'
binding = json.loads((packet / 'source-binding.json').read_text())

def git(repo, *args):
    return subprocess.check_output(['git', '-C', str(repo), *args])

assert binding['acceptanceStatus'] == 'unaccepted'
assert binding['runtimeObserved'] is False
expected_repositories = {
    '.', 'references/Hexalith.EventStore', 'references/Hexalith.Parties',
    'references/Hexalith.Platform', 'references/Hexalith.Builds',
}
repository_paths = [repo['path'] for repo in binding['repositories']]
assert len(repository_paths) == len(set(repository_paths))
assert set(repository_paths) == expected_repositories
checked = 0
exact = 0
differences = 0
gitlinks = 0
for repo in binding['repositories']:
    directory = root / repo['path']
    revision = repo['revision']
    assert re.fullmatch(r'[0-9a-f]{40}', revision)
    assert git(directory, 'rev-parse', 'HEAD').decode().strip() == revision
    if repo['path'] != '.':
        entry = git(root, 'ls-tree', binding['rootRevision'], repo['path']).decode()
        mode, kind, object_id = entry.split('\t', 1)[0].split()
        assert (mode, kind, object_id) == ('160000', 'commit', revision)
        gitlinks += 1
    source_paths = [source['path'] for source in repo['sources']]
    assert len(source_paths) == len(set(source_paths))
    for source in repo['sources']:
        entry = git(directory, 'ls-tree', revision, '--', source['path']).decode()
        header, path = entry.rstrip('\n').split('\t', 1)
        mode, kind, object_id = header.split()
        assert (path, mode, kind, object_id) == (source['path'], source['mode'], 'blob', source['gitBlob'])
        blob = git(directory, 'cat-file', 'blob', object_id)
        assert len(blob) == source['byteLength']
        assert hashlib.sha256(blob).hexdigest() == source['sha256']
        worktree = (directory / path).read_bytes()
        assert len(worktree) == source['worktreeByteLength']
        assert hashlib.sha256(worktree).hexdigest() == source['worktreeSha256']
        same = worktree == blob
        assert same == source['worktreeBytesMatchCommittedBlob']
        if same:
            exact += 1
        else:
            assert repo['path'] == 'references/Hexalith.Builds'
            assert path == 'Props/Directory.Packages.props'
            assert source['checkoutTransformation']['attributes'] == {'text': 'set', 'eol': 'crlf'}
            assert b'\r\n' not in blob and worktree == blob.replace(b'\n', b'\r\n')
            attributes = git(directory, 'check-attr', 'text', 'eol', '--', path).decode().splitlines()
            assert attributes == [f'{path}: text: set', f'{path}: eol: crlf']
            subprocess.run(['git', '-C', str(directory), 'diff', '--quiet', 'HEAD', '--', path], check=True)
            print(f'DECLARED REPRESENTATION DIFFERENCE: {repo["path"]}/{path}: committed LF; clean CRLF checkout; both exact hashes verified')
            differences += 1
        checked += 1
root_repository = next(repo for repo in binding['repositories'] if repo['path'] == '.')
assert binding['rootRevision'] == root_repository['revision']
assert differences == binding['worktreeRepresentationDifferenceCount'] == 1
print(f'PASS: {len(binding["repositories"])} revisions, {gitlinks} root gitlinks, {checked} committed/worktree hash pairs: {exact} exact byte matches, {differences} declared representation difference; source inspection only')
PY
```

For this documentation slice, validation is limited to source hashes/revisions,
worktree agreement or the recorded declared representation difference, local
Markdown destinations/fragments, evidence labels/exclusions,
whitespace, and unchanged frozen intent/status/runtime files, followed by an
independent documentation review. No live topology, build or xUnit lane is run.

Separately authorized future verification only, not executed here: if the
ordinary Debug build is blocked by CS1704, the current user's permitted
conditional fallback, run from the ChatBot root, is:

```bash
timeout 300s dotnet build tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj --configuration Debug -m:1 -p:HexalithBuildsRoot="$PWD/references/Hexalith.Builds" -p:HexalithCommonsFromSource=false
```

This conditional source/package fallback preserves every other gate. Any later
authorized runtime lane must use `timeout`, and every later xUnit invocation
must also use `< /dev/null`. It must produce its own evidence and cannot reuse
this packet as fresh native qualification.
