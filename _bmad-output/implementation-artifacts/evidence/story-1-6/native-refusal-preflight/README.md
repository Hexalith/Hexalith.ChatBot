# Native refusal preflight — unaccepted executable handoff

This is an **unaccepted proposal** for the bounded missing-proof branch of Story
1.6 AC6. It does not authorize policy persistence, tenant onboarding, AC5 physical
isolation, stale-proof/outage qualification, all-copy protection, A6 acceptance or
TE-2 completion. The original spec baseline and literal `review` status remain.
The [historical A6 packet](../policy-snapshot-a6/README.md) and retained owner-service
receipts are unchanged and supply no current native witness evidence.

The implementation is under
[`PolicyIsolation`](../../../../../tests/Hexalith.ChatBot.IntegrationTests/PolicyIsolation/PolicyRefusalPreflightTests.cs).
The [wire proposal](contract-proposal.json) is not a story-completion contract.
The [successor inspection](source-binding.json) binds an inspected source graph
and the new implementation bytes separately. It is not runtime evidence.

## What executes and what stays blocked

The producer reuses the existing local AppHost. Its dedicated Redis has an exact
run label, loopback port and ephemeral `/data` tmpfs, including its fixture AOF.
It has no ambient Redis fallback and claims no persistence across run cleanup.
Platform's public `DaprSelfHostedMtls.AddLocalApplicationInfrastructure` supplies
the run-owned Sentry, placement, scheduler and credential lifecycle. Every
selected sidecar explicitly enables mTLS before the supported catalog helper
renders closed receiver ACLs. The helper's readiness/security gates remain in
force; an unavailable helper or failed identity infrastructure stays blocked.
The existing Keycloak is explicitly enabled with `KeycloakPersistent=false` and
session lifetime; ambient external Projects/Memories or certificate overrides
are rejected before owner calls. The
sidecars use Dapr's built-in SQLite name-resolution component with one private
run-owned registry file, preserving actual app IDs without shared mDNS discovery.
That registry contains service-discovery metadata only; domain state stays in
EventStore's selected Redis backing. The supported EventStore helper prepares
the actual compiled Tenants, Parties and
Projects owners, renders finite receiver ACLs, and installs its real committed
catalog authority. It never uses the convenience capture helper with the local
default-allow ACL. No production source, admission provider or policy gate is
replaced. No mock authority, global-admin JWT flag, raw domain-state write or
Platform foreign-sentinel grant is installed.

The real Tenants bound-workload bootstrap represents `admin-user`, using the
existing owner's issuer, private resource proof and sidecar channel. The disposable
composition declares only the actual Tenants bootstrap command domains,
`tenants,global-administrators`. Every normal owner command must complete with
positive committed sequence and event count. Both tenants must be created and
their distinct principals added as `TenantOwner`; Parties identities must commit.
An owner-native, signed bound query must confirm each exact tenant/principal.
Authentication and HTTP acceptance alone cannot satisfy provisioning.

Each policy submission additionally needs a real committed ChatBot
`admin:policy` authority grant for its exact change resource. The producer attempts
the supported canonical `GrantChatBotAuthority` command. If A13's accepted
owner-dispatched atomic participant is unavailable, it fails with that prerequisite
and never substitutes a grant/provider. A catalog, issuer, mTLS/ACL, readiness or
current-owner refusal also remains a failure. ChatBot currently has no compiled
owner-catalog declaration; the helper cannot invent one. Consequently code
availability does not establish that the production graph can yet execute the
whole producer lane.

The producer separately records two **generic HTTP** responses: HTTP 503,
`code=dependency_degraded`, `retryable=true`, `clientAction=retry-later`.
These fields can also result from an admission exception before the policy gate;
they never prove that the `policy-snapshot-unavailable` branch ran. Each response
must retain the immutable request's exact correlation/task IDs. Both request
bodies and command/correlation/task IDs are constructed **before** the immutable
request is hashed and handed to the observer. Only body hashes and metadata are
exported; the precisely reused UTF-8 bodies/policy values stay in memory.

The exact current blocker is a **ChatBot-owned, independently observable admission
cause reader** bound to command ID, command type, correlation ID, task ID, tenant,
principal and body hash, with reason `policy-snapshot-unavailable`.
`InMemoryAuditWriter.AuthorizationFailures` has no supported cross-process reader;
its existing fact also has no command ID or body-hash field. A future accepted
owner observation must establish that complete correlation, rather than treating
the existing seven-field in-process fact or a generic 503 as sufficient. This
slice adds no production API, admission replacement or producer cause facts.
The live producer therefore explicitly remains **blocked/unknown** on that
prerequisite even if generic HTTP responses and native inventory look correct.

