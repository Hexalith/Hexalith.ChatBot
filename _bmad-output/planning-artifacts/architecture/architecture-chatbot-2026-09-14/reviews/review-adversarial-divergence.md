---
title: Adversarial Two-Compliant-Units Divergence Review
date: '2026-09-14'
reviewedArtifact: ../ARCHITECTURE-SPINE.md
reviewedCompanion: ../../../architecture.md
candidateSha256: 6c99ff39c87e1d76a78f2f80b05f748a328624f453ea866d5d1ba6df24e3f6d4
candidateStatus: final
companionSha256: b421a3bbd79803a1992a1aa67fc5e030cd30522988ced0f9ecea8d1807611485
lens: two-compliant-units-divergence
reviewedLines: 555
reviewedCompanionLines: 1185
result: pass
blockingFindings: 0
advisoryFindings: 0
recommendedRemediations: 0
---

# Adversarial Two-Compliant-Units Divergence Review

## Final status/hash confirmation and verdict

The current 1,185-line companion `architecture.md`, SHA-256
`b421a3bbd79803a1992a1aa67fc5e030cd30522988ced0f9ecea8d1807611485`, and the current 555-line
`ARCHITECTURE-SPINE.md`, SHA-256 `6c99ff39c87e1d76a78f2f80b05f748a328624f453ea866d5d1ba6df24e3f6d4`,
were rechecked after the final spine frontmatter transition from `status: draft` to `status: final`. That status-only
change establishes architecture-document finality, not implementation or release readiness. For every AD and its
companion restatement, the reviewer had attempted
to construct two independent implementations that could both quote the architecture as compliant while producing
incompatible command, lifecycle, retry, authorization, state, audit, bootstrap, ownership, hosting, transport,
scheduling, replay, recovery, evidence, or parity behavior. Citations use `Companion:Lx` and `Spine:Lx`.

**PASS.** There are zero blocking findings, zero advisory findings, and zero recommended remediations. D-1 through
D-6 and the editorial E-1 regression are resolved. A5, A6, A10, A11, and A13 remain open exactly as required; this
review makes no implementation, qualification, deployment, or release-readiness claim.

## Tier 1 — Active findings

None.

## Tier 2 — Remediation confirmation history

| Prior finding | Final evidence | Disposition |
| --- | --- | --- |
| D-1 / editorial E-1 — contradictory admin-read audit scope | The companion authorization and availability rules require every tenant-admin dashboard read and prohibit adapter-specific aggregation (`Companion:L464-L465`, `Companion:L481-L484`); its implementation table uses that same every-read population (`Companion:L865`), matching the spine (`Spine:L115-L116`, `Spine:L252-L254`). | **Resolved.** |
| D-2 — owner response/event authority | AD-15 makes persisted owner event/revision authoritative, constrains synchronous advancement to the same committed effect, normalizes response/event to one deterministic identity, and requires each A13 mapping to name its signal/formula (`Spine:L345-L352`). | **Resolved; owner execution remains A13-gated.** |
| D-3 — parity record versus immutable origin | AD-16 separates the canonical semantic tuple from immutable surface origin and directs the oracle to compare each correctly (`Spine:L358-L363`). | **Resolved.** |
| D-4 — isolated replay versus production fingerprints | AD-13 assigns fingerprinting to a separate gate-owned read-only verifier and binds its versioned inventory, digest, exclusions, equality, and failure rules (`Spine:L316-L324`). | **Resolved.** |
| D-5 — mandatory versus optional auditable attempt | AD-7 makes the classifier-unavailable attempt mandatory; AD-3 defines fail-closed audit-outage behavior; AD-10 applies the same path to its complete named population (`Spine:L120-L124`, `Spine:L190-L192`, `Spine:L252-L254`). | **Resolved.** |
| D-6 — handwritten HTTP contract outside the sole authority | AD-18 now permits handwritten code to replace generator machinery only, requires every HTTP route/DTO/error/version to remain in OpenAPI and covered by drift/conformance checks, and states that no HTTP wire-authority exception exists (`Spine:L392-L401`). | **Resolved.** |

