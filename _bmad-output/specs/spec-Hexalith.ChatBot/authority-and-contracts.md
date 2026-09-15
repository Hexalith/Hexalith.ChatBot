# Authority and Contract Map

This companion is a navigation and preservation map. It does not restate detailed tables as a new authority. If a summary below differs from its named authority, the named authority prevails; correct the summary.

## Authority order

| Surface | Authority | Preservation rule |
| --- | --- | --- |
| Product scope, requirements, lifecycle, public operations, ownership, increments, evidence, disable conditions, and permitted claims | Finalized `prd.md` plus approved `addendum.md` | Product decisions change only in the PRD package. |
| Exact consumed revisions and hashes | `source-manifest.md` | Compatibility and pin currency do not prove producer acceptance or readiness. |
| Mutable qualification state | `qualification-evidence.md` | Missing, stale, failing, or candidate-mismatched evidence remains open or `unsupported`. |
| Current nine-context A13 status | `reconcile-full-sibling-a13-2026-09-14.md` | This is the sole current A13 reconciliation; narrower or historical checks cannot close A13. |
| Implementation consistency | Final `architecture-chatbot-2026-09-14/ARCHITECTURE-SPINE.md` | AD-1 through AD-19 are stable and binding. |
| Detailed architectural rationale, stack, structure, and enforcement | `architecture.md` | It is subordinate to the PRD package and architecture spine where wording conflicts. |

Planning finality establishes no implementation, pilot, qualification, production, or release readiness.

## Open release gates

| Gate | State | Binding effect |
| --- | --- | --- |
| A5 — live-AI provider | OPEN | Live AI and M0/M1 onboarding remain blocked without the approved provider contract and negative evidence. |
| A6 — runtime data protection | OPEN | Pilot data/PII persistence, onboarding, GDPR-satisfaction, and tamper-evident-completeness claims remain blocked without the approved data-class contract and witnessed runtime evidence. |
| A9a — detector/classifier qualification | OPEN | No `approved-current` exact-artifact record exists; the affected detector or classifier remains disabled, and first use plus later-increment revalidation are blocked. |
| A13 — owner execution, authority, audit, and fencing | OPEN | The indivisible six-part owner bundle for the exact candidate has not been accepted; the complete M0 loop, live AI, onboarding, and tamper-evidence claims remain blocked. |
| A11-M1 — mandatory M1 metric qualification | OPEN | No current machine-readable bundle freezes and evidences the required M1 measurement contract; the governed cross-surface pilot claim remains blocked. |
| A10 — recovery qualification | OPEN / provisional | No exact candidate has a fresh qualifying evidence bundle from hosted controlled-loss and RTO-capability testing; M2 production/release-candidate claims remain blocked. |
| A11-M2 — exact-candidate SLO qualification | OPEN / unsupported | Every current SLO row is unsupported with no selected candidate; M2 and each narrower SLO-backed production claim remain blocked. |

Closing any one gate does not compensate for another open gate or any other missing item in the PRD increment table.

## Stable architecture decisions

The titles below are index labels only; the complete rule under each identifier in the architecture spine is binding.

| ID | Stable title |
| --- | --- |
| AD-1 | One inward dependency direction |
| AD-2 | One atomic mutation spine |
| AD-3 | Current owner authorization and closed policy |
| AD-4 | Family state machines, lifetime identity, and Retry Profile v1 |
| AD-5 | Cross-context sovereignty |
| AD-6 | Command-created M0 governance bootstrap |
| AD-7 | Independent classifiers and deny-by-default AI authority |
| AD-8 | Inbound authenticity precedes outbound authority |
| AD-9 | Derived-state isolation and correction |
| AD-10 | Canonical audit, investigation views, and data protection |
| AD-11 | Disjoint recovery evidence channels |
| AD-12 | Qualification gates remain external to architecture completion |
| AD-13 | Replay-safe composition |
| AD-14 | Material dependency changes trigger bounded recheck |
| AD-15 | Durable cross-context choreography, never distributed dual-write |
| AD-16 | Canonical parity and governed streaming |
| AD-17 | EventStore domain host and environment boundary |
| AD-18 | OpenAPI Contract Spine is the HTTP wire authority |
| AD-19 | Durable runtime controls and fair operations |

These identifiers, titles, and binding meanings are not available for reuse, renumbering, weakening, or local override. The Retry Profile row-count discrepancy recorded in `planning-follow-up.md` requires an owner correction without changing AD-4's identity or substantive rule.

## Binding contract locations