The producer first requires an independently signed **before** observation.
The proposed **after** consumer additionally requires two independent, exact
ChatBot-owned cause observations and unchanged complete native state. Its wire
support is a future executable consumer contract, not an installed cause reader.
Background or unexplained writes fail this
conservative predicate. Such failure does not diagnose a policy mutation; it
leaves the absence claim blocked/unknown.

## Producer procedure

1. Independently enroll an observer/operator and public key. This is an operational
   test enrollment, not accepted A6 protection or Compliance/Architecture approval.
   The observer holds its private key, native read-only credential and provider
   inspection authority outside the producer checkout. No such authority or
   credential is supplied by this repository. Missing enrollment fails before
   topology startup.
2. Ensure every root/index gitlink agrees with its actual checkout. Reinspect/rebind
   after any candidate or implementation change. Every consumed repository's full
   tracked/untracked dirty inventory must be clean apart from explicitly bound
   implementation/evidence paths and the mutable spec. The identical manifest,
   candidate, source/configuration and selected runtime artifact bytes are checked
   before allocation, after builds/preparation, immediately before submissions
   and final acceptance. Never fix this by staging pointers,
   fabricating mapping hashes or initializing nested submodules in this task.
3. Build the narrow project, preserving all existing gates:

   ```bash
   timeout 300s python3 .github/scripts/with-source-mapping.py dotnet build tests/Hexalith.ChatBot.IntegrationTests/Hexalith.ChatBot.IntegrationTests.csproj --configuration Debug -m:1 --nologo
   ```

   Only if ordinary Debug reports CS1704, use the authorized additional arguments
   `-p:HexalithBuildsRoot="$PWD/references/Hexalith.Builds" -p:HexalithCommonsFromSource=false`.
   This fallback does not override HXW004 or other mapping/authority refusals.
4. The separately enrolled, private trust file has exactly the operational fields
   below. `authorizedBy` records who enrolled the observer; it is not a legal basis,
   gate decision or cryptographic attestation of A6 approval. Supply the public key
   only, never the private key:

   ```json
   {"observerId":"independently-enrolled-operator","authorizedBy":"enrolling-test-owner","publicKeyPem":"-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"}
   ```

5. Select the producer explicitly. The flag selects execution and grants no
   authority or approval. Use live output to obtain the private exchange directory:

   ```bash
   HEXALITH_POLICY_REFUSAL_PREFLIGHT=1 HEXALITH_POLICY_REFUSAL_OBSERVER_TRUST=/private/observer-trust.json timeout 900s python3 .github/scripts/with-source-mapping.py dotnet tests/Hexalith.ChatBot.IntegrationTests/bin/Debug/net10.0/Hexalith.ChatBot.IntegrationTests.dll -method '*MissingProofMustRefuseBothRealPrincipalsWithoutNativeMutation' -showLiveOutput < /dev/null
   ```

   The internal deadline is 14 minutes; each external prerequisite command is
   bounded to 60 seconds and each selected owner preparation to three minutes.
   An unselected test skips with an explicit no-evidence label. Once selected,
   missing prerequisites fail; no catch converts them into passes/skips. The wrapper
   retains the verified mapping environment for the selected owner child builds.

## Independently operated read-only observer procedure

The observer is a separately enrolled operator/process, using a key unknown to the
producer. The producer has no observation collector, signing key or success stub.
The signed assertion is meaningful only when that operator actually follows these
steps and the enrollment is independently controlled. File signatures cannot prove
organizational independence or accepted protection by themselves.

1. Inspect the exchange `before-request.json`, `request.json`, `configuration.json`
   and successor manifest. Independently resolve actual root/owner source and
   implementation hashes and both immutable submission metadata/body hashes.
   Independently identify the actual producer, AppHost, selected child processes,
   loaded assemblies/dependency manifests and native sidecar executables; reconcile
   their loaded identities/content hashes against **every** `runtimeArtifacts`
   entry. Files merely present on disk are insufficient. Set
   `actualLoadedArtifactsVerified=true` only after this independent verification.
   Bind the same artifact inventory in both signed observations. Compare the live
   sidecar process arguments with **every**
   rendered input path/hash in `configuration.json`, plus current loaded component
   metadata. Verify that `statestore`, `chatbot-statestore`,
   `chatbot-workflow-statestore`, pub/sub and dedicated catalog authority all select
   the run-owned Redis. Check actual database index, source-defined prefixes,
   provider/image version and unique deployment/run identity. A producer model or
   a source hash is insufficient proof that a process loaded it.
2. Through independent provider inspection authority, inspect the exact full Docker
   container ID, `io.hexalith.policy-refusal.run` label, image content digest and
   published loopback Redis endpoint. Through a separately issued **read-only**
   native Redis credential, query `INFO server` and require its `run_id` to match.
   Require DB 0 of that disposable instance. Do not observe an ambient endpoint,
   replica, backup, another DB or a filtered tenant namespace. Stop if independent
   read-only credentials/inspection authority are absent.
