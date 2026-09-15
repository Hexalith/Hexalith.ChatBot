# Input Reconciliation — Recommended Validation Remediation

- **Run date:** 2026-09-14
- **Change signal:** Apply the actionable Critical and High recommendations from `validation-report.md` while preserving stable requirement IDs.
- **Purpose:** Extract the authoritative product intent, qualitative experience, scope, ownership, and safety constraints that must survive the remediation.
- **Result:** The recommendation set is directionally compatible with the product brief and durable decisions. Five reconciliation constraints below determine how the ambiguous recommendations should be implemented.

## Input scope

`source-manifest.md` identifies these direct inputs:

- `_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md` — original product intent and user-voice anchor.
- `_bmad-output/planning-artifacts/architecture/architecture-chatbot-epic-12-recovery-provenance-2026-08-24/ARCHITECTURE-SPINE.md` — accepted recovery evidence-integrity contract, still `activation: pending`.
- `orient-extract.md`, `review-adversarial-general.md`, `review-rubric.md`, and the prior `validation-report.md` — historical orientation and review evidence, not independent product authority.
- `.memlog.md` — canonical decisions and overrides, including the 2026-09-14 authorization to apply the latest actionable Critical and High recommendations while preserving stable requirement IDs.

The current `prd.md`, approved normative `addendum.md`, current `validation-report.md`, and `qualification-evidence.md` were used as the reconciliation targets. Sibling revisions and contract files in `source-manifest.md` constrain compatibility and owner acceptance; they do not transfer source ownership to ChatBot or close A5, A6, A10, A11, or A13 by themselves.

## Product intent and qualitative anchors

The original promise is simple collaboration around a project or subject, with AI helping work move forward without losing context. The MVP narrows “subject” to a governed Project and uses email as the first wedge because external collaborators already work there and should not need another portal.

The experience should continue to feel like:

- **Invisible governance for external participants:** external customers, suppliers, and partners continue using email; identity, authenticity, association, file governance, and authorization happen behind the scenes.
- **Controlled acceleration for project contributors:** project context is assembled without repeated explanation, but boundary-crossing AI actions pause for an intelligible human decision.
- **Accountable repair:** corrections remove contaminated context from future use without erasing history or pretending irreversible effects never happened.
- **Operational confidence:** ambiguous, unauthorized, blocked, delayed, or failed work remains visible to an authorized owner with a reason and next safe action.
- **Repeatability across surfaces:** UI, CLI, and MCP use one command model with equivalent authorization and outcomes; parity never means equal privileges for every principal.

The differentiating product thesis remains email becoming authorized project work—not a generic chatbot, security console, broad project-management replacement, or autonomous agent platform in the MVP.

## Scope and ownership that remediation must preserve

- The MVP remains one email-to-governed-action loop delivered in dependency order M0 → M1 → M2. Breadth may shrink, but tenant isolation, authorization, fail-closed behavior, lifetime idempotency, atomic audit, and non-downgradable human approval may not.
- M0 is the UI-only controlled pilot preview for one tenant and one controlled mailbox pattern. M1 adds governed chat, CLI/MCP parity, outbound draft/send, service-client permissions, and the full tenant-governance surface. M2 adds production operations, recovery, replay isolation, and evidenced SLOs.
- Scheduled, file-addition, and broad event-triggered automation; general user uploads; additional messaging channels; unrestricted commands; broad document intelligence; general non-Project workspaces; and an external-user portal remain outside MVP.
- ChatBot owns orchestration and security-sensitive derived records: channel intake, Project-context resolution/association workflow, task-intent capture, approval routing, AI mediation, and cross-surface exposure. Projects, Conversations, Folders, Parties, Tenants, EventStore, and mail integration retain their named source ownership.
- Every source mutation or repair crosses a versioned owner command/event contract. ChatBot may coordinate and wait for acknowledgements; it must not silently take ownership of source conversations, files, Project membership, tenant roles, event durability, or mailbox authority.
- The exact AI allowlist remains deny-by-default. The six boundary-crossing effect classes—state mutation, file exposure, external send, task creation/assignment, tool invocation, and acting on behalf—always require authorized human approval and cannot be downgraded by policy.
- The current release claims remain bounded: A5/A6/A13 block M0 and M1; A10/A11 additionally block M2. Documentation remediation must not imply that a clarified schema or newly named test is evidence that closes a gate.