| Contract | Sole detailed authority | Required preservation |
| --- | --- | --- |
| M0 → M1 → M2 lifecycle and release claims | PRD §Minimum Release Slice — Three Increments | Strict dependency order; M0 is controlled pilot preview, M1 governed cross-surface pilot, M2 first production/release candidate. |
| Workflow family states and transitions | PRD §Shared Workflow Contract | Exact family-specific states, commands, guards, events, terminal/successor rules, expected revision, identity, and atomic audit. Do not collapse families into one enum. |
| Public ChatBot operations and queries | PRD §Command and Query Contracts | Stable IDs are exhaustive; architecture may map or compose them but may not rename or invent owner targets. |
| Command spine and atomicity | PRD FR81a/NFR15a; addendum §Shared Command Pipeline; AD-2 | Every mutation origin enters one ordered gateway and co-commits the event, lifetime terminal idempotency result, policy/approval references, and canonical envelope, or nothing commits. |
| Closed command admission | Addendum §Shared Command Pipeline; AD-2 | `CommandGateway` selects exactly one closed-map profile. Unknown, absent, or duplicate mappings reject; no origin may select or alter a profile. `ai-read-v1`, `ai-effect-v1`, and `projection-delivery-v1` execute only their mandated stages. |
| Idempotency and concurrency | Addendum §Idempotency Keys; PRD NFR13/NFR13a/FR90; AD-4 | Lifetime `operation_id` and `decision_slot_id` outcomes; semantic conflicts are typed; expected revision independently resolves races. |
| Retry Profile v1 | Addendum §Retry Profiles; PRD NFR18; AD-4 | Each product-authoritative table row defines retryability, terminality, retry limits, full-jitter backoff, exhaustion behavior, the recovery owner and command, and the successor form. The current addendum has 12 data rows; no row may be dropped because AD-4 says eleven. |
| Authorization and administration | PRD §Context Ownership, §Permission Model, §RBAC Matrix, §Owner-authority mapping, FR75a–FR75g; AD-3 | Current owner authority and the most restrictive result win; no global/admin/debug, machine-role, claims-only, or mirror-based bypass. |
| M0 governance bootstrap | PRD §Shared Workflow Contract and Service Client Permissions; AD-6 | Two distinct current Tenants owners use `GrantChatBotAdminRole`, `UpdateTenantPolicy`, and `GrantServiceClientPermission`; four enumerated M0 client classes; no direct data/config seed. |
| Service-client permissions | PRD §Service Client Permissions and command catalog; AD-3/AD-6 | Preserve all seven M0/M1 client classes, exact scopes and operations, delegated-human evidence, least-privilege expiries, revocation, and deny-by-default behavior. The four M0 bootstrap grants do not authorize the three M1 clients. |
| AI classification and execution | Addendum §Confidence Thresholds, §Task-Intent Detector, §Risk Classifier, §Command Allowlist v0/v1; AD-7 | Only `low-risk` and `approval-required` are successful determinate classes; `denied` and `unsupported` remain pre-classification dispositions, and the six mandatory-approval effects never downgrade. `classifier-indeterminate` creates no proposal, domain/idempotency success, approval action, or effect; `classifier-unavailable` may appear only as its non-canonical safe reason. The gateway/attempt seam owns the redacted non-mutating terminal attempt and successor link. The attempt records the original `operation_id`, correlation, classifier version/input tuple, reason, and remediation; its ledger is the only durable record, cannot authorize continuation or act as domain idempotency state, and links a fresh independently classified operation by immutable predecessor. The original attempt cannot be resumed, reinterpreted, or approved. |
| Inbound/outbound authority | Addendum §Inbound Message Authenticity and authority mapping; AD-8 | `strict|paranoid` inbound policy; exactly five outbound authority classes with execution-time authority/approval revalidation. |
| Audit and data protection | PRD NFR15a/NFR49a/NFR50a; addendum §Shared Command Pipeline; AD-10 | `100%` canonical mutation-envelope atomicity is an invariant, not a current claim. Auditable attempts and `>=99.5%` M2 investigation-view availability are separate. Unavailable durable attempt audit returns redacted `AuditUnavailable`, exposes no protected data, writes no domain/idempotency state, signals the incident, and cannot be replaced by telemetry. A6/A13 remain required. |
| Derived state and correction | PRD §Data Governance Surface, FR55a/FR91a, NFR9a/NFR17a; AD-9 | The ChatBot correction aggregate owns immutable manifest membership and `Correcting | CorrectionDelayed | Corrected`. Its frozen manifest covers every ChatBot-derived store, every affected Conversations/Folders record and index, approved or executed AI action, appended message, task-intent conversion, sent mail, external/tool effect, file disclosure, and required irreversible-effect disposition. Each item has a stable correction/owner/resource/version-or-digest/outcome identity. Only A13-mapped owner adapters/actors may submit authenticated repair/rebuild acknowledgement or `contained`, `compensation-required`, or `cannot-repair`; coordinators/projections cannot mutate membership or self-acknowledge. All affected source/destination AI context stays blocked until every item completes and the workflow reaches `Corrected`. |
| ID evolution / A12 | Addendum §ID Evolution Contract; PRD A12; AD-14 | `IdentityEvolved` is proposed only. Until every producer accepts the complete versioned contract, original audit IDs remain immutable, unresolved current identity fails closed to authorized review, and automatic cross-context identity migration is not binding. |
| Cross-context ownership and execution | PRD §Context Ownership; A13 reconciliation; AD-5/AD-14/AD-15 | Owner contexts retain source truth; ChatBot records orchestration intent/status; owner commands/events drive effects; no dual-write; every mapping remains A13-gated. |
| Replay | Addendum §Replay Isolation; PRD FR95a/NFR69; AD-13 | Separate test tenant/composition, no production credentials, replaced effectful adapters, deny egress, and before/after production-resource invariance. |
| Recovery evidence | PRD NFR54a/NFR65a/A10; addendum §Recovery Qualification; qualification evidence; AD-11 | Diagnostics, exact-candidate story completion, and retained A10 evidence are disjoint; Epic 12 remains pending activation; no current bundle closes A10. |
| A9a qualification | PRD A9a; qualification evidence; AD-7/AD-12 | Separate exact-artifact association, task-intent, and action-risk partitions gate M0 first use and M1/M2 revalidation independently of A5/A6/A13 and both A11-M1/A11-M2 milestones. |
| A11-M1 qualification | PRD A11-M1 and measurable outcomes; addendum §Operating Baselines; qualification evidence; AD-12/AD-19 | One independently machine-readable bundle freezes and evidences definitions, denominators, supported mix, provisional targets, minimum samples/windows, sources, owners, and pass/fail rules for SM8, SM16, SM-C3, and SM-C5, with SM12/SM15 counts. Any stale, partial, mismatched, historical, unverifiable, or failed bundle blocks M1. |
| A11-M2 qualification | PRD A11-M2 and measurable outcomes; addendum §Operating Baselines; qualification evidence; AD-12/AD-19 | Every declared SLO requires its complete exact-candidate tuple plus the pilot baseline, recalibration, supported mix, catalog-drift proof, metric families, dashboard states, alert routing, and burn evidence. Any incomplete row is `unsupported` and blocks M2 and its narrower claim; A11-M1 and A11-M2 cannot substitute for each other. |
| Increment gate admission | Addendum §Increment Gate Record Contract; AD-12 | Release Governance publishes one immutable `gate_set_id` per candidate/increment over every required record. Consumers recompute status at use; incomplete, mixed-candidate, expired, invalidated, or superseded sets fail closed; CI/release/runtime use the same set; drift creates a new set. |
| Measurable outcomes | PRD §Measurable Outcomes and increment table | Preserve SM1–SM16 and SM-C1–SM-C5 with their formulas, thresholds, diagnostic/gating roles, owners, increments, and counter-metric coupling; story counts are not a substitute. |
| Increment UI and accessibility | PRD §UI Surface Inventory and NFR60–NFR64 | Preserve M0 S1–S3, M1 S1a/S4–S7, and M2 S8–S10. Each delivered increment requires WCAG 2.2 AA evidence from automated, keyboard-only, and screen-reader checks on its in-scope UI surfaces; CLI/MCP are outside NFR60. |
| HTTP and surface parity | PRD FR28a–FR28f/FR81a–FR86; AD-16/AD-18 | OpenAPI is sole HTTP wire authority; typed clients normalize semantic commands; UI/CLI/MCP outcomes match while origin remains immutable. |

