## Run: 2026-05-31T13:54:21Z

**Epic:** Hexalith.ChatBot - Epic Breakdown
**Stories:** 1.14, 1.15, 1.16, 1.17, 1.18, 1.19, 1.20, 1.21

### Patterns Observed
- Sprint-status was the most reliable completion source when monitor-session output lagged or helper parsing failed.
- Focused compiled xUnit v3 executables were the reliable local validation path in this sandbox because default VSTest socket setup was blocked.
- Review passes repeatedly improved story records, file lists, UI safety contracts, and documentation alignment.
- Runtime topology learnings surfaced during implementation and needed retrospective documentation updates before Epic 2.

### Code Review Insights
- Common issues: stale story metadata, over-broad acceptance claims, missing file-list entries, weak edge-case coverage, and documentation drift.
- Average cycles to clean: one review cycle per resumed story in this run.

### Timing Estimates
- create-story: roughly 6 to 8 minutes per story after resume
- dev-story: roughly 10 to 16 minutes per story
- code-review: roughly 5 to 10 minutes per cycle

### Recommendations for Future Runs
- Treat sprint-status plus story artifact checks as the source-of-truth pair for completion verification.
- Keep compiled xUnit validation documented as the local fallback while VSTest sockets are unavailable.
- Run documentation reconciliation immediately after epics that discover live topology details.
- Start Epic 2 with Epic 1 conformance, tenant isolation, redaction, localization, and UI safety fixtures as regression gates.

## Run: 2026-06-01T11:19:32Z

**Epic:** Hexalith.ChatBot - Epic Breakdown
**Stories:** 2.1-2.9, 3.1-3.14

### Patterns Observed
- Sprint-status plus story-file status remained more reliable than tmux monitor final states when sessions exited without monitor-friendly output.
- Max-parallel enforcement needed manual attention when fallback agents attempted nested orchestration. Single-session prompts prevented further nested workflow fan-out.
- Epic-level retrospectives surfaced real documentation drift, but the update scope stayed narrow when verified against code.
- Story reviews repeatedly found high-value projection, redaction, DAPR wiring, wire-token, and file-list issues before commit.

### Code Review Insights
- Common issues: stale/out-of-order projection behavior, redaction leakage, generated enum names instead of wire tokens, missing DAPR endpoint/DI wiring, incomplete file lists, and unsafe fallback provenance.
- Average cycles to clean: one review round for the completed Epic 3 stories after implementation reached review.

### Verification Notes
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` remained the build gate.
- Compiled xUnit v3 runners remained the reliable local test path because VSTest socket creation is blocked in this sandbox.
- `git diff --check` and staged diff checks caught generated-client whitespace before the final Story 3.14 commit.

### Recommendations for Future Runs
- For new projection handlers, require DI registration, endpoint mapping, DAPR replay/idempotency, and store-parity checks in the review prompt.
- For every contract expansion, require direct wire-token serialization, OpenAPI parity, generated-client hash, and UI mapping coverage.
- Keep fallback prompts explicit about no nested workflows and no subagents when max parallel is 1.
- During retrospective doc audits, update only implementation-claim docs with verified drift; leave PRD/epics alone when they remain product-level requirements.
