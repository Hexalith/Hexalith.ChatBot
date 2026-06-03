# ADR: Consent and lawful-basis metadata — a compliance-admin-gated governed consent-recording command, five closed token sets, a deterministic `ConsentRequirementMatrix.Published` regulatory-profile seed, pure `ConsentRequirementPolicy`/`ConsentGate` fail-closed decision functions, a per-project no-leak read redaction, and an NFR50 audit-evidence block

## Status

Accepted (realized by Story 9.10, FR20 / FR68 / FR75f / FR75g / NFR1 / NFR2 / NFR7 / NFR15a / NFR42 / NFR45 / NFR50 /
NFR55 / two-altitude idempotency Story 1.5). Built directly on the Story 9.7 data-class inventory
([data-class-inventory-and-retention-policy.md](data-class-inventory-and-retention-policy.md)) for the **one**
redaction-sensitivity set and the seed-catalog shape, the Story 9.8 tenant export workflow
([tenant-export-workflow.md](tenant-export-workflow.md)) and the Story 9.9 deletion/erasure workflow
([deletion-and-erasure-workflow.md](deletion-and-erasure-workflow.md)) as the structural templates, the Story 9.3
per-project no-leak compliance read surface, the Story 7.4 compliance-admin retention editor + token validators, and
the Story 7.1 admin-scope model. It is the **fourth member** of the additive compliance-governed-command family
(9.7 inventory → 9.8 export → 9.9 deletion → 9.10 consent) on the mature CommandGateway / audit / admin-scope seams.

## Context

FR20 / NFR55 require that, **where tenant policy or the regulatory profile requires it**, the system records consent or
lawful-basis metadata for the four governed subjects — external participants, retained content, attachments, and
AI-processing events — and that it is queryable for authorized compliance review. NFR7 / FR68 require security-sensitive
operations to **fail closed** when a required basis is absent or policy evaluation is unavailable. NFR1 requires
non-authorized consent recording to fail closed at the gateway, write nothing, and be audited. NFR2 / FR75f require
consent reads to be redacted per project — unauthorized detail excluded and **indistinguishable from not-found** — and
only the human compliance scope to query. NFR50 requires every consent recording/change envelope to carry the full
required-field set plus the consent source-evidence refs.

The framing risk is twofold: (a) building a *second* sensitivity set, validation-result type, token validator, authority
path, or governed-command shape (a define-once violation — Stories 9.7/9.8/9.9 already solved each), or (b) letting an
**ambiguous** case (an unknown subject kind, a missing requirement entry, a non-`active` basis) bias toward "allowed"
rather than "blocked". Consent governance protects data subjects, so every ambiguous case must bias to `required` ⇒
`blocked-missing-basis`.

## Decision

Ship the **governed decision/recording/gating layer** as an almost-entirely-additive contracts + pure-decision-functions
+ governed-command + gateway-gating + audit-evidence story built on mature seams — a structural twin of Story 9.9.

1. **`SubmitConsentLawfulBasisRecord` governed command** — a line-for-line structural twin of
   `SubmitDeletionErasureRequest`: allowlisted in `ChatBotSpineCommandAllowlist`, gated at `ParticipantAuthorizationStage`
   by `HasHumanAdminScope(.., AdminScope.Compliance)` + a typed `IsValidConsentLawfulBasisRecord` /
   `ReadSubmitConsentLawfulBasisRecord`, routed through the one CommandGateway audit-commit spine, fail-closed (no durable
   write) on non-compliance scope / non-human actor (service client / AI actor) / invalid command / audit-writer-down.
   `RecordId` is the stable idempotency/run key (Story 1.5 two-altitude floor).

2. **Five closed token sets**, each a static class with `All` + `Contains` mirroring `DeletionErasureClassActions`
   line-for-line: `ConsentSubjectKinds` `{ external-participant, retained-content, attachment, ai-processing }`;
   `ConsentLawfulBases` `{ consent, contract, legal-obligation, vital-interests, public-task, legitimate-interests }`
   (GDPR Article 6); `ConsentRecordStatuses` `{ active, withdrawn, expired, superseded }`;
   `ConsentRequirementDispositions` `{ required, not-required }`; `ConsentGateDecisions`
   `{ satisfied, blocked-missing-basis }`. These deliberately avoid the legacy-lifecycle literals
   (`pending`/`accepted`/`running`/`succeeded`/`cancelled`), so `ScaffoldArchitectureTests` needs **no** new allowlist
   entry.

