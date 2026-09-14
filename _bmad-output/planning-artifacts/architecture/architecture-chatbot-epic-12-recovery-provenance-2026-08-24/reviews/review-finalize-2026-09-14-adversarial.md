# Adversarial finalizer review — Epic 12 recovery-primary provenance

## Lens and evidence

**Lens:** Construct two implementation units one level below the epic that each obey every written architecture decision literally, yet still compose into an ambiguous, unsafe, or non-completing recovery-primary lifecycle.

**Evidence inspected:**

- `../ARCHITECTURE-SPINE.md`
- `.github/workflows/ci.yml`
- `story-evidence-policy.json`
- `docs/story-evidence-integrity.md`
- `docs/adrs/live-recovery-validation-drivers.md`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/RecoveryTrxSanitizer.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/ProvenanceAttestor.cs`
- `tools/Hexalith.ChatBot.StoryEvidenceGate/TrxEvidenceReader.cs`
- current recovery producers and cleanup seams under `tests/Hexalith.ChatBot.IntegrationTests/Recovery/`

## Verdict

**CHANGES REQUIRED — four High-severity composition holes remain in the proposed cleanup and enforcement contracts, plus one Medium activation-accountability gap.** The update correctly separates diagnostic from authoritative artifacts, assigns closeout phase boundaries, advances the supported toolchain, and keeps activation honestly pending. It does not yet make a positive cleanup verdict, receipt binding, GitHub check identity, or dual-upload closeout mechanically unambiguous to independently built units.

## Findings

### A-001 — High — Observation-set completeness can be mistaken for cleanup success

**Two-unit counterexample:**

1. A scenario-producer unit registers every run-owned mutation and emits exactly one metadata-only observation per policy-declared scenario/mutation class. For a failed fresh-partition erasure it emits a truthful negative observation such as `cleanupComplete: false`; the spine does not name an observation schema, positive terminal predicate, or outcome vocabulary that would forbid this shape.
2. The repository finalizer unit validates that the observed keys equal the policy's expected keys and writes receipt status `complete`, interpreting that status as “the expected observation set is complete.” It rejects missing, duplicate, and unexpected records exactly as AD-7 requires.

Both units obey the literal cardinality and closed-status rules. Composed, `RecoveryTrxSanitizer` accepts `complete` even though an observation proves that a mutation remains dirty. AD-7 lists the operations that precede finalization, but never states the truth table connecting each observation's terminal result to the aggregate receipt status. It also leaves “one observation for every policy-declared scenario/mutation class” ambiguous between a union of scenario and mutation-class keys and the `(scenario, mutation-class)` pairs actually exercised.

**Evidence:** AD-7 at spine lines 91-95 names `recovery-cleanup-policy.json` v1, exact-set cardinality, receipt identity fields, and the three receipt statuses, but it names no versioned observation envelope and no rule that every expected observation must positively prove its declared clean postcondition before status can be `complete`. The current brownfield producers return booleans or embed `CleanupComplete` in several unrelated report records, demonstrating that no existing single boundary shape can be silently inherited.

**Consequence:** A canonical TRX and sidecar can attest a receipt whose collection is complete while cleanup itself is incomplete, recreating the poisoned-state outcome AD-7 is intended to prevent.

**Recommended disposition:** **autofix.** Bind the minimal observation contract: one versioned envelope; one unambiguous policy key (including scenario, mutation class, and run-owned instance identity where multiplicity exists); an exact positive terminal predicate per class; and a finalizer truth table. Receipt status may be `complete` only when the expected-key set matches exactly and every observation proves its required clean state after the bounded quiescence/read phase. Any negative result is `incomplete`; deadline exhaustion is `timed-out`. Put the quiescence bound or its repository-owned policy field under the same authority.

### A-002 — High — The receipt digest is written into an unspecified TRX location and is never required to reconcile at validation

**Two-unit counterexample:**

1. The sanitizer unit validates the receipt, hashes its exact UTF-8 bytes, and “binds” the digest/status into the canonical TRX using a plausible TeamTest property or root attribute. It therefore follows AD-7.
2. The generic TE-2 validator unit continues to validate the canonical TRX checksum against the provenance sidecar, but neither recognizes that sanitizer-specific property nor loads `recovery-primary/recovery-cleanup-receipt.json`. The artifact-publisher unit later copies whichever receipt occupies that path into the authoritative artifact.

These units all obey the written order. If the receipt is rewritten, normalized, replaced, or staged from a different source after sanitization, the TRX checksum and sidecar still validate while the published receipt bytes no longer match the embedded digest. Even without a replacement, independently implemented sanitizer and validator units can choose different XML locations because the spine does not pin the transport field. The current `TrxEvidenceReader` checks the standard TRX shape and provenance checksum but has no cleanup-receipt input, while the current sidecar schema has no receipt field; repository reality therefore supplies no hidden stronger convention.

**Evidence:** AD-7 at lines 91-95 requires `RecoveryTrxSanitizer` to bind digest/status into the TRX, while AD-2 at lines 61-65 merely says the job later “validates.” AD-7 does not bind `StoryEvidenceValidator` or define an exact TRX field. AD-4 at lines 73-77 calls the authoritative receipt “validated” but does not name the component that revalidates it at the final gate or its exact authoritative-artifact path. `RecoveryTrxSanitizer.cs:116-155` currently emits only standard identity/times/counters/outcome nodes; `TrxEvidenceReader.cs:42-78` and `ProvenanceAttestor.cs:175-190` currently bind only the TRX checksum and existing provenance fields.

**Consequence:** The attested checksum can prove one TRX containing an uninterpreted receipt digest while the retained artifact presents different cleanup evidence. A downstream reviewer cannot mechanically prove that the cleanup receipt, canonical test result, and provenance sidecar are one immutable evidence set.

**Recommended disposition:** **autofix.** Choose one exact binding mechanism and enforce it end to end. Prefer adding explicit `cleanupReceiptSha256` and `cleanupReceiptStatus` fields to the recovery provenance schema, with the attestor and final validator both loading the exact receipt path, requiring `complete`, and comparing the SHA-256 of the same bytes. If the TRX remains the transport, pin the exact XML location/grammar and require `TrxEvidenceReader` to compare it with the receipt bytes. In either design, freeze or copy the validated exact bytes once and make both artifact channels derive from that immutable staging source; pin the authoritative artifact's receipt path.

### A-003 — High — The required-check string is not defined as a globally unique GitHub check-run identity

**Two-unit counterexample:**

1. The workflow unit interprets “the CI workflow must expose `story-transition-evidence-integrity`” literally and sets that value as the top-level workflow `name`, while retaining a differently named job. Alternatively, it uses the string only as the YAML job key and supplies a friendlier job `name`.
2. The repository-rules unit requires the context string `story-transition-evidence-integrity` from the GitHub Actions app, as AD-6 directs.

Both units use the mandated string and keep schedule/manual diagnostics under another identity, but GitHub's required status check is the emitted job/check-run name rather than the workflow name or necessarily the YAML key. The required context may therefore never appear. A second pull-request/push workflow can also emit the same job name and still obey the narrower rule that only scheduled/manual diagnostics differ; expected source `GitHub Actions` does not distinguish those workflows.

**Evidence:** AD-6 at lines 85-89 uses “workflow,” “check identity,” and “transition identity” interchangeably and constrains schedule/manual collisions only. Activation at lines 143-151 asks for branch-rule/source verification but does not require global repository-workflow uniqueness or inspection of the emitted check run. Current `.github/workflows/ci.yml:1,150-152` illustrates the three distinct names: workflow name, job key, and job display name.

**Consequence:** The implementation and repository administration can each pass their local review while the completion gate is absent, permanently pending, or satisfiable by the wrong PR/push job. That leaves the repaired provenance chain advisory at its external enforcement boundary.

**Recommended disposition:** **autofix.** Define `story-transition-evidence-integrity` as the exact non-matrix job/check-run display name, emitted only by the named transition workflow for `pull_request` and `push`. Require a repository-wide workflow test that no other job can emit the same context, and activation evidence that queries an actual check run plus the protected-branch rule with the expected GitHub App identity.

### A-004 — High — Two mandatory artifact publishers still compete for the same undivided ten-minute floor

**Two-unit counterexample:**

1. The diagnostic-publisher unit starts at minute 350 and uses the entire remaining ten minutes for retries or a slow `recovery-primary-diagnostics` upload. This follows AD-4's success/failure attempt requirement and AD-5's publication floor.
2. The authoritative-publisher unit is ordered after it, also under `if: always()`, and expects to publish `story-evidence-integrity-reports` after validation as AD-4 requires.

Each publisher is compliant in isolation. Composed sequentially, the first can consume the job ceiling before the second is scheduled. Reversing the order merely chooses the opposite missing artifact. “No phase may borrow the next phase's reserve” does not arbitrate two consumers inside the same phase, and a GitHub `always()` condition cannot run a step after the enclosing job is terminated.

**Evidence:** AD-4 at lines 73-77 requires both artifact channels on success and failure; AD-5 at lines 79-83 reserves minutes 350-360 for publication but gives the channels no sub-deadlines, concurrency rule, or priority. The current workflow already demonstrates serial artifact steps (`ci.yml:383-395` and `446-455`), although its pre-update names/layout remain activation work.

**Consequence:** A failure path can lose its bounded reconstruction evidence, or a successful validated transition can lose its authoritative artifact, despite the spine promising both channels and a protected publication floor.

**Recommended disposition:** **autofix.** Assign independent absolute upload deadlines or an explicit order plus maximum duration for each publisher so both are attempted before minute 360. A simple sequential contract is five minutes per channel with `always()` on the second; a concurrent/supervised implementation is also valid if the spine names the supervisor and preserves disjoint paths. State which artifact has priority if the host cannot start both.

### A-005 — Medium — No single activation authority records that every external and hosted prerequisite converged

**Two-unit counterexample:**

1. Amelia completes repository implementation and focused local recovery/provenance tests, then treats the repository-visible activation list as satisfied except for branch administration.
2. Murat verifies the branch rule and expected GitHub Actions source, then treats the external enforcement prerequisite as satisfied. Winston reviews the architecture boundary.

All three owners complete their assigned duties, but no owner or durable record is responsible for joining those facts, identifying the exact workflow revision and GitHub rule, deciding whether “focused” verification must be a hosted transition-declared run, and changing `activation: pending` to active. One document maintainer can therefore activate from local tests while another correctly waits for hosted proof; conversely, everyone can wait for someone else to make the terminal decision.

**Evidence:** AD-6 names separate implementation, policy, and review owners. Activation Preconditions at lines 143-151 list the checks but do not name the activation decision owner, evidence location, exact hosted/local threshold, or atomic transition rule. Deferred at lines 153-159 explicitly permits hosted duration distributions to remain unproven, which makes the minimum activation run especially important to distinguish from later tuning evidence.

**Consequence:** `activation` and Story/TE lifecycle status can drift from the real protected-branch and executable state even when every team follows its local assignment.

**Recommended disposition:** **discuss or defer explicitly.** Name one activation authority and one durable activation record. Require it to bind the exact repository revision, emitted check-run context/source, protected-branch/ruleset identity, workflow run ID/attempt for the required focused verification, tool versions, reviewer sign-offs, and the single transition that may change the spine to active and the Story/TE records out of `review`.

## Attacks that did not expose a remaining hole

- **Retained recovery substituted for current-run completion:** no hole. AD-1 fixes the exact source, paths, locator, and single-consumer cardinality, while AD-3 keeps scheduled/release evidence outside TE-2 authority.
- **Diagnostic content cited as TE-2 or A10:** no authority hole in the declared design. AD-4 names a distinct artifact, closed inventory, prohibited TRX/sidecars, and explicit absence of both authorities. A report-schema confidentiality review may still be useful during implementation, but it is not needed to resolve authority ownership.
- **Producer or teardown consumes finalization time:** the adopted `330/335/350/360` phase boundaries close the original shared-closeout finding. A-004 is narrower: competition remains only between the two publishers inside the final phase.
- **Mutable artifact transport actions:** no hole. AD-8 pins both completion-path actions to reviewed full commit SHAs and records their release versions.
- **Activation falsely claimed today:** no hole. `status: final` and `activation: pending` are clearly distinguished, and the current repository mismatches are explicitly activation work rather than silently ratified reality.

## Gate recommendation

Do not hand off the spine as a convergent build substrate until A-001 through A-004 are tightened in enforceable Rules. A-005 can remain an explicit activation open item if the implementation work begins before the activation owner/evidence record is selected; it must be resolved before `activation` or the Story 12.15 / TE-2 lifecycle status changes.

## Final rerun after gate fixes — 2026-09-14

This section supersedes the earlier verdict and gate recommendation above.

### Rerun verdict

**CHANGES REQUIRED — no Critical findings remain, but one High-severity shared-data-shape finding remains.** A-001, A-003, A-004, and A-005 are closed. A-002 is materially improved but not fully closed because its three exact names still have no exact representation or single shared schema authority inside the TRX.

### A-002 — REMAINS HIGH — Named cleanup properties still lack one interoperable TRX grammar

**Two-unit counterexample:**

1. The sanitizer unit obeys revised AD-7 by emitting exactly `hexalith.cleanupReceipt.schemaVersion`, `hexalith.cleanupReceipt.status`, and `hexalith.cleanupReceipt.sha256` as root attributes, or as three TeamTest `<Property>` entries below a `<Properties>` node.
2. The validator unit obeys revised AD-7 by reloading and hashing the receipt and then looking for the same three names, but it chooses the other plausible TRX representation. A permissive implementation may instead search descendants by local name, which introduces duplicate/foreign-namespace ambiguity.

Both units use the exact required names and compare the required receipt values. They cannot interoperate because “TRX properties” does not fix XML namespace, parent/location, key/value representation, cardinality, lexical form, or duplicate/unknown-property rejection. Current repository code supplies no convention to inherit: `RecoveryTrxSanitizer` emits no properties, and `TrxEvidenceReader` has no cleanup-binding parser.

**Consequence:** Independent sanitizer and validator work can fail every otherwise valid transition, or a permissive parser can read a different duplicate property than the sanitizer intended. Because these fields are the only path from cleanup proof into the attested TRX, ambiguity remains at a High-severity evidence-integrity seam.

**Required disposition:** **autofix.** Either pin the minimal canonical XML fragment—namespace, exact parent path, exactly-one cardinality, key/value encoding, lowercase 64-hex digest, fixed schema/status lexical values, and duplicate/foreign/unknown rejection—or name one repository-owned cleanup-binding serializer/parser that is the sole authority used by both `RecoveryTrxSanitizer` and `StoryEvidenceValidator`. The second option keeps structural seed out of the spine while still preventing independent implementations from diverging.

### Closed findings

- **A-001 — closed:** `hexalith.chatbot.recovery-cleanup-observation/v1`, identity matching, explicit outcomes, stable postcondition IDs, exact-set reconciliation, and the all-positive requirement now prevent observation presence from masquerading as cleanup success.
- **A-003 — closed:** AD-6 now fixes the exact globally unique check-run name, PR-only event, always-run behavior, distinct non-authoritative identities, direct-push prohibition, and activation verification record.
- **A-004 — closed:** AD-5 gives each always-attempted publisher an exclusive absolute five-minute window and states that the first failure cannot skip the second.
- **A-005 — closed:** TE-2 is now the sole durable activation record and must bind the protected branch, exact check/source, protection identifier, owners, and verification run.
- **AD-9 isolation attack — passed:** fresh hosted runners, job-local ephemeral state, Testing guards, generated secrets, run-attempt-bound identities, and one non-canceling repository concurrency group prevent two compliant recovery producers from sharing or canceling destructive state.

### Final gate recommendation

Tighten the cleanup-binding TRX grammar or assign its one shared serializer/parser authority, then rerun this lens. No other Critical or High adversarial finding remains in the revised spine.

## Final closure rerun — 2026-09-14

This section supersedes every earlier verdict and gate recommendation in this review.

### Final verdict

**PASS — no Critical or High adversarial findings remain.**

Revised AD-7 closes the last two-unit counterexample by making `RecoveryCleanupTrxBinding` the sole serializer/parser authority shared by `RecoveryTrxSanitizer` and `StoryEvidenceValidator`. It now fixes the TeamTest `TestRun` root as the parent, unqualified attribute representation, exact three names, digest lexical form, and fail-closed handling of missing, duplicate, malformed, or unexpected cleanup attributes. The validator also reloads the retained receipt, revalidates it, recomputes the exact-byte digest, and matches all three attributes before accepting `recovery-primary`; sanitizer and validator can no longer choose independently incompatible representations.

The full rerun also confirms that the other seams remain closed:

- cleanup observation presence cannot masquerade as success because exact-set, identity, outcome, and positive-postcondition rules govern the aggregate receipt;
- the required check-run is always-run only for pull requests targeting protected `main`, with different bases/events carrying distinct non-authoritative identities;
- diagnostic and authoritative publication have separate absolute windows and failure of the first cannot skip the second;
- destructive runs are isolated on fresh hosted runners with job-local ephemeral state and no GitHub concurrency group that could cancel an older pending required check;
- TE-2 is the sole durable activation record, while repository implementation and external enforcement remain honestly activation-pending.

No further adversarial architecture change is required before handoff. Implementation and activation evidence remain governed by the spine's Activation Preconditions rather than findings against the spine itself.