## Tier 3 — Recovery Stack split confirmation

The Stack now names three identities without conflation:

- current general CI/release Dapr CLI `1.18.0` plus checksum and sidecar runtime `1.18.0`, both explicitly
  non-qualifying (`Spine:L441-L442`);
- the accepted Epic 12 `recovery-primary` target, CLI `1.18.2`, runtime `1.18.4`, archive checksum, and
  `activation: pending` (`Spine:L443`); and
- the actual current recovery-job wiring, which is explicitly identified as the mismatching general `1.18.0/1.18.0`
  pair and barred from completion-authority evidence until alignment and independent activation (`Spine:L455-L456`).

The following sentence independently preserves A10: even an activated completion lane cannot satisfy A10 without the
separate fresh controlled-loss/full-window evidence required by AD-11 (`Spine:L456-L457`). Two teams cannot treat the
current general wiring, the accepted target, and a qualifying A10 candidate as interchangeable.

## Tier 4 — Intentionally gated detail, not findings

- **A5:** provider/region/negative evidence is absent, so live AI and M0/M1 onboarding remain blocked
  (`Companion:L64`, `Spine:L536`).
- **A6:** the data-class, custody, protection/erasure, backup, export/delete, and witnessed runtime evidence remains absent
  (`Companion:L65`, `Spine:L537`).
- **A13:** the indivisible owner execution/authority/audit/fencing bundle remains unaccepted and blocks the complete M0
  loop and every named dependent claim (`Companion:L66`, `Spine:L538`).
- **A10:** no qualifying fresh exact-candidate hosted controlled-loss/full-window bundle exists; its state remains
  `OPEN / provisional` (`Companion:L67`, `Spine:L539`).
- **A11:** every row remains `unsupported`, candidate `not-selected`, and the state remains `OPEN / unsupported`
  (`Companion:L68`, `Spine:L540`).

These are deliberately open evidence/acceptance boundaries, not license for divergent implementations. The release
ledger also says it is not the complete release gate and preserves the PRD increment table as authority
(`Spine:L530-L532`).

## Every-AD divergence ledger