## Cross-context ownership

| Context | Sovereign ownership | ChatBot boundary |
| --- | --- | --- |
| ChatBot | Association decisions, governed AI/workflow orchestration, application permission vocabulary, policy/approval snapshots, lifecycle, queues, investigation views, and other PRD-enumerated derived records | Orchestrates owner effects without absorbing source records. |
| Mail integration | Message capture, provider headers, attachments, delivery state, and Microsoft 365/Exchange synchronization concerns | Supplies authenticated mailbox evidence and delivery outcomes; mailbox permission never grants Project authority. |
| Projects | Project identity, lifecycle, membership/access, and Project authorization | Current Project grant is required; a ChatBot role or identifier is not authority. |
| Conversations | Conversation identity, messages, append/history, and conversation-to-Project assignment/reassignment | The legacy product ID maps to Conversations; executable append/assignment remains A13-blocked. |
| Parties | Internal and external Party identity | ChatBot stores stable Party IDs and uses current owner evidence; A6 governs regulated-PII runtime proof. |
| Folders | Governed folders, attachments, file access, and metadata | ChatBot uses opaque tenant-scoped IDs and owner authorization; A6 governs protection evidence. |
| Tenants | Tenant facts, boundaries, membership, tenant policy/authorization context, and native roles | ChatBot owns its closed application grants/snapshots; exact mapping remains A13-blocked. |
| EventStore | Current aggregate-local command/event durability | Ownership of the proposed atomic event/idempotency/policy/audit boundary—or an approved alternative—plus the supported path, ACLs, and fencing remain A13-blocked. |
| FrontComposer | Governed UI shell and progress transport | ChatBot owns composer lifecycle and authorization; progress is advisory and requires authorized re-query. |
| Memories | Optional AI memory/vector/graph capability | M2/post-MVP only; no M0/M1 authority; first-use isolation and A5/A6 qualification required. |
| Commons | Shared access-evaluation mechanisms and primitives | Mechanisms cannot broaden ChatBot permissions or replace current owner checks. |
