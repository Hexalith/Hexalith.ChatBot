# Story Completion

This policy defines how a ChatBot story or technical-enabler item reaches `done`. It replaces
the TE-2 story-evidence integrity gate, retired on 2026-11-21 by
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-21.md`.

## Lifecycle

- Development ends with the spec at `review` (or `in-review`) and the sprint entry at `review`.
  Review rounds keep that status.
- `done` is a separate, records-only commit. It changes the spec status, the sprint entry and the
  spec's `## Completion` section, and nothing else. Product changes ship in earlier commits.
- An agent may prepare the completion record, but it never commits a `done` transition without
  the maintainer's explicit approval for that story.

## Completion criteria

A story may move to `done` only when all of these hold:

1. **Review is closed.** The latest code-review round has no open patch or decision item, and
   every deferral has an owner and a closure condition in
   `_bmad-output/implementation-artifacts/deferred-work.md`.
2. **Acceptance is mapped.** The spec's `## Completion` section names the exact commit being
   completed and maps every acceptance criterion to the tests that prove it (project, class and
   method).
3. **Product CI passed.** The module's required CI succeeded on that commit, cited by run link.
   Until ChatBot adopts Hexalith.CiCd, run each affected test project individually from a Release
   package-mode build, and record the command, commit and passed/failed/skipped counts.
4. **Live claims ran live.** An acceptance criterion that claims browser, SignalR, hosting/asset,
   Aspire/Dapr or recovery behavior cites a non-skipped run of the named live test: either a CI
   run link, or a local Tier-3 run with its command and commit. Fixtures, fallbacks, screenshots
   and unrelated suites never satisfy a live claim. If the required live path cannot run, the
   story stays in `review`.
5. **Scope gaps are explicit.** Anything the story deliberately leaves open, such as owner
   approvals or external acceptance, is named in the Completion section together with the story
   that now owns it.

Do not commit TRX files or bulk diagnostics. Keep the Completion section short: record decisions
and current state, and leave run-by-run history to commits and CI run links.

## History

TE-2 (`sprint-change-proposal-2026-08-03.md`) reconciled story status, File Lists, scoped diffs,
TRX provenance and primary-path lanes mechanically in module CI. `bf0f44f` removed its tool,
policy and scripts during the shared-CI migration, and the Hexalith baseline keeps story-evidence
checks out of module CI. The evidence contracts and reports under
`_bmad-output/implementation-artifacts/evidence/` remain as frozen history for the stories they
completed. No new contracts are created.
