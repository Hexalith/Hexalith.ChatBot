# PRD Quality Review — Hexalith.ChatBot Recommended Update v2 (2026-09-14)

## Overall verdict

**Conditionally ready after one High release-gate fix.** The autofix closes three prior High findings completely and closes the M0/M1 portion of the fourth: the command-profile map is exhaustive, inbound authenticity now has a query and two-person reprocessing contract, and the correction impact manifest is governed as an M0 durable record. The remaining High is a new edge created by that qualification fix: exact detector/classifier qualification is mandatory through M1 but is omitted from the authoritative M2 revalidation path, so the production gate could accept an expired or drifted safety artifact.

## Decision-readiness — adequate

The product decisions, trade-offs, owners, evidence gates, disable conditions, and permitted claims are stated directly. §Current Release Status and §Minimum Release Slice continue to make A5, A6, A10, A11, and A13 uncertainty explicit rather than treating missing evidence as success. The added A9a qualification records give TaskIntentDetector and ActionRiskClassifier exact artifacts, datasets, formulas, thresholds, approvers, expiry, and invalidation behavior for M0 and M1.

The last release-decision gap is at M2: the sole gate table expressly revalidates several inherited records but not the new A9a records. A smaller existing gate-status vocabulary mismatch also remains.

### Findings

- **high** Detector/classifier qualification falls out of the M2 production gate (§Current Release Status; §Minimum Release Slice M1/M2 rows and closing dependency-order paragraph; A9a; `addendum.md` §Detector and Classifier Qualification Records and §Increment Gate Record Contract) — M0 and M1 now require `approved-current` records for the exact deployed TaskIntentDetector and ActionRiskClassifier, and the appendix invalidates them on expiry, dataset drift, or artifact/version change. But the authoritative M2 row revalidates only A5/A6/A13/A11-M1, the dependency-order paragraph repeats that exact list, and the gate-record appendix summarizes A5/A6/A13/A11/A10 without A9a. Thus M2 can nominally pass after the classifier or detector record expires or after the deployed artifact drifts. *Fix:* require exact deployed A9a detector/classifier records to remain `approved-current` at M2, add them to the M2 evidence and disable condition, and include A9a in the gate-record summary/reopen semantics.
- **medium** Machine-evaluable gate status vocabulary still drifts (§Current Release Status, paragraph after the increment bullets; §Minimum Release Slice disable conditions; `addendum.md` §Increment Gate Record Contract) — the PRD calls the positive computed state `Current` while the exact enum is `approved-current`; the gate rows also use `failed` and `drifted` alongside enum values without saying whether these are evidence conditions that compute to `open`, `invalidated`, or another state. The contract does not define how `decision=rejected` computes. *Fix:* use the exact five-value enum everywhere and map rejected evidence, threshold failure, and drift explicitly to one computed status.

## Substance over theater — strong

The document's depth remains earned by its trust, multi-tenancy, cross-context ownership, and recovery obligations. Named protagonists drive concrete authorization, evidence, correction, audit, and operational decisions; the NFRs use product-specific thresholds and observable failures; and unsupported compliance, recovery, and production claims are explicitly blocked.

No substantive theater finding was identified.

## Strategic coherence — strong

The thesis remains clear: transform ordinary external project email into governed Project work, with AI constrained by the same authorization, approval, command, evidence, and audit boundaries as other actors. M0 proves the vertical UI loop, M1 proves governed cross-surface parity, and M2 proves production operability and recovery. Features, non-goals, success metrics, and counter-metrics follow that thesis.

The autofixes preserve this strategy. The new qualification records strengthen the trust bet; the remaining M2 omission is a gate-wiring defect, not a competing product direction.

## Done-ness clarity — adequate

The prior High implementation gaps are closed. The command catalog and pipeline map contain the same 74 unique stable commands with no omissions or duplicate mappings; multi-stage chat and outbound work use explicit successor commands. Authenticity review/reprocessing now names initiator, independent approver, evidence digest, revision, operation identity, successor behavior, query fields, and redaction. Correction-manifest persistence now carries retention, isolation, owner, and data-rights obligations.

