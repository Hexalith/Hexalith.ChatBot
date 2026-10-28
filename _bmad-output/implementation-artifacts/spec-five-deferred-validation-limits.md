---
title: 'Close five deferred recovery evidence validation items'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve five related deferred recovery-validation items: bound XML structural amplification before building TRX document trees; bound aggregate evidence-contract discovery and retain only inactive contract identities; verify the existing repair of the rendered-realm grant-expiry fixture against current owner-authority semantics; verify the existing mailbox-secret assertion repair; and retire the unused live-recovery runsettings file and misleading acceptance probe. Preserve strict canonical TRX validation, metadata-only failure summaries, output/sidecar preservation on refusal, active duplicate detection, inactive malformed-contract isolation, and current owner-authority admission. Record exact validation and ledger dispositions, independently review, commit, and push the task changes on main.

</frozen-after-approval>

## Implementation Notes

Investigation found no intent gaps or irreversible implementation actions. This is an internal validation bugfix batch with no dependency, submodule, public API, persistence, deployment, or authority-policy changes. The user explicitly requested five deferred items and a push to main; use the oneshot route.

Code map: add an internal bounded TRX loader under `tools/Hexalith.ChatBot.StoryEvidenceGate`, preflight immutable already-byte-bounded inputs with secure XmlReader depth/node/attribute budgets before XDocument construction, and use it from `TrxEvidenceReader`, `RecoveryTrxSanitizer`, and `RecoveryAttemptSummarizer`. Preserve each caller's existing reason/subject mapping. In `TransitionDetector.FindSingleContract`, immediately project parsed candidates to the five identity fields used by predicates and duplicate checks, and bound file discovery and aggregate admitted bytes. Extend gate tests with structural adversaries, boundary acceptance, output atomicity and active-duplicate/discovery-limit cases. Document budgets in `docs/story-evidence-integrity.md`.

The two topology fixture repairs already exist in `d718220801f7855dc4c7b6c0d140f3785e95d6e9`; run the current `RecoveryValidationTopologyContractTests` without changing production authority. Remove the unused root `live-recovery.runsettings`, its XML assertion, and the two obsolete runner-probe methods from `LiveRecoveryValidationArchitectureTests`. Retain the actual runner's zero-executed-test guard and execute its existing Python regression.

Validation: inspect a supported source-mapped Aspire baseline before code edits, then stop the exact task-owned AppHost. Build and execute original Debug gate tests, Architecture tests, and the existing Integration topology class using the pinned source-mapping workflow where needed. Run the executable Python evidence guards. These ordinary checks qualify parser/fixture behavior, not a live recovery drill or A10. Keep unrelated story and sprint states intact. Retain exact commands and machine results under `TestResults/five-deferred-validation` and a tracked evidence summary.


Implemented outcomes: the shared secure loader scans immutable bounded input before all three TRX DOM loads. Each active identity scan retains detached identity strings and counts every examined file plus all admitted bytes, including malformed JSON. Individually oversized inactive files remain isolated and still consume file slots. The unused settings/probe pair is retired, and the two existing fixture repairs are verified rather than duplicated. No dependency, submodule, production authority or product-story/sprint state changes are included.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Duplicate members in an unrelated inactive JSON can throw ArgumentException | medium / deferred, pre-existing | The baseline has the same lazy JsonNode dictionary access. A direct SDK reproduction confirms ArgumentException. Record local malformed-input containment and an active-completion regression separately. |
| One concentrated XML start tag can allocate before XmlReader exposes AttributeCount | maybe-false, medium if confirmed / deferred, pre-existing parser boundary | Both old and new paths use XmlReader. DOM construction is now bounded; buffering remains byte/character bounded. Measure a concentrated distinct-attribute stress case before selecting a tighter parser. Docs explicitly retain this limit. |
| Multiple transitions repeatedly scan the same evidence inventory | medium / deferred, pre-existing | All story/ledger/sprint callers already perform separate lookups. The new ceiling is documented per scan; caching and multi-transition read-count qualification are separate performance work. |
| Malformed JSON byte accounting lacked a direct boundary regression | medium / patched | Exact/excess aggregate-byte cases now include malformed inactive files as well as valid large identity inputs. |
| Ignored oversized files were absent from the file-slot boundary | medium / patched | Exact/excess file-count cases now include an individually oversized inactive file, preserving isolation while proving it consumes a slot. |
| Identity-only retention had no direct regression | medium / patched | The production projection seam has a large nested-input test asserting exactly five copied identity strings, normalized paths and independence from candidate mutation. |
| New discovery-limit subject lacked table remediation | low / patched | The operator table now includes both numeric ceilings and the archive/remove corrective action. |

The blind reviewer rechecked the four corrections and reported no correction defects. No additional review layers were configured. Three pre-existing review follow-ups and the unchanged broad Architecture blocker are recorded in the ledger; historical entry evidence remains intact.

## Verification

The original Debug gate, Architecture and Integration projects built with zero warnings/errors. The real task-owned Aspire baseline started successfully; ChatBot became healthy, and final inspection found 41 Running and three Finished resources. The exact AppHost then stopped successfully. The existing HXW009/HXW011 source-mapping advisories remain; pins and guards are unchanged.

Focused Architecture passes 16/16, the existing RecoveryValidationTopologyContractTests lane passes 30/30, and the executable Python evidence guards pass 14/14, all zero skips. The Python suite includes a real native-runner zero-test refusal with no checksum. Final gate counters, exact commands and source/binary/TRX hashes are retained in `validation/five-deferred-validation-limits.json`; raw logs and results remain local in `TestResults/five-deferred-validation`.

The separate full Architecture run has 156 passed, one failed, zero skipped. Its unchanged sibling-catalog guard rejects FrontComposer EventStore Admin 3.128.2 overrides against shared Builds 3.128.3. The focused changed class passes; the broad failure is not hidden or weakened. This implementation spec remains in-review rather than claiming a terminal completion without its required evidence contract and broad gate. No live recovery, A10 or package-only release qualification is claimed. The five selected ledger entries record three implementations and two verified prior fixes.

Final reviewed gate run: 324 passed, zero failed/skipped. Source and binary hashes in the tracked validation summary match this final build and run. Commitlint and the CRLF-aware whitespace gate are checked before commit; the user authorized the main push.