3. **`ConsentLawfulBasisRecord` model** — metadata-only: a `ConsentSubjectKinds` kind, an **opaque**
   `AuditMetadata.IsSafeStableIdentifier` `SubjectLocator` (never raw participant email, file name, or message body), a
   project scope ref, a `ConsentLawfulBases` basis, a `ConsentRecordStatuses` status, a safe `BasisSource` token, the
   `RedactionSensitivity` (a `DataClassRedactionSensitivities` member — the **one** sensitivity set, never forked), a UTC
   timestamp, and a `sha256:` `RecordFingerprint`.

4. **`ConsentRequirementMatrix.Published` regulatory-profile seed** — immutable, deterministic, token-only (no `UtcNow`),
   modeled on `DataClassInventoryCatalog.Published`. It biases every governed subject kind to `required`; a future
   tenant-policy override may relax a kind. Exposed as a `ConsentRequirementProfile` (a bounded
   `subject-kind → disposition` map).

5. **Pure `ConsentRequirementPolicy.Evaluate` + `ConsentGate.Evaluate` (the AC4 fail-closed decisions)** — real, testable
   functions, not comments. `ConsentRequirementPolicy.Evaluate(subjectKind, profile)` returns a
   `ConsentRequirementDispositions` token; an **unknown subject kind or a missing/empty profile entry biases to
   `required`**. `ConsentGate.Evaluate(disposition, activeRecordStatus)` returns a `ConsentGateDecisions` token:
   `not-required ⇒ satisfied`; `required` **and** an `active` basis ⇒ `satisfied`; `required` with a
   `null`/`withdrawn`/`expired`/`superseded` status ⇒ `blocked-missing-basis`; an **unknown disposition** biases to
   `blocked-missing-basis`. Both carry no `ClaimsPrincipal` dependency.

6. **`ConsentLawfulBasisSchema` validation** reuses the Story 7.4 `RetentionValidationResult` + the
   `ComplianceAdministrationSchema.IsSafe*`/`IsUtc` token helpers — no second result type or validator. `ValidateRecord`
   enforces per-field closed-set/safe-token checks; `ValidateRequirementProfile` enforces a **bijection** over
   `ConsentSubjectKinds.All` (every subject kind declared exactly once — the `consent_requirement_profile_incomplete`
   invariant, mirroring the Story 9.7 inventory-completeness rule).

7. **Per-project no-leak read redaction (`ConsentLawfulBasisRedactionPolicy.Redact`)** — pure, in `.Contracts`. When the
   reader lacks the compliance scope or the record's project scope ref is not in the bounded
   `ConsentLawfulBasisAuthorityView.AuthorizedProjectRefs`, it drops the `SubjectLocator` + `ProjectScopeRef` and
   collapses the sensitivity to `metadata-only`, so an unauthorized read is indistinguishable from safe-not-found (NFR2).
   The server `ConsentLawfulBasisAuthorizationPolicy` is the **only** place a `ClaimsPrincipal` touches the decision — it
   mirrors `DeletionErasureAuthorizationPolicy.AuthorityFor` line-for-line, projecting `ProjectOwnerClaim` grants filtered
   by `AuditMetadata.IsSafeStableIdentifier` into the bounded view. No second authority path.

8. **NFR50 audit-evidence block** — `AuditEnvelopeFactory.SourceEvidenceRefs` admits `SubmitConsentLawfulBasisRecord` and
   yields `admin-operation:submit-consent-lawful-basis-record`, `admin-scope:compliance`, and the bounded refs
   `consent-record:`, `consent-subject-kind:`, `consent-lawful-basis:`, `consent-record-status:`, `consent-basis-source:`,
   `consent-scope-project:` (authorized refs only), and `consent-fingerprint:`. The opaque `subjectLocator` is **never**
   emitted as a ref — only the record id + scope-project localize the record. Actor / actor-type / timestamp /
   policy-snapshot / decision / reason are carried by the envelope `Create`, not duplicated.

