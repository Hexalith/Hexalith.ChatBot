---
title: 'Resolve five deferred association review defects'
type: 'bugfix'
created: '2026-10-28'
status: 'in-review'
route: 'oneshot'
baseline_commit: 'aa4450b21339a6d902147b91f936ed5c5150dec2'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-105, DW-106, DW-107, DW-108 and DW-109 as one association-review component maintenance batch. Derive DOM identities and every matching label, description, banner and action reference from a stable component-instance prefix, including repeated evidence chips. Consolidate the duplicated association key-value rendering and restricted-evidence display helpers while preserving Fluent/FrontComposer composition, encoded metadata and restricted-evidence suppression. Make CSS checks bind responsive, reduced-motion and forced-colors behavior to the selectors responsible for association content; replace blanket character bans with declaration-value checks for literal colors. Move unrelated hosting, shell, project-conversation and approval component assertions into appropriately named test classes, retaining their coverage. Verify rendered identity isolation and the shared metadata renderers, including restricted evidence, and exercise the CSS checks against mutations that previously passed. Close exactly these five deferred items with honest validation evidence after independent review, then commit and push the changes on main as requested. Preserve dependency pins, submodule contents and unrelated user changes.

</frozen-after-approval>

## Implementation Notes

Investigation found no human-only intent gaps or irreversible application changes. This is a localized refactor of four association Razor files and their existing source/render tests, with internal rendering helpers and test-only CSS inspection. It follows the small-change oneshot route under the user's five-item batch and push authorization. GUIDs are already used by neighboring UI components for local DOM identity; these are not domain identifiers.

Code map: `ChatBotAssociationReviewActions.razor` has fixed heading, textarea, counter, banner and action IDs plus composed aria-describedby references. `ChatBotAssociationEvidenceComparison.razor` has fixed heading/empty-state IDs; both comparison and candidate evidence IDs currently collide for repeated project IDs. Three Razor files duplicate CodeRow/TextRow; candidate/comparison duplicate EvidenceText. `AssociationReviewComponentContractTests.cs` mixes association facts with six unrelated shell/hosting/conversation/approval facts. Responsive association selectors live in the 48rem media block, reduced-motion selectors in their media block, and shared governed-action reason focus selectors supply forced-colors behavior. Preserve design-system control focus styling. Existing `Story12CssRetirementE2ETests` renders actions and must resolve generated IDs rather than literal IDs.

Baseline: `dotnet build tests/Hexalith.ChatBot.UI.Tests/Hexalith.ChatBot.UI.Tests.csproj --configuration Debug --no-restore -m:1` passed with zero warnings/errors. Required `aspire start --non-interactive --apphost src/Hexalith.ChatBot.AppHost/Hexalith.ChatBot.AppHost.csproj` exited 2 before launch: Conversations Contracts/Client require the Platform tool-selected source mapping. No AppHost launched; startup/browser qualification remains independently blocked.

Implemented instance-local IDs throughout the three reusable association components, including headers, labels, field counters, banners, governed action reasons and repeated evidence references. Identity is independent of project/evidence content and survives parameter transitions. The shared internal AssociationReviewRendering helper preserves encoded code values, ordinary Fluent text and localized restricted-evidence placeholders. Existing source guards now follow the shared renderer instead of requiring private helper copies.

Unrelated contract facts now live under shell/body, host defaults, project conversation, approval item, task-intent and Why-project classes. Shared source-reading utilities keep those moves mechanical. Association CSS tests inspect exact media/selector/property pairs, honor declaration order and important values within the existing flat-rule subset, bind their selectors to actual rendered association elements, and fail on selector removal, width/min-width substitution, delay changes and later overrides. Literal-color tests cover hexadecimal forms, alpha/modern functions, named colors, nested literals, token-only expressions, non-color identifiers, comments, selectors and URL fragments. No product stylesheet change remains.

The configured oneshot blind review was the only review layer; no additional layers were configured. All ten findings below were patched. The same reviewer rechecked the final implementation, found no concrete unresolved issue and independently ran 51 focused renderer/CSS cases successfully. No new work was deferred.

This build record remains in-review: full UI validation retains four pre-existing shared-catalog expectation failures, and the topology startup baseline is blocked. The independent browser primary lane was not executed; AppHost startup failure is not evidence that this separate lane cannot run. No product story, sprint item, technical-enabler or release gate is marked complete by this maintenance batch. Scoped verification and exact limitations are recorded in validation/five-deferred-association-review.json; raw local machine results remain in TestResults/five-association-*.{ctrf.json,trx}.

## Review Triage Log

| Finding | Verdict / disposition | Evidence |
| --- | --- | --- |
| Token-only color-mix/light-dark rejected as literals | low / patched | Wrapper expressions with only variables now pass; nested literal-color arguments remain detected. |
| Width assertion also matches min-width | medium / patched | Exact property matching and a width-to-min-width mutation now fail correctly. |
| Later matching declarations can override asserted values | medium / patched | Flat-rule declaration order and important values are respected; width and animation override mutations fail. |
| Added forced-colors association selector duplicates the shared selector | low / patched | Redundant selector removed; tests bind the shared reason selector to actual rendered association reasons. |
| Reduced-motion transition-delay not covered | low / patched | Exact zero-delay invariant and changed-delay mutation added. |
| CSS checks do not prove markup retains selectors | medium / patched | Real-render regressions require candidate, evidence, comparison, action-wrap and shared reason classes. |
| Ordinary Fluent text row typography unproven | low / patched | Confidence and correction owner values render encoded and outside code elements. |
| Restricted evidence placeholder unasserted | low / patched | All three restricted states must render the localized association evidence placeholder. |
| Populated comparisons omit ShowHeading in theory | low / patched | Both populated variants receive ShowHeading and assert headings or accessible labels. |
| Empty parameter rerender omits state transitions | low / patched | Same-instance validation, correction visibility, candidate and evidence transitions preserve surviving IDs and local references. |

Final verification: both original UI test projects build in Debug with zero warnings/errors. The focused association/relocated/definition-list lane passes 205/205 and the existing Story12 static-render lane passes 6/6, all without skips. The broad UI suite reports 585 total, 581 passed and the same four failure identities observed before changes (baseline 533 total, 529 passed). Exactly DW-105 through DW-109 are closed. Commitlint passed, result/source hashes match the tracked receipt, and no dependencies or submodule contents changed.