| AD | Final two-unit divergence result | Disposition |
| --- | --- | --- |
| AD-1 — inward dependencies | Contracts, Client, adapters, Server/domain seams, pure aggregates, and durable cross-seam coordination have one dependency direction (`Spine:L80-L84`). | **Pass.** |
| AD-2 — atomic mutation spine | Admission order, envelope construction, co-commit target, prohibited bypasses, and unsupported current capabilities are explicit (`Spine:L86-L99`). | **Pass subject to open A13.** |
| AD-3 — authorization/policy | Closed owner mapping, cache/revocation bounds, safe defaults, mutation authority, every-read attempts, and audit-outage behavior converge across companion and spine (`Companion:L454-L484`, `Spine:L101-L126`). | **Pass.** |
| AD-4 — lifecycle/retry identity | Family transition authority, lifetime identities, Retry Profile v1 interpretation, stored-result replay, exhaustion state, and approval/version rules converge (`Spine:L128-L147`). | **Pass.** |
| AD-5 — cross-context sovereignty | All nine context owners, ChatBot ownership, stable references, event-consumed owner facts, and acceptance boundary are explicit (`Spine:L149-L163`). | **Pass.** |
| AD-6 — M0 bootstrap | Named commands, distinct principals, scopes/expiries, separation of duty, no implicit authority, and no direct-seed workaround converge (`Spine:L165-L180`). | **Pass subject to open A5/A6/A13.** |
| AD-7 — classifiers/AI authority | Classifier independence, failure dispositions, mandatory attempts, thresholds, allowlist separation, and A13 use gate converge (`Spine:L182-L210`). | **Pass.** |
| AD-8 — authenticity/outbound | Recorded evidence, strict/paranoid behavior, classifier separation, closed outbound modes, revalidation, and typed failures converge (`Spine:L212-L223`). | **Pass.** |
| AD-9 — derived state/correction | Immutable decisions, tenant-qualified physical addressing, projection authority, correction acknowledgements/deadlines, and AI/vector guards converge (`Spine:L225-L242`). | **Pass subject to open A6.** |
| AD-10 — audit/data protection | Canonical envelope, mandatory attempt population, completeness boundary, alerts, and qualification posture converge; the implementation table names every tenant-admin dashboard read (`Companion:L865`, `Spine:L244-L260`). | **Pass subject to open A6/A13.** |
| AD-11 — recovery evidence | Diagnostic and authority channels, stage failures, activation prerequisites, exact-candidate/freshness predicates, and invalid historical evidence converge (`Spine:L262-L281`). | **Pass subject to open A10.** |
| AD-12 — qualification | Strict increment order, non-substitutability, exact evidence tuples, A13 bundle, A11 derivation, and unsupported/current labels prevent readiness inference (`Spine:L283-L310`). | **Pass; gates remain open.** |
| AD-13 — replay composition | Separate replay/verifier principals and the exact versioned invariance manifest converge (`Spine:L312-L324`). | **Pass.** |
| AD-14 — dependency recheck | Material triggers, five-business-day deadline, blocking inaccessible evidence, identity evolution, immutable migration, and acceptance recording converge (`Spine:L326-L336`). | **Pass.** |
| AD-15 — owner choreography | Intent ownership, owner transaction, authority signal, deterministic coordinator identity, retry/repair semantics, and mapping acceptance boundary converge (`Spine:L338-L352`). | **Pass subject to open A13.** |
| AD-16 — parity/streaming | Semantic/origin comparison, adapter restrictions, advisory progress, authoritative re-query, partial-output, concurrency, retry, and status semantics converge (`Spine:L354-L378`). | **Pass.** |
| AD-17 — host/environment boundary | One DomainService host/admission hook, prohibited parallel pipelines, local-only AppHost, shared-key/single-replica invariant, and environment boundary converge (`Spine:L380-L390`). | **Pass.** |
| AD-18 — HTTP wire authority | OpenAPI is the sole HTTP wire source; handwritten exceptions affect generator machinery only and cannot create another wire authority (`Spine:L392-L401`). | **Pass; D-6 resolved.** |
| AD-19 — runtime controls/fair operations | One durable admission view, fail-closed consumers, no permissive production fallback, WDRR hierarchy, leases/dead letters, worker ownership, honest health, and gate boundaries converge (`Spine:L403-L416`). | **Pass subject to open A6/A11.** |

## Cross-area attack summary

| Attack area | Final result |
| --- | --- |
| Command stages | Convergent through AD-2 and the companion's exact pipeline restatement. |
| Audit attempts | Convergent: every tenant-admin dashboard read uses the separately measured path, with fail-closed outage behavior. |
| Owner choreography and cross-context ownership | Convergent through AD-5/AD-15; execution remains A13-gated. |
| Lifecycle/retry identity | Convergent through family tables, lifetime identities, stored outcomes, exact retry semantics, and links. |
| Authorization/policy/M0 | Convergent and fail closed; bootstrap remains non-executable while A5/A6/A13 are open. |
| Audit hashing | Fixed invariants converge; the interoperable profile remains intentionally A6/A13-gated. |
| HTTP/streaming/parity | Sole OpenAPI authority, semantic parity/origin, advisory progress, and re-query authority converge. |
| Replay/recovery/evidence | Replay verifier and recovery channels converge; Stack identities are distinct and A10 remains open. |
| Hosting/deployment | DomainService host, sole hook, replica protection, and local-versus-production boundary converge. |
| Runtime controls/operations | Admission source, fairness hierarchy, lease/dead-letter behavior, and honest unsupported state converge; numbers remain A11-gated. |

## Final disposition

**Final reviewer gate: PASS.** There are zero blocking findings, zero advisory findings, and zero recommended
architecture remediations for companion SHA-256
`b421a3bbd79803a1992a1aa67fc5e030cd30522988ced0f9ecea8d1807611485` and spine SHA-256
`6c99ff39c87e1d76a78f2f80b05f748a328624f453ea866d5d1ba6df24e3f6d4` (`status: final`). D-1 through D-6 and E-1
are resolved. A5,
A6, A10, A11, and A13 remain open, and no readiness inference is made.
