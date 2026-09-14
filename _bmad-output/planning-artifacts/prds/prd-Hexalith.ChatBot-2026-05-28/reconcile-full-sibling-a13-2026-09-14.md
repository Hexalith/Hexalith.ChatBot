---
title: Full Sibling and A13 Reconciliation
status: current
created: "2026-09-14"
scope: "Nine-context PRD dependency baseline and complete A13 closure state"
supersedesForGateStatus:
  - reconcile-sibling-contexts-2026-09-14.md
incorporatesNarrowVerification:
  - reconcile-current-sibling-contracts-2026-09-14.md
---

# Full Sibling and A13 Reconciliation — 2026-09-14

## Verdict

The PRD wording is reconciled across all nine sibling contexts, but A13 remains open and blocks M0. Current repositories do not yet provide one owner-accepted executable bundle for Conversations append/concurrency, conversation-to-Project assignment, Projects/Tenants/EventStore authority, atomic canonical mutation audit, or supported-write-path ACL/fencing. The exact revisions and consumed-file hashes are authoritative in `source-manifest.md`.

## Current context results

| Context | Revision | Current PRD dependency result |
| --- | --- | --- |
| Hexalith.Conversations | `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | Owns messages and conversation-to-Project assignment. The v1 append DTO/client/event shape is mapped, but no production handler, caller expected revision, lifetime outcome retention, or accepted atomic-audit mapping exists. **A13 open.** |
| Hexalith.Projects | `4f05a352edd67c4d5595913ee584539c1948dd58` | Owns Project identity, lifecycle, resource access, and authorization. Current reads are authorization-filtered and safely denied; the exact ChatBot-role-to-Projects resource-grant contract is not owner-accepted. **A13 open.** |
| Hexalith.Folders | `b409b03f9c3e35bc65c80ed03422c540e6201b20` | Reviewed OpenAPI supports governed folder/file metadata operations. Concurrent generated-client edits do not change the consumed contract hash. Runtime data-protection proof remains governed by A6. |
| Hexalith.Parties | `14d249fde316b0002aec84351d7a7cdf953d1d30` | Owns Party identity. Production key custody/data-protection evidence is absent under A6; revocation-sensitive authority must use current owner/gateway evidence under A13. |
| Hexalith.Tenants | `ff43dc941b01d4a68070f92dde0536f5ab1ef4df` | Owns tenant membership and role facts. Its closed role vocabulary does not natively grant ChatBot roles; the deny-by-default mapping in the PRD awaits owner acceptance. **A13 open.** |
| Hexalith.EventStore | `7579b858ecd30f0273bec3ab3e8c88f93232f0a5` | Can co-commit an aggregate-local event batch but not the full terminal idempotency/audit unit. No generic canonical audit/checkpoint contract or proven storage fencing exists. Payload-protection seams are not runtime A6 evidence. **A6 and A13 open.** |
| Hexalith.FrontComposer | `b0ad2fb69bcf5e7aadd7b388d25415d0fba876d5` | Compatible governed UI host/progress transport; ChatBot owns the governed-composer lifecycle and authorization. |
| Hexalith.Memories | `d99bc96371afbf55f8f37cd812c9e6cedba15b1d` | Optional post-MVP/M2 dependency only; it does not expand M0/M1 authority or close A6. |
| Hexalith.Commons | `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6` | Shared tenant-access evaluator is compatible as a mechanism; ChatBot retains its closed role/permission vocabulary and owner checks. |

## A13 closure bundle

A13 closes only when one exact candidate records all of the following with owner acceptance and passing contract tests:

1. Conversations production append handler plus versioned `AppendMessageCommand`/`MessageAppended` mapping.
2. Owner-accepted lifetime duplicate barrier and expected-revision or equivalent append concurrency rule.
3. Conversations ownership and executable contract for conversation-to-Project assignment/reassignment.
4. Closed ChatBot-to-Tenants, Projects resource-grant, and EventStore permission mapping with current gateway authorization.
5. Named transactional owner for domain event, durable idempotency, applied policy/approval references, and canonical audit-envelope co-commit.
6. Supported actor-only write path, storage ACLs, ETag/first-write fencing, and passing concurrent-write, fork, reorder, checkpoint-rebuild, and recovery tests.

Approvers are the System Architect and Conversations, Projects, Tenants, and EventStore owners; Security validates authority and fencing. Missing, expired, changed, or partially accepted evidence leaves A13 open, disables live AI and pilot onboarding, and prohibits a tamper-evident-completeness claim.

## Artifact authority

This file is the single current full-sibling/A13 gate reconciliation. `reconcile-sibling-contexts-2026-09-14.md` is the historical initial five-gap extraction; its resolved wording findings and old A8 shorthand are not current gate authority. `reconcile-current-sibling-contracts-2026-09-14.md` remains the narrow technical H4/H12 evidence incorporated here. A8 governs allowlist membership only; executable producer, authority, audit, concurrency, and fencing acceptance are A13.
