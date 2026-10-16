# Isolated policy snapshots: synthetic foundation, live writes denied

Status: partially implemented; Story 1.6 remains in progress. A6 and native
isolation acceptance are incomplete. This decision does not authorize live policy
persistence, pilot data, compliance claims, or production readiness.

Policy submit, approve, direct association-threshold update, escalation policy
submit, notification-routing snapshot change, and mailbox-configuration snapshot
change commands are denied before admission reservation or dispatch. This applies to legacy HTTP requests, canonical requests, SDK `/process`
requests, and correctly signed admission markers. A marker proves transport
integrity; it is not protection or isolation approval. Existing authentication and
tenant-binding failures keep their safe semantics. Denials use the existing audit
path and `policy-snapshot-unavailable`, mapped by the safe-message catalog to
`dependency_degraded`. No protected policy values are included in that outcome. Dependency denials remain
audited without incrementing the authorization-failure spike counter.

The internal snapshot, history fold, and resolver are explicitly synthetic. They
have no DI registration, store, event subscription, or runtime persistence path.
Snapshot construction copies nested lists and dictionaries, validates the closed
policy schema, and retains tenant, snapshot ID, policy/schema versions, effective
UTC, actor, provenance, redaction, retention, legal hold, export/deletion, backup,
encryption, supersession, and rollback references. All protection references are
unaccepted synthetic metadata; the model cannot confer A6 acceptance. Diagnostics
must label this material synthetic and must not export policy values or secrets.

History is append-only: exact equivalent ID/version reuse replays the original;
conflicting reuse retains it without change. Replacement and rollback append new
versions with later effective times and explicit predecessor references. A
rollback also identifies its historical source and must reproduce that source's
values and schema. Historical decision references remain exact tenant/ID/version
tuples. The synthetic resolver requires trusted tenant context, authorization,
fresh evidence, and a single exact match; it returns no default on ambiguity,
malformed references, missing versions, or foreign tenants. The AI evaluator also
matches the requested tenant and snapshot ID, requires a positive resolved policy
version, and retains that exact tuple in resolved decisions. The request carries
no expected version. It returns a redacted unavailable outcome for unresolved,
stale, invalid, or foreign snapshots. The production snapshot
provider remains unavailable.

Live persistence can be designed only after accepted data-class protection
evidence and independently witnessed native isolation proof are available. Future
evidence must bind the exact provider/candidate, trusted tenant-derived physical
partition or namespace, supported write/read paths and ACLs, protection and key
custody, redaction, retention/hold precedence, export/deletion, backup propagation,
restoration and crypto-erasure obligations. Native adversarial tests must prove
persisted end-state for two tenants, including cross-tenant writes/reads, replay,
projections, caches, exports, backups and recovery. Missing, stale, failing, or
unavailable proof or storage must deny before mutation. Prefixes, filters,
admission markers, flags, and these synthetic tests cannot satisfy that gate.

The evaluator now fails closed: a missing, invalid or stale AI policy snapshot is
`Blocked` (`policy-snapshot-unavailable`) instead of routed to approval, and early
routes no longer echo the requested snapshot id. This outcome change is intentional
and signed off by the story owner; no code or contract consumed the old reason codes.
The persistence gate covers every command the audit map classifies as
`tenant-policy-mutation`, enforced by a guard test.

Any future domain write must use Hexalith.EventStore; queries, projections, read
models, and cursors must use the domain-service SDK seams. There is no history
migration or sibling change in this work. Atomic command admission and mutation /
audit commitment remain Stories 1.7 and 1.8. Completion also requires the native
persisted-end-state evidence and qualification in
[`story-evidence-integrity.md`](../story-evidence-integrity.md).
