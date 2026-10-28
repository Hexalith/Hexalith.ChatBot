---
title: 'Resolve five deferred recovery read-model coverage items'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
baseline_commit: 'acf8f6c219ea85f278740eb7b0ba3c38888c086f'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-104, DW-138, DW-139, DW-140 and DW-142 as one recovery read-model verification maintenance batch. Reuse production intake-index key factories, include indexed intake conversation and participant descendants in recovery cleanup and absence verification, and retain their identities across index erasure. Preserve other intakes, tenants and shared project/global indexes. Qualify cleanup claims to indexed fixture records and captured descendants; arbitrary unindexed orphan discovery is outside this key-addressed store contract. Correct the historical B2 positive-control claim to distinguish the controlled NFR59 observation from cleanup absence checks, which cannot prove channel liveness after all known-present projections have been erased. Verify the already-corrected rebuild regression and complete the outstanding independent review of the shared probe. Preserve polling cadence, final boundary observation, cancellation and failure propagation, stored key strings, dependencies and submodule contents. Close these five ledger entries with scoped evidence, then commit and push on main as requested; do not claim live recovery or release qualification from focused tests.

</frozen-after-approval>

## Implementation Notes

Investigation found no human-only intent gaps or irreversible application changes. This localized maintenance uses the oneshot route: internal production key/inventory helpers, the existing shared recovery probe and its two consumers, focused tests and claim corrections. The five selected entries share the existing recovery read-model verification boundary. DW-140's brittle write count is already replaced at the baseline by persisted-state and neighboring-tenant checks; closure requires a passing receipt.

Production ReadModelProjectConversationProjectionStore owns private intake/participant/attachment indexes and project indexes. Materialized participant and attachment conversation rows appear only in the shared project index, so inventory must inspect indexed projects and select matching tenant/intake values. Shared indexes remain intact. Cleanup captures descendant keys before erasing discovery indexes and preserves the captured keys for subsequent absence checks.

Aspire startup via the repository source-mapping wrapper succeeded. Resource inspection found Keycloak unhealthy and dependent projects waiting; the app was stopped successfully before builds. This startup observation is not a healthy full-topology or live recovery receipt.

Implemented production-owned root-key enumeration and a read-only indexed-row inventory. The shared probe captures immutable per-tenant/intake inventories before erasure, merges earlier captured descendants, and checks roots plus discovered/captured children. Both concrete operations classes erase descendants before the five intake roots. Shared project/global indexes remain untouched. The historical B2 claim and its helper documentation now distinguish NFR59 positive-control evidence from cleanup diagnostics.

The full dependency-graph Integration Debug build and Server.Tests Debug build pass with zero warnings/errors. The initial implementation needed six ConfigureAwait(false) corrections in new helper methods; no build rules were weakened. Baseline probe/rebuild verification passed 27/27, including DW-140. Initial focused verification passed 64/64; full Integration passed 465/472 with zero failures and seven explicit live, retained-evidence and production-browser opt-in skips.

The blind review examined the full shared probe, its existing tests and fake store as the requested deliberate DW-142 follow-up. It identified six verification gaps. All were patched: captured child reappearance at the closing boundary; deterministic monotonic time for the intermediate observation; late inventory failures/cancellation through concrete cleanup; missing shared-index fallback; multiple projects sharing item identities; and stale participant ownership. TimeProvider.System preserves the production monotonic clock and default 500 ms polling; the internal advancing test clock removes wall-clock races. No additional review layers were configured.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Captured descendants lack closing-boundary coverage | medium / patched | CapturedParticipantReappearingAtTheClosingBoundaryIsNotLostWithItsIndex injects a participant after its discovery index is erased and only on the final sweep. |
| Intermediate test also passes when intermediate polling is skipped | medium / patched | Internal advancing monotonic clock makes transient presence visible strictly before the window closes; the closing sweep observes no injected presence. |
| Late inventory failures and cancellation untested in cleanup consumers | medium / patched | Four concrete subscription/Graph cases fail or cancel after reading descendant metadata, assert restoration is attempted, no erasure begins, exact caller cancellation propagates and all keys survive. |
| Intake-reference fallback lacks independent tests | low / patched | Three combinations erase tenant-project/project discovery indexes while retaining the intake reference; the association is still inventoried. |
| One-project fixtures omit nested project enumeration | medium / patched | Two projects reuse the same association/item identities, capture both materialized child sets without duplicates and preserve another intake. |
| Participant ownership rejection untested | medium / patched | Reassigning a real participant identity to a second intake makes inventory fail before erasure while preserving both intakes and the foreign participant. |


Final independent recheck confirmed all six findings addressed and found no additional concrete issue within the isolated indexed/captured fixture scope. Updated focused tests pass 74/74 without skips; full Integration passes 475/482 with zero failures and the same seven explicit opt-in skips. Full unfiltered Server passes 3,534/3,534 with zero skips using -parallel none. A deliberate mutant that skips intermediate observations builds cleanly but fails the rewritten intermediate-poll test (expected false, observed true); the original source was restored byte-for-byte before final build and verification. No new work was deferred.


Final restored-source verification passes 74 focused tests without skips and 475 full Integration tests with zero failures and seven opt-in skips. The retained receipt is validation/five-deferred-read-model-coverage.json with exact commands, summaries and source/result SHA-256 values. Exactly DW-104, DW-138, DW-139, DW-140 and DW-142 are closed. This completes the maintenance batch; no story, sprint, release, A10 or production recovery acceptance is advanced. Shared project/global discovery indexes remain intact. All submodules and package pins are unchanged.