## Deferrals (inert-control-floor honesty)

This story ships the governed *decision/recording/gating* layer that makes "defensible consent governance" real — the
governed command, the record model, the closed token sets, the `ConsentRequirementMatrix.Published` seed, the pure
`ConsentRequirementPolicy`/`ConsentGate`, the per-project no-leak read redaction, the `ConsentLawfulBasisSchema`
invariants, the NFR50 audit recording, tests, and this ADR. **Deferred** (modeled as documented seams):

- the live S-tagged consent-metadata **UI surface**;
- the live wiring of `ConsentGate` into the **actual** AI-processing (`ProposeAIAction`) and retention **execution** call
  sites — modeled as the `ConsentGateEvaluator.EvaluateForGovernedAction` hook (the server-callable fail-closed decision
  the live worker will consult), exactly as Story 9.9's `DeletionErasureRunner.DestroyNonAuditStoreSubjectAsync` modeled
  the non-audit-store destruction runtime;
- the tenant-policy-knob **override** that lets a tenant additionally require a basis beyond the regulatory-profile
  default — modeled as the `ConsentRequirementProfileMapper.ProfileFor` seam (which today returns
  `ConsentRequirementMatrix.Published` unchanged).

The recording **is** governed, the requirement **is** evaluated, the gate decision **is** real and tested (AC4 holds at
the decision layer), and every record **is** audited. A deferral never reads as "consent is ungoverned/unaudited".

## Consequences

- **Define-once preserved**: one sensitivity set (`DataClassRedactionSensitivities`), one validation-result type
  (`RetentionValidationResult`), one token-validator family (`ComplianceAdministrationSchema` + `AuditMetadata`), one
  admin authority gate (`AdminAuthorityEvaluator.HasHumanAdminScope`), one per-project authority pattern, one audit
  evidence path, one allowlist. No fork.
- **Boundary holds (NetArchTest-enforced)**: the shared contracts (the `Consent*` records, the command, the five token
  sets, the schema, `ConsentGate`/`ConsentRequirementPolicy`/`ConsentRequirementMatrix`, the redaction policy, the
  authority view) live in `.Contracts` with no server/gateway/`ClaimsPrincipal` dependency; the server internals
  (`ConsentLawfulBasisAuthorizationPolicy`, `ConsentGateEvaluator`, `ConsentRequirementProfileMapper`, the auth-stage
  validator/reader) are `internal` to `.Server`. `DependencyDirectionFitnessTests` / `AdapterBoundaryFitnessTests` stay
  green; `ScaffoldArchitectureTests` needs no new allowlist entry.
- **Fail-closed is absolute over convenience**: an unknown subject kind, a missing requirement entry, or a non-`active`
  basis biases to `required` ⇒ `blocked-missing-basis`. Backward compatibility is preserved — all Stories 7.4 / 9.3 /
  9.7 / 9.8 / 9.9 compliance, gateway, audit, and architecture tests stay green.

## References

- Story 9.10 — `_bmad-output/implementation-artifacts/9-10-consent-and-lawful-basis-metadata.md`
- Story 9.9 (structural template) — [deletion-and-erasure-workflow.md](deletion-and-erasure-workflow.md);
  `_bmad-output/implementation-artifacts/9-9-deletion-and-erasure-workflow.md`
- Story 9.8 (structural template) — [tenant-export-workflow.md](tenant-export-workflow.md)
- Story 9.7 (sensitivity set + seed-catalog shape) — [data-class-inventory-and-retention-policy.md](data-class-inventory-and-retention-policy.md)
- Story 9.3 (per-project no-leak read surface); Story 7.4 (compliance scope + token validators); Story 7.1 (admin-scope model)
- `_bmad-output/planning-artifacts/epics.md` (FR20, FR68, FR75f, NFR1, NFR2, NFR7, NFR50, NFR55, Story 9.10 lines 2540-2562)
- `_bmad-output/planning-artifacts/architecture.md` (CommandGateway flow 542-551; audit envelope min fields 712; pattern enforcement 577-585)
