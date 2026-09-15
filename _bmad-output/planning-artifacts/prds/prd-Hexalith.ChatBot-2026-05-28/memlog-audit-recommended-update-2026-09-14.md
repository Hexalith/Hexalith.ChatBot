---
title: Hexalith.ChatBot Recommended-Update Memlog Audit
status: complete
created: "2026-09-14"
updated: "2026-09-14"
---

# Memlog Audit — Recommended Update

Scope: all 50 append-only records in `.memlog.md` after the 2026-09-14 recommended update. YAML metadata is not a decision record.

Result: **39 captured in `prd.md`; 11 captured in `addendum.md`; 0 set aside.** “Captured in addendum” means the normative appendix is the primary executable expression; the PRD may also summarize or reference it.

| # | Memlog record | Classification | Canonical capture |
| --- | --- | --- | --- |
| 1 | Email-first governed B2B workspace | Captured in PRD | §Executive Summary; §Product Scope |
| 2 | One email-to-governed-action loop | Captured in PRD | §Executive Summary; §MVP |
| 3 | ChatBot orchestration and sibling source ownership | Captured in PRD | §Executive Summary; §Context Ownership |
| 4 | Deterministic evidence precedes inference; unresolved association fails to review | Captured in PRD | §Key Product Risks; §Shared Workflow Contract |
| 5 | Authorization and non-disclosure at every surface boundary | Captured in PRD | §Tenant Model; NFR1–NFR12 |
| 6 | AI is governed; unresolved work is refused; risky work needs a human | Captured in PRD | §System Journey; FR39–FR46; NFR16 |
| 7 | Controlled M365/Exchange first; external users remain Parties without a portal | Captured in PRD | §Increment M0; UJ3; §Microsoft 365 / Exchange Permission Constraints |
| 8 | One release in M0→M1→M2 order with an uncuttable safety floor | Captured in PRD | §Minimum Release Slice — Three Increments |
| 9 | M0 complete UI-only controlled vertical | Captured in PRD | §Increment M0 |
| 10 | M1 CLI/MCP parity, service clients, outbound, and tenant governance | Captured in PRD | §Increment M1; FR81–FR86 |
| 11 | M2 production operability | Captured in PRD | §Increment M2; recovery/operability NFRs |
| 12 | Governed interactive chat through FrontComposer/command gateway | Captured in PRD | §Vision; UJ1; S1a; FR28a–FR28f |
| 13 | Terminal Rejected/Failed and audit-linked correction | Captured in PRD | §Shared Workflow Contract; FR91a; NFR17a |
| 14 | Audit readiness and idempotency for every mutation | Captured in PRD | FR55, FR81a, FR90; NFR13–NFR15a |
| 15 | MVP exclusions preserve the governed email loop | Captured in PRD | §Out of scope for MVP; §B2B SaaS Non-Goals |
| 16 | Per-increment WCAG; CLI/MCP excluded from WCAG scope | Captured in PRD | NFR60–NFR64 |
| 17 | 2026-09-13 remediation authorization and reopen | Captured in PRD | Frontmatter `editHistory` |
| 18 | Low-risk subtypes only; six effect classes cannot be downgraded | Captured in addendum | §Risk Classifier; §Tenant Policy Schema |
| 19 | Deny-by-default AI allowlist with exactly two v1 commands | Captured in addendum | §Command Allowlist v1 |
| 20 | Atomic mutation/audit/idempotency durability boundary | Captured in addendum | §Shared Command Pipeline |
| 21 | Derived-store isolation proof at first introduction | Captured in PRD | Increment gates; FR55a; NFR9a |
| 22 | Governed interactive chat assigned to M1 | Captured in PRD | §Increment M1; S1a; FR28a–FR28f; NFR60 |
| 23 | M0 inbound-authenticity floor; advanced tuning in M1 | Captured in addendum | §Inbound Message Authenticity; §Tenant Policy Schema |
| 24 | `Skipped` is a terminal M0 state | Captured in PRD | §Shared Workflow Contract association matrix |
| 25 | Stable operation and decision-slot identities | Captured in addendum | §Idempotency Keys |
| 26 | Association, task-intent, and risk classifiers are separate versioned contracts | Captured in addendum | §Confidence Thresholds; §Task-Intent Detector; §Risk Classifier |
| 27 | “Subject” is framing; actionable work resolves to a Project | Captured in PRD | §Executive Summary; MVP non-goals |
| 28 | Conversations owns conversation identity/messages and Project assignment | Captured in PRD | §Context Ownership; A13 |
| 29 | Story-completion and operational recovery evidence cannot substitute | Captured in PRD | A10; NFR54a; NFR65a |
| 30 | General user uploads deferred post-MVP | Captured in PRD | MVP non-goals; §Growth Features |
| 31 | M0 preview, M1 governed pilot, M2 release candidate | Captured in PRD | §Minimum Release Slice gate table |
| 32 | A6 pre-pilot data-protection blocker | Captured in PRD | §Current Release Status; A6; NFR49a |
| 33 | `IdentityEvolved` is unaccepted until producer approval | Captured in addendum | §ID Evolution Contract |
| 34 | 2026-09-14 sibling re-check and bounded compatibility result | Captured in PRD | §Project Classification; A13; source-manifest reference |
| 35 | A13 owner-accepted cross-context stop-ship gate | Captured in PRD | §Current Release Status; increment gates; A13 |
| 36 | A6 requires runtime data-protection and erasure evidence | Captured in PRD | A6; §Compliance Requirements; NFR49a |
| 37 | Per-stream audit hash links plus signed tenant checkpoints | Captured in addendum | §Shared Command Pipeline |
| 38 | `Project.AppendConversationMessage` maps to Conversations v1 and remains A13-blocked | Captured in addendum | §AI allowlist executable contract mapping |
| 39 | Final safety-contract remediation bundle | Captured in PRD | §Workflow and Operation Contracts; FR55/FR75/FR81a; reliability/audit NFRs |
| 40 | Structure/prose polish and normative authority map | Captured in PRD | Frontmatter `editHistory`; §Current Release Status; §Authority Map |
| 41 | Activation-pending Epic 12 architecture and A10 separation | Captured in addendum | §Recovery Qualification |
| 42 | Exhaustive operations/retries/chat races/A11 evidence/source traceability | Captured in PRD | §Shared Workflow Contract; §Command and Query Contracts; A11; NFR15a |
| 43 | Closed FR74 controls/admin roles and sole current A13 reconciliation | Captured in PRD | FR74–FR75g; A13; §Shared Workflow Contract |
| 44 | M0 governance bootstrap and per-family retry profiles | Captured in PRD | §Increment M0; §Shared Workflow Contract; FR65; NFR18 |
| 45 | Hold-release-only erasure retry successor | Captured in PRD | §Shared Workflow Contract data erasure family |
| 46 | PRD finalized before the current reopen | Captured in PRD | Frontmatter `finalizedAt` and historical `editHistory` entry |
| 47 | Current “do recommended” authorization and stable-ID constraint | Captured in PRD | Frontmatter current `editHistory`; stable FR/NFR IDs retained |
| 48 | A11 split into M1 metric and M2 SLO qualification | Captured in PRD | §Current Release Status; increment gates; A11; NFR42a |
| 49 | Approval TTL defaults and immediate material-drift invalidation | Captured in addendum | §Approval Freshness |
| 50 | Four-Critical/eleven-High remediation set applied with gates open | Captured in PRD | Frontmatter current `editHistory`; §Current Release Status; updated workflow/FR/NFR contracts |

## Exceptions and supersession notes

- **Entry 46 is historical, not current status.** It records the completed prior finalization and is retained in frontmatter. The current update intentionally reopened both artifacts as `draft`. The §Current Release Status sentence still says “Artifact: final as of 2026-09-14” and must be updated before this run is finalized.
- **Entry 48 intentionally supersedes the prior A11-is-M2-only interpretation.** The current PRD consistently applies A11-M1 to the governed-pilot gate and A11-M2 to production SLO qualification; the earlier state remains only in history.
- **Entry 50 records applied document remediation, not reviewer acceptance or external evidence.** The current PRD correctly keeps A5, A6, A10, A11-M1, A11-M2, and A13 open pending their named evidence and re-validation.

No memlog decision is omitted from both normative artifacts, and no entry requires placement in a downstream architecture or UX addendum.
