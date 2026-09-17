---
title: 'Fix generated idempotency helper for required enum fields'
type: 'bugfix'
created: '2026-09-17'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Hexalith.Folders idempotency-helper generator emits a null comparison for the required, non-nullable `PathMetadataPathPolicyClass` enum, so the generated helper fails compilation with CS0037.

**Approach:** Correct the owning generator template, regenerate the checked-in helper, and add focused regression coverage proving required non-nullable enum fields are never null-checked. Update affected client-test fixtures to use the generated enum, then validate the focused tests and Release build with warnings as errors under .NET SDK 10.0.401; do not hand-edit generated output, commit, or push.

</frozen-after-approval>

## Implementation Notes

- Corrected `SpecialFields.Registry` so `path_policy_class` presence depends only on nullable `PathMetadata`; regenerated `HexalithFoldersIdempotencyHelpers.g.cs` through its MSBuild target, including the deterministic self-hash update.
- Added `RequiredNonNullableEnumFieldsDoNotEmitNullChecks`, which binds the OpenAPI required enum declaration, generated non-nullable CLR enum type, and emitted helper expression. Updated affected client-test fixtures to use the generated enum.
- The umbrella Release build then exposed one direct ChatBot consumer still assigning the retired free-form string. `AttachmentStorageIdentity` now uses the conservative `Metadata_only` enum value for arbitrary mailbox attachments, with focused adapter coverage.
- .NET SDK 10.0.401 validation: the Folders client-test Release build passed with 0 warnings/errors; the new regression passed 1/1, file-mutation helper passed 1/1, upload convenience passed 30/30, and the ChatBot attachment adapter passed 1/1.
- A fresh direct generator run compared byte-for-byte equal with the checked-in generated helper. The ChatBot server-test project also passed a Release warnings-as-errors build with 0 warnings/errors.
- The exact `Hexalith.ChatBot.slnx` Release build compiles the corrected Folders client and ChatBot server with 0 warnings, then remains blocked by pre-existing `Hexalith.ChatBot.AppHost/Program.cs` CS1503 errors at lines 153-154 (string credentials passed where parameter-resource builders are required). The owning Folders solution restore is separately environment-blocked because its nested dependency submodules are intentionally uninitialized and repository rules prohibit initializing them.

## Review Triage Log

- `medium`, deferred — the Folders sample still assigns a string to the closed enum; this is pre-existing contract-migration work, and CLI/MCP inputs require validated parsing rather than a partial literal-only patch.
- `false` — the `path_policy_class` expression is intentionally a special projection from `FileMutationRequest` into nested `PathMetadata`; another required enum does not inherit this expression, and generic nested fields already gate only on their nullable container.
- `false` — `ShouldNotContain("PathMetadata.PathPolicyClass is not null")` rejects the invalid check anywhere in all generated variants, while existing Add/Change/Remove behavioral coverage exercises the three helper paths; wire-token formatting is outside this nullability regression.
- `maybe-false`, deferred — a cross-version retry could change its idempotency key, but confirming impact requires evidence that the legacy free-form value was deployed and accepted plus an old/new replay test.
- `maybe-false`, deferred — `Metadata_only` is the conservative valid class for arbitrary attachment bytes; proving permanent AI-context loss requires tracing or testing the authoritative post-scan policy/reclassification and content-read path.