3. Independently repeat the real owner completion/readback for both positive
   fixtures, including distinct current `TenantOwner` memberships. Identify a
   different actual persisted membership/event key for each tenant by inspecting
   native records and reconciling the exact owner stream and committed sequence.
   This identification cannot be inferred from a prefix or copied from a
   producer-provided boolean. Keep protected native bytes in the observer process
   only; retain key hashes and metadata-only control assertions.
4. Capture the **entire database** in one atomic read-only Redis script. The exact
   script body is:

   ```lua
   local keys = redis.call('KEYS', '*')
   table.sort(keys)
   local entries = {}
   for _, key in ipairs(keys) do
     table.insert(entries, {key, redis.call('TYPE', key).ok,
       redis.call('DUMP', key), tostring(redis.call('PEXPIRETIME', key))})
   end
   return {redis.call('DBSIZE'), entries}
   ```

   Invoke with `EVAL_RO <script> 0` on the independently verified DB. The observer
   account must permit these read operations and `EVAL_RO`, while denying writes;
   its credential must never be a producer token or written to this packet.
   An independently operated RESP client must preserve the returned binary bytes.
   Require `DBSIZE == entry count`, unique raw keys and no `nil` dump. Do not use
   `SCAN MATCH`, tenant-prefix filtering, sampling, a Dapr state API or a
   producer-produced inventory. Reject truncation, decode errors, timeouts and
   database changes during collection. Bound work/memory to the disposable fixture
   database; exceeding the observer's limit is blocked, never partial success.
5. For each returned entry, retain `keySha256 = SHA256(raw key)` and
   `valueSha256 = SHA256(frame(type) || frame(dump) || frame(expiry))`, where
   `frame` is an unsigned 8-byte big-endian byte length followed by exact bytes;
   type and expiry use ASCII. This includes absolute expiry in the mutation
   predicate without exposing native keys, values, actor payloads or credentials.
   Sort retained entries by `keySha256`. Positively identified control keys must
   appear in that full inventory. Hashes of synthetic fixture records are diagnostic
   metadata, not encryption or anonymization acceptance for live tenant data.
6. Build the closed payload specified in `contract-proposal.json`, copying exact
   source/run/request/configuration/backing bindings only after independent
   verification. Record UTC start/end immediately around the atomic observation.
   The before capture must finish before any policy submission. Hash `request.json`
   exact bytes for `requestSha256`. Attest the two positive controls only after step
   3 succeeds. Use RSA at least 3072 bits, SHA-256, PSS with a 32-byte salt. Sign the
   **exact UTF-8 payload bytes**; do not reserialize after signing. Publish only
   `{payloadBase64,signatureBase64}` as `before.observer.json` using atomic rename.
   With OpenSSL, signing exact payload bytes uses:

   ```bash
   openssl dgst -sha256 -sign /private/observer-key.pem -sigopt rsa_padding_mode:pss -sigopt rsa_pss_saltlen:32 -out /private/signature.bin /private/payload.json
   ```

7. The current producer writes `after-request.json`, then explicitly blocks on the
   missing exact ChatBot-owned cause reader. Do not manufacture an after cause
   file to make it pass. Once an accepted owner observation mechanism exists,
   independently confirm that both
   submissions ended, then repeat steps 1–6 against the **same backing**. Include
   the SHA-256 of exact `before.observer.json` bytes as `beforeEnvelopeSha256`.
   Obtain the two exact owner cause records independently, correlate all immutable
   request IDs, tenant/principal and body hashes, require the exact policy reason,
   and retain a metadata-only evidence hash/reference. `policyGateCauses` must use
   owner `ChatBot` and source kind `chatbot-owned-correlated-admission-observation`;
   producer HTTP receipts/log claims cannot supply them. Record their independent
   readback time within the after capture. Missing or ambiguous owner causes stay
   blocked/unknown. Publish `after.observer.json` atomically. The verifier compares every key/value
   digest and exact count across the full inventories. Added, removed, changed or
   expired records, missing controls, wrong backing and invalid signatures fail.
   A missing file reaches the bounded producer deadline and fails.

## Receipts, cleanup and limitations