One pre-existing lifecycle vocabulary mismatch remains for attachments.

### Findings

- **medium** Attachment state vocabulary conflicts with the authoritative workflow (FR34; §Shared Workflow Contract attachment-handling row) — FR34 calls `captured`, `pending`, `unavailable`, `rejected`, `unsafe`, `failed`, and `retryable` attachment states, while the authoritative workflow defines `PendingScan`, `Stored`, `Unsafe`, and `Failed`; capture/retry are events or commands, and unavailable/rejected have no transition. *Fix:* make FR34 use the canonical enum, or extend the transition table with the omitted states and exact terminal/retry rules.
- **medium** The glossary permits policy approval of a risky action (§Functional Requirements → Glossary, `Approval`; FR41; `addendum.md` §Risk Classifier) — `Approval` is defined as “a human or policy decision that permits ... a proposed risky action,” while FR41 and the classifier contract require a currently authenticated human for every boundary-crossing effect and say policy cannot downgrade that requirement. *Fix:* define approval as a human decision for approval-required actions; policy may classify, route, or deny, but cannot satisfy the human approval gate.

## Scope honesty — strong

The artifacts remain drafts, preserve the historical finalization only as history, and state that prose or file presence cannot close a gate. MVP exclusions, post-MVP channels and triggers, source-contract gaps, provisional recovery targets, and unsupported SLO rows are explicit and owned.

No scope-honesty finding was identified.

## Downstream usability — adequate

The authority map, stable identifiers, lifecycle tables, exhaustive command/profile map, traceability overview, acceptance guidance, and named journeys make the PRD highly source-extractable for UX, architecture, stories, security, compliance, and QA. The previously missing inbound-authenticity and correction-manifest contracts now round-trip cleanly across capability, workflow, governance, and NFR sections.

Two light mechanical issues remain in addition to the attachment vocabulary finding.

### Findings

- **medium** M0 accessibility scope is under-enumerated in its release slice (§Minimum Release Slice → Increment M0; §UI Surface Inventory; NFR60) — the M0 scope says WCAG applies to the M0 surfaces “enumerated in NFR60” but names only ambiguous-association review, AI-action approval, and Project conversation, omitting the new M0 inbound-authenticity review surface. NFR60 correctly includes authenticity review, so the local summary is contradictory. *Fix:* add inbound-authenticity review to the M0 scope bullet.

## Shape fit — strong

This remains the right shape for a multi-stakeholder, chain-top B2B orchestration PRD. Journeys carry actor context; capability and invariant statements live in the PRD; executable schemas and mechanisms live in the addendum; evidence remains in separate mutable artifacts. The density reflects real integration and governance complexity rather than template inflation.

No shape-fit finding was identified.

## Mechanical notes

- FR1–FR96 and NFR1–NFR70 remain contiguous and uniquely defined; all letter-suffixed definitions are unique.
- The stable mutator catalog and the addendum command-to-profile map each contain 74 commands; set comparison found no missing, extra, or duplicate mapping.
- All eight human journeys have named protagonists; the separate System Journey is intentionally actor/system oriented.
- Every inline `[ASSUMPTION ...]` tag resolves to A9a or A11. No live `[NOTE FOR PM]` or Open Questions section remains; unresolved release matters are represented by owned gates.
- **low** The Glossary still does not define the central capitalized term `Project`. Add its bounded-context and ownership meaning once to reduce ambiguity in extracted stories.
- Both normative artifacts consistently remain `status: draft`.

## Prior High verification

- Detector/classifier release qualification: **partially closed** for M0/M1; residual M2 High above.
- Closed command-pipeline coverage: **closed**; exhaustive 74-command one-profile mapping verified.
- Authenticity query and reprocessing authority: **closed**.
- Correction-impact-manifest governance: **closed**.

## Finding totals

- Critical: 0
- High: 1
- Medium: 4
- Low: 1