## Reconciliation constraints for the latest recommendations

### 1. Human approval and MCP parity must be reconciled by principal class

**Applies to:** Critical “AI-bound MCP principal can approve its own action”; related service-client and parity edits.

The product brief requires the same governed command surface across UI, CLI, and MCP, and also requires human approval before AI changes Project content or sends information externally. These are compatible only if surface parity is separated from actor authority.

Preserve approval operations in the parity catalog for a current, human-authenticated delegated MCP session. Structurally deny `ApproveAIAction`, `RejectAIAction`, `RequestAIActionRevision`, cancellation used as a review decision, and equivalent approval mutations to AI/tool MCP principals, service identities, workers, and the proposing AI actor. A valid decision records current user presence, `actorType=human`, the human principal, authority evidence, and a non-self-approval check. Do not “fix” the finding by removing MCP approval parity for legitimate human-delegated use or by allowing an AI-bound client to impersonate a human.

### 2. Correction and data-rights workflows must remain owner-driven

**Applies to:** Critical “correction omits source-owned and irreversible effects”; High “data-rights workflows lack actors, queries, and surface.”

The original brief and durable decisions make ChatBot an orchestrator, while Conversations owns messages and conversation assignment, Folders owns files, Projects owns Project authority, Parties owns identities, and EventStore owns or must accept the canonical durability seam. A correction impact manifest should therefore enumerate every derived record, owner-owned record, disclosure, append, send, task/action, and other effect, then dispatch owner-specific reassignment, invalidation, compensation, or disposition commands. Completion requires current authority for both source and destination Projects and recorded acknowledgements from every affected owner. Irreversible effects are explicitly dispositioned; history is not rewritten.

The pre-pilot export, erasure, legal-hold, and retention contracts should similarly name a human `compliance-admin` initiator, independent reviewer where A6 requires it, owner-side execution, status/result/redaction queries, and an explicit admin API/CLI surface. They must not create an external-participant portal, grant ChatBot ownership of source data, or turn compliance support into a new customer-facing MVP workflow. Any new producer obligation remains A6/A13-gated until accepted.

### 3. M0 policy bootstrap and A11 must retain their increment split

**Applies to:** Critical “M1 depends on undefined A11 measurements”; High “policy snapshot is M0 dependency and M1 record.”

The memlog already resolves the policy sequencing choice: the immutable base policy snapshot and its first two-person bootstrap exist in M0 through provisioning automation; the full policy editor and broader tenant-governance surface arrive in M1. Reclassify the durable policy-snapshot record as M0 and describe M1 as administration capability over successor versions. Do not invent two competing policy record classes or move M0's safety defaults into M1.

Split A11 by decision timing without weakening it: an A11-M1 gate freezes every M1 pass/fail metric definition, population, denominator, supported-request mix, target, evidence source, owner, and observation window before M1 measurement begins; A11-M2 continues to own the complete candidate-bound SLO catalog, error budgets, live signals, routes, and burn tests. This preserves the Product Lead's ownership and prevents provisional metrics from passing M1 while leaving production SLO qualification in M2.

### 4. Fail-closed AI semantics need one non-overridable path

**Applies to:** High “indeterminate risk has incompatible outcomes,” “approval freshness undefined,” “untrusted content not governed,” and “universal command spine applies AI-only stages to all mutations.”