The sanitized exchange artifacts contain source/configuration hashes, fixture
identities, immutable submission/body/runtime hashes, owner metadata receipts,
generic HTTP receipts and signed observation envelopes. Those artifacts must
contain no tokens, passwords, private keys, raw Redis records or protected policy
values. Private diagnostic subdirectories are excluded from that sanitized set
and must never be copied as observer receipts. `request.json` is immutable; later phase requests bind
its hash. `producer-result.json` explicitly leaves every broader acceptance flag
false. Each actor's separate EventStore bearer is retained in memory for EventStore
completion polling; the ChatBot bearer is used only for ChatBot calls.
Failed startup attempts a sanitized `blocked.json`; failure to write it never skips
cleanup. Exact prerequisite command/exit/stdout/stderr diagnostics are bounded to
1 MiB per stream and retained in private mode-0700 directories with mode-0600
files. Only their safe locator is exposed. Raw diagnostics may contain secrets and
are **not** sanitized/exportable receipts. Detailed owner-preparation logs stay
in the supported helper's private directory, not a sanitized receipt.
Review private logs before sharing; never copy secret-bearing generated realm
imports or arbitrary runtime logs into this packet.

Normal/failing test disposal stops its own testing application, disposes selected
owner/catalog preparations, and removes only containers whose exact name and run
label match. Every cleanup step runs despite earlier failures, and every failure
is aggregated. Redis uses tmpfs instead of leaving an image-created anonymous AOF
volume. If an anonymous volume nevertheless exists, its exact image-declared
mount and exclusive attachment to the run-owned container must be verified before
`docker rm --force --volumes`; both container and exact volume removal are checked.
Shared/named/unproven volumes are never removed. The owned mTLS directories are
removed after disposal, and `cleanup.json` reports sanitized per-step outcomes.
No global Aspire stop, Docker prune, ambient deletion or shared volume
cleanup is allowed. On external termination, retain the run directory and inspect
each exact container's label before `docker rm --force <exact-owned-id>`; remove
only this run's storage and private files after the independent operator has
retained approved sanitized evidence. Cleanup failure is reported as failure.

The remaining authorities are unchanged: accepted policy-specific protection,
provider/custody binding, independently witnessed native isolation and persisted
end-state acceptance, named Compliance/Data Protection and Architecture decisions,
and the separate TE-2 completion gate. This preflight cannot accept any of them.
Actual build/test results and fresh blockers are appended to the Story 1.6 spec;
passing synthetic verifier tests prove the consumer's refusal rules only.

## Reproduce the source inspection

Run from the root with Python isolated; this checks exact committed and worktree
bytes without changing the index, siblings or source. A later HEAD/byte change
requires a separately reviewed successor binding. Root gitlink agreement is
checked independently of the manifest's summary flag.

```bash
python3 -I - <<'PY'
import hashlib, json, pathlib, subprocess
root = pathlib.Path.cwd()
packet = root / '_bmad-output/implementation-artifacts/evidence/story-1-6/native-refusal-preflight'
manifest = json.loads((packet / 'source-binding.json').read_bytes())
def require(ok, reason):
    if not ok:
        raise SystemExit(reason)
def git(repository, *args):
    return subprocess.check_output(['git', '-C', str(repository), *args], stderr=subprocess.DEVNULL)
def digest(data):
    return hashlib.sha256(data).hexdigest()
require(git(root, 'rev-parse', 'HEAD').decode().strip() == manifest['rootRevision'], 'root candidate moved')
seen = set()
for repository in manifest['repositories']:
    relative = repository['path']
    require(relative not in seen, 'duplicate repository')
    seen.add(relative)
    actual = git(root / relative, 'rev-parse', 'HEAD').decode().strip()
    require(actual == repository['worktreeRevision'], 'owner checkout moved')
    if relative != '.':
        link = git(root, 'rev-parse', 'HEAD:' + relative).decode().strip()
        require(link == repository['rootGitlinkRevision'] == actual, 'root gitlink differs')
declared = {line.split(' ', 1)[1] for line in git(root, 'config', '-f', '.gitmodules', '--get-regexp', r'^submodule\..*\.path$').decode().splitlines()}
require(seen == declared | {'.'}, 'repository inventory incomplete')
seen = set()
for source in manifest['sources']:
    path = source['path']
    require(path not in seen, 'duplicate source')
    seen.add(path)
    data = (root / path).read_bytes()
    require(len(data) == source['worktreeBytes'] and digest(data) == source['worktreeSha256'], 'worktree bytes changed')
    if source['committedSha256'] is not None:
        blob = git(root / source['repository'], 'show', 'HEAD:' + source['repositoryRelativePath'])
        require(len(blob) == source['committedBytes'] and digest(blob) == source['committedSha256'], 'committed bytes changed')
for artifact in manifest['preservedHistoricalArtifacts']:
    data = (root / artifact['path']).read_bytes()
    require(digest(data) == artifact['sha256'] and data == git(root, 'show', 'HEAD:' + artifact['path']), 'historical packet changed')
print('Source inspection reproduced; no runtime or acceptance claim.')
PY
```
