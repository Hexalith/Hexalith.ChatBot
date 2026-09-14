---
title: Hexalith.ChatBot PRD Source Manifest
status: current
created: "2026-09-14"
updated: "2026-09-14"
workspaceRevision: f0ba70ed9c76a88d0204e1d228562b476742c1b5
---

# Source Manifest

This manifest makes the brownfield evidence used by the 2026-09-14 PRD update reproducible. Repository-relative paths are authoritative; hashes identify local review inputs that were not committed at update time.

## Direct product inputs

| Source | Revision or SHA-256 | Role |
| --- | --- | --- |
| `_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md` | commit `eceaa02ac63769eba580b8f29b8b23e048003a16`; SHA-256 `d890417042c0329f168306936ae9b1004f2aa40c22f3e1d2845579db9b1ffd63` | Original product intent and user-voice anchor |
| `_bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md` | commit `bab0218c14f5d5c4bc9513014535c5f22e84cde7`; SHA-256 `72f06150a77f6d51e1f52a4d7a85b58dd0459b0badfe91aa61e7466a6e1acaf1` | Recovery evidence-integrity contract |
| `orient-extract.md` | SHA-256 `e347e94774e82596bbc5a2a2773191632da705190d9bba0c105aa00a76ce8228` | 2026-09-13 PRD orientation extract |
| `review-adversarial-general.md` | SHA-256 `cac9f929eff9d4a800cd1e9163379b4d14e0de5075d28510634ef0e58bf67a18` | 2026-09-13 adversarial findings |
| `review-rubric.md` | SHA-256 `b569edf82e2c2d4c8120d84cd2dce3fe013e1b00c22e2e1c2e6885975fd85e1a` | 2026-09-13 rubric findings |
| `validation-report.md` | SHA-256 `1aa8295437bcc700cdcb67b10459175b85b9cdedbc1bfac471e35ef3542f7dae` | 2026-09-13 synthesized validation input |
| `.memlog.md` | append-only; initialized 2026-09-13 | Canonical decision and override memory |

## Sibling bounded-context revisions inspected

| Context | Revision | PRD dependency role |
| --- | --- | --- |
| Hexalith.Conversations | `596cee6fa5ae12a7ff6e8ac35960f60863b55a27` | Conversation identity and messages |
| Hexalith.Projects | `4f05a352edd67c4d5595913ee584539c1948dd58` | Project identity, membership, and project boundary |
| Hexalith.Folders | `b409b03f9c3e35bc65c80ed03422c540e6201b20` | Governed file and folder records |
| Hexalith.Parties | `bfc15cc15b6556c5f97ae3d06bdefdd809d3b666` | Party identity and external participants |
| Hexalith.Tenants | `ff43dc941b01d4a68070f92dde0536f5ab1ef4df` | Tenant identity and tenant policy boundary |
| Hexalith.EventStore | `4502913cafbb6151a17544923b5b1ba76ea5e5ec` | Command/event durability and canonical audit envelope |
| Hexalith.FrontComposer | `b0ad2fb69bcf5e7aadd7b388d25415d0fba876d5` | Governed UI shell and interactive chat surface |
| Hexalith.Memories | `d99bc96371afbf55f8f37cd812c9e6cedba15b1d` | Optional AI memory dependency |
| Hexalith.Commons | `19d7d4d6b21160557b7449f55a0ad0f55e6d7dc6` | Shared contracts and primitives |

These revisions identify the checked-out dependency baseline; they do not by themselves prove acceptance of every proposed cross-context contract. `IdentityEvolved` remains unaccepted until the producer contracts named in the addendum are approved.

## Re-check rule

The System Architect opens a re-check within five business days when a listed context changes a consumed command/event schema, authorization or identifier semantics, integration topology, or referenced RBAC rule. The outcome is appended to `.memlog.md` and updates this manifest. Missing source artifacts or inaccessible revisions are recorded as blockers rather than inferred.