The durable decision says unresolved AI requests are refused, mandatory effects are never downgradable, and every durable mutation uses the atomic audit/idempotency boundary. For an invalid or indeterminate classifier result, choose the typed no-proposal/no-mutation denial path; do not create a normal approvable proposal that lets a reviewer override missing risk evidence. A separate security-sensitive attempt record may be emitted only through its defined fail-closed audit path and does not authorize execution.

Add explicit approval expiry and material-drift invalidation per effect class, with reapproval creating a linked decision against refreshed authority, policy, proposal, content/resource digest, recipients, and target revision. Label external email, quoted threads, attachment text/names, retrieved Project content, and tool results as untrusted data rather than instruction authority; suspicious content should produce a plain-language review or refusal outcome without forcing external senders into a new UX.

Replace the literal all-stages-for-all-commands reading with a closed admission-stage matrix. Authentication, tenant binding, resource authorization, stable identity/concurrency, and atomic audit remain universal for durable mutations. AI risk classification and approval validation apply only to declared AI/effect classes; mailbox intake, policy, legal-hold, projection, and other operations keep their own typed guards. This is a clarification of the one command spine, not permission for adapters to choose or bypass controls.

### 5. Provider, authenticity, outbound uncertainty, and gate evidence must stay bounded

**Applies to:** High “unknown outbound-send outcome,” “M365 authority conflation,” “authenticity block has no canonical outcome,” and “gate approval freshness is not executable.”

For outbound sends, an unknown provider outcome must enter an explicit investigation/reconciliation state and must not be blindly retried. Resolution must use provider evidence and end as sent, proven-not-sent, or unresolved; a new draft/approval is allowed only after proven-not-sent. Authority must be a provider-neutral intersection of authenticated principal/client permission, mailbox resource/delegation grant, current membership where applicable, sender identity, tenant, Project, ChatBot scope, and fresh approval. Keep the M365 mapping as the first concrete profile while preserving the controlled-provider contract for optional generic email.

For `paranoid` inbound authenticity, add a canonical blocked/quarantined intake outcome with typed reason, restricted visibility, A6-governed retention, release/reprocess authority, and no Project association or AI use. Do not silently discard the message and do not expose internal candidate context to the sender.

Machine-evaluable A5/A6/A13 gate records may define revision binding, evidence hashes, independent signers, approval/expiry timestamps, reopen predicates, supersession, and computed state. Their introduction clarifies what “current” means; it does not close the gates or manufacture missing evidence.

## Other recommendation compatibility notes

- The recommended `Deferred -> NeedsReview -> Associated|Rejected` path is the one already expressed by the lifecycle summary and preserves deliberate human deferral. Update every matrix row, FR, surface, error, and test consistently; do not reopen terminal `Rejected`, `Failed`, or `Skipped` records in place.
- Association-quality remediation should keep separate precision, recall, abstention/safe-no-association, and wrong-association measures. Zero critical false positives and fail-closed review are non-negotiable; safe abstention must remain available but cannot inflate the headline accuracy metric.
- A canonical unknown-send workflow, authenticity state family, approval-expiry state, and data-rights queries add contract completeness, not new product scope. Keep their UI language concise: why work stopped, who owns it, and the next safe action.
- Prompt-injection controls, approval anti-self-dealing, and sender-authority evidence should be included in the existing A5/A8/A13 qualification sets as appropriate; they do not authorize restoring scheduled/file triggers, general uploads, new channels, unrestricted tools, or other post-MVP breadth.

## Reconciliation verdict

Apply the latest Critical and High recommendations, subject to the five constraints above. The recommended edits strengthen the original product promise when they make governance quieter, failures clearer, and authorization enforceable. They would contradict the inputs if they remove legitimate human-delegated MCP parity, let ChatBot mutate sibling-owned source records directly, move M0 safety policy into M1, turn indeterminate classification into an approvable bypass, broaden the MVP, or claim that clarified documentation closes external evidence gates.
