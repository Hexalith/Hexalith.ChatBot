---
title: Product Brief vs PRD Reconciliation - Hexalith.ChatBot
status: complete
created: "2026-05-28"
inputs:
  - "_bmad-output/planning-artifacts/product-brief-Hexalith.ChatBot.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md"
---

# Product Brief vs PRD Reconciliation — Hexalith.ChatBot

This reconciliation walks the product brief (`product-brief-Hexalith.ChatBot.md`, 2026-05-10) section by section against the rewritten PRD (`prd.md`) plus its `addendum.md`. The PRD has been heavily restructured to address 52 validation findings: a new 3-increment M0/M1/M2 sequencing, a fail-closed contract (NFR15a), a shared command pipeline invariant (FR81a), addendum-housed schemas (Confidence Thresholds, Risk Classifier, Command Allowlists v0/v1, Tenant Policy Schema, Idempotency Keys, Replay Isolation, ID Evolution, Inbound Message Authenticity), and explicit assumptions A1–A11. The reconciliation focuses on qualitative content (tone, vision, feel, user-experience aspirations, named pain points, stakeholder voice, success metrics) that the formalization process tends to strip away.

## Method

For each substantive thematic area in the brief, the reconciliation states:
- **Brief intent** — what the brief actually said, including its tone and emphasis.
- **PRD coverage** — where (if anywhere) the rewritten PRD addresses it.
- **Gap classification** — Preserved / Preserved-but-dilute / Partial / Missing / Over-formalized.

## Section-by-section reconciliation

### Executive Summary (brief lines 19–25)

**Brief intent.** The brief frames the product as a **"shared conversational workspace where internal and external participants can coordinate around a project, exchange files, trigger AI-assisted tasks, and keep project execution moving across channels."** Key qualitative signals:

1. *Multi-channel from the start*: "the same capabilities are exposed through the chatbot UI, mailbox integration, CLI console application, and MCP server so humans, automation, and AI agents can operate through one consistent command surface" — chatbot UI is named as a first-class channel alongside mailbox/CLI/MCP.
2. *Builds on Hexalith platform*: the brief names eight Hexalith building blocks (Conversations, Projects, Folders, Tenants, Parties, EventStore, FrontComposer, Aspire) and Keycloak — and frames the product as composing them rather than reinventing.
3. *First high-value use case is communication between internal and external users through email*, supporting both Microsoft 365/Exchange and **"generic email integration"** as parallel MVP scopes.

**PRD coverage.**
- Multi-channel framing is **narrowed**. The PRD Executive Summary says "The MVP deliberately starts with email because external collaboration already happens there." The "chatbot UI" framing has been replaced with "project conversation view + ambiguous association review + AI action approval" — and the PRD explicitly flags this in a `[NOTE FOR PM]` (line ~307): *"The 'ChatBot' name reflects the vision-state interactive surface, not the M0/M1 MVP shape. In M0 the user-visible interaction model is the project conversation view + ambiguous association review + AI action approval; there is no conversational chat surface."* This is candidly acknowledged as a positioning gap.
- The platform-composition framing is preserved in §Context Ownership and §Integration List.
- Generic email support has been **explicitly narrowed**: per §MVP, "Generic email support is not a separate MVP channel. Non-Microsoft 365 / Exchange mailbox sources may be included only when they satisfy the same controlled mailbox contract... This depends on A1." The brief treated MS365 *and* generic email as parallel MVP scopes; the PRD treats only MS365/Exchange as M0/M1 scope and gates generic email behind a contract.

**Gap classification.**
- **Gap 1 (Partial/acknowledged):** The brief's framing of a "shared conversational workspace" with a chat-style UI is not in MVP scope. The PRD acknowledges this in a `[NOTE FOR PM]` but does not formally flag it as a stakeholder-expectation risk that needs sales/positioning mitigation.
- **Gap 2 (Partial):** Brief's parallel framing of "Microsoft 365/Exchange-style enterprise mailboxes **and** generic email integration" has been narrowed to MS365/Exchange only, gated by A1. This is a legitimate scoping decision but represents a real reduction from the brief's commitment.

### The Problem (brief lines 27–31)

**Brief intent.** The Problem section names specific human pain points in vivid language:
- *"context is scattered, attachments are duplicated, actions are implicit, and project state is hard to audit"*
- *"Developers and project managers then spend time reconstructing decisions, finding the latest document, and translating conversation into executable work"*
- *"AI assistants make this fragmentation more visible. They can draft, summarize, and automate, but they often lack a durable project boundary"*
- The brief explicitly names the AI's missing context: *"which files are authoritative, who is allowed to participate, which task was requested, what changed, and whether the action was triggered by a schedule, a file arrival, a mailbox message, or a user instruction"*
- *"Without a governed project workspace, AI stays useful but unreliable for enterprise execution."*

**PRD coverage.**
- The PRD Executive Summary captures the core problem ("email loses the connection between messages, files, decisions, approvals, and execution") and "What Makes This Special" captures the governance thesis.
- The brief's enumerated triggers — **"a schedule, a file arrival, a mailbox message, or a user instruction"** — collapse to one trigger in MVP (mailbox event from associated project email). The other three triggers are explicitly out of MVP scope per §MVP Out-of-Scope: *"Scheduled-time, file-addition, and broad event-triggered automation outside associated project email."*
- The brief's developer/PM pain language ("reconstructing decisions, finding the latest document, translating conversation into executable work") is replaced by neutral PRD prose ("less coordination overhead and higher trust"). The visceral language is gone.

**Gap classification.**
- **Gap 3 (Partial):** Brief's four-trigger automation model (schedule / file arrival / mailbox / user instruction) is reduced to one trigger (mailbox). The other three are listed as out-of-scope but are not captured as a post-MVP roadmap commitment that the brief implicitly made.
- **Preserved-but-dilute:** The human pain-point language is gone. The PRD is correct but bloodless. This may be acceptable for an FR/NFR document but should be re-injected when this PRD is read by anyone outside the implementation team.

### The Solution / "Users can…" (brief lines 33–48)

**Brief intent.** The brief gives a seven-bullet user-facing promise:

1. Collaborate with internal and external participants around a project or subject.
2. **Continue project conversations through the chatbot UI or mailbox**, with future channels such as Teams, WhatsApp, and other messengers.
3. Add files to project folders and ask the AI to act on them.
4. **Trigger automated tasks by schedule, by adding a file, or by a request from a conversation actor.**
5. Use the same commands through the UI, CLI console app, and MCP server.
6. Require human approval before an AI action changes project folder content or sends information to an external recipient.

The user-facing promise is: *"I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context."*

Then a one-line aspiration that the PRD has not preserved verbatim: *"Every automated action should remain attributable: who or what requested it, which project and files it used, which command was sent, what result came back, and what follow-up is required. That traceability is what makes AI useful for enterprise project work rather than just convenient for individual productivity."*

**PRD coverage.**
- Bullet 1 → preserved (FR1–FR20, UJ3).
- Bullet 2 → **partially missing.** "Chatbot UI" is not the MVP UI; "future channels" (Teams, WhatsApp) appear in §Growth Features but the brief's signaling that this is a multi-channel product from day one is gone.
- Bullet 3 → preserved (FR29–FR34, FR33 scoped AI context).
- Bullet 4 → **partially missing.** Schedule-triggered and file-add-triggered automation are explicitly out of MVP scope (§MVP Out-of-Scope, §Vision). The brief's language framed these as part of the user promise; the PRD pushes them to Growth Features.
- Bullet 5 → preserved (FR81a shared command pipeline, FR82, FR83).
- Bullet 6 → preserved (FR41, FR42, §Risk classification defaults).
- The user-facing promise verbatim ("I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context") is **not in the PRD as a quoted user voice.** The PRD has equivalent governance language but not the simple human-language framing.
- The "every automated action should remain attributable" aspiration is preserved as audit FRs (FR55, NFR50, NFR50a) but the **prose framing — "what makes AI useful for enterprise project work rather than just convenient for individual productivity"** — is gone.

**Gap classification.**
- **Gap 4 (Missing):** The brief's user-facing promise ("I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context") does not appear as a quoted vision statement in the PRD. The PRD has no equivalent single-sentence user-voice anchor. This is the kind of qualitative framing that survives sales conversations; FR/NFR catalogs do not.
- **Gap 5 (Partial):** Brief bullets 2 and 4 (multi-channel chatbot UI; schedule/file-add automation) are MVP commitments that the PRD has demoted to Growth/Vision. This is a legitimate scoping move, but it should be explicitly tracked as a "PRD departed from brief" decision in `.decision-log.md` — the PRD does not yet record this as a deliberate departure.

### What Makes This Different (brief lines 50–60)

**Brief intent.** Seven differentiators, each one sentence:
1. **Project-first AI context** — "conversations, files, folders, tasks, participants, and triggers belong to a project boundary instead of floating across tools."
2. **Email as a first-class collaboration channel** — "the MVP starts where external collaboration already happens, then turns mailbox activity into durable project context and actionable work."
3. **One command model across surfaces** — "chatbot UI, CLI, and MCP expose the same operations, reducing drift between human workflows, scripts, and AI agents."
4. **Built on Hexalith services** — names six bounded contexts.
5. **Automation with auditability** — "scheduled, file-triggered, and conversation-triggered work can be represented as commands, events, projections, and task outcomes instead of hidden chatbot side effects."
6. **Governed external collaboration** — "internal and external users can participate in the same project context while tenant, party, identity, and authorization boundaries remain explicit."
7. **Human approval for higher-risk actions** — "project file mutations and external information sharing require explicit approval."

**PRD coverage.**
- 1 → preserved as "AI as a governed project actor" (Executive Summary; Innovation section).
- 2 → preserved (MVP scope, §Microsoft 365 / Exchange Integration Requirements).
- 3 → **strengthened** into FR81a shared command pipeline (architectural invariant), beyond the brief's framing.
- 4 → preserved (§Context Ownership, §Integration List).
- 5 → **partially missing.** The brief's framing — "scheduled, file-triggered, and conversation-triggered work" — is reduced to conversation-triggered only in MVP. The auditability framing is preserved.
- 6 → preserved.
- 7 → preserved (FR41, §Risk classification defaults).

**Gap classification.** See Gap 5 above. The brief's differentiator 5 collapses from three trigger types to one. This is the same gap as bullet 4 in the Solution section.

### Who This Serves (brief lines 62–70)

**Brief intent.** Four named stakeholder personas with explicit success criteria:
- **Business teams** — "collaborate with customers, partners, suppliers, and internal specialists." Success: *"email discussions, documents, and action items become organized project work instead of scattered correspondence."*
- **Project managers** — "visibility into what was requested, what the AI did, which files changed, and which tasks are complete." Success: *"less coordination overhead and fewer manual follow-ups."*
- **Developers and automation builders** — "reliable command surface for integrating project collaboration into scripts, services, and AI agents." Success: *"the CLI and MCP server can perform the same operations as the UI without custom one-off integrations."*
- **Platform operators and administrators** — "tenant isolation, identity, access control, service composition, and operational evidence." Success: *"external collaboration and AI automation can be governed with the same rigor as other enterprise services."*

**PRD coverage.**
- The four personas are preserved and expanded into eight user journeys (UJ1–UJ8) plus the System Journey, plus an RBAC matrix (lines 706–720) with ten role classes.
- The brief's qualitative success criteria are preserved in the Journey narratives (Journey 1 Amira ≈ "business teams"; Journey 5 Nora ≈ "platform operators"; Journey 6 Leo ≈ "developers/automation builders"; Journey 4 Priya ≈ "project owners/managers").
- The brief's stakeholder named "Business teams need to collaborate with customers, partners, suppliers, and internal specialists" maps to UJ1 + UJ3 but the **brief's emphasis on "without forcing everyone into the same tool"** is preserved in UJ3 (Elena) — this signal survived.

**Gap classification.** Preserved. The personas survived the formalization process cleanly.

### Success Criteria (brief lines 72–84)

**Brief intent.** Six primary success measures, four of which are framed as **business-outcome metrics** rather than technical metrics:

1. **Task completion rate** — percentage of project tasks completed successfully after being created from conversation, file, or schedule triggers.
2. **Reduced project coordination time** — measurable reduction in time spent finding context, following up, and translating email threads into work.
3. **Document management automation** — percentage of incoming files classified, stored, linked to the right project folder, and made available to AI workflows without manual handling.
4. **Cross-surface command parity** — core chatbot UI operations are also available through CLI and MCP with consistent authorization, validation, and outcomes.
5. **External collaboration reliability** — mailbox-driven conversations correctly preserve participants, attachments, project association, and task requests.
6. **Mailbox-to-project accuracy** — incoming messages and attachments are associated with the intended project.

**PRD coverage.**
- 1 (Task completion rate) → **missing.** The PRD has no task-completion-rate metric. Task lifecycle management is explicitly out of MVP scope (§MVP scope-limit: *"Task intent is limited to detecting candidate action requests from associated project email and surfacing them for governed review; it does not include full task lifecycle management"*). The brief's measure does not fit the PRD's MVP, and the PRD has not added a replacement business-outcome metric to anchor the same intent.
- 2 (Reduced project coordination time) → **partially preserved as A11.** §Measurable Outcomes states "Median time from email receipt to available governed project context is reduced by at least 40% compared with the pilot tenant's current manual email-to-project update process, measured against the A11 baseline." This is narrower than the brief's framing.
- 3 (Document management automation) → **partially missing.** The PRD has FR29–FR34 (attachment capture/storage) but no metric for "percentage of incoming files classified, stored, linked to the right project folder, and made available to AI workflows without manual handling." The brief's metric framed file management as a measurable success outcome; the PRD treats it as a capability.
- 4 (Cross-surface command parity) → preserved and strengthened (FR81a invariant; §Cross-surface parity outcomes).
- 5 (External collaboration reliability) → preserved (§User Success; §Business Success).
- 6 (Mailbox-to-project accuracy) → preserved and strengthened (Association quality outcomes; A9a calibration targets).

**Gap classification.**
- **Gap 6 (Missing):** Brief success metric 1 (task completion rate) is not in the PRD and has no replacement business metric. This is partly because the MVP descopes task lifecycle, but the brief's underlying intent — *do governed tasks actually complete?* — should still be a measurable outcome even at MVP scale (e.g., "AI action approval-to-execution success rate" or "approved AI action completion rate"). NFR46 mentions rubber-stamp approval rate but not completion-after-approval.
- **Gap 7 (Missing):** Brief success metric 3 (document management automation percentage) is not in the PRD. The "without manual handling" framing is the actual business outcome; the PRD captures the capability but not the success rate.

### MVP Scope (brief lines 86–110)

**Brief intent.** Thirteen MVP-in-scope items, five MVP-out-of-scope items. The brief frames MVP success as: *"create or select a project, invite or identify participants as parties linked to email addresses, receive project email, store attachments in the project folder, create or request an AI task from the conversation, request approval when the task will modify folder content or send external information, execute the task through EventStore-backed commands, and show the outcome consistently in the UI, CLI, and MCP surfaces."*

Key brief MVP items the PRD has changed:
1. **"Chatbot UI for project conversations and task requests"** — the PRD does not deliver a chatbot UI in M0; it delivers a project conversation view + association review + approval. (See Gap 1.)
2. **"Mailbox integration for internal and external project communication"** — preserved in M0.
3. **"Support for both enterprise mailbox integration and generic email integration"** — narrowed to M365/Exchange only. (See Gap 2.)
4. **"Project participation model for internal and external users, backed by parties linked to email addresses and tenant-aware security"** — preserved (FR13–FR20).
5. **"File ingestion into project folders from user upload and mailbox attachments"** — **partially missing.** The PRD has mailbox-attachment ingestion (FR29–FR34) but does not explicitly preserve "user upload" as an M0 file source. M0 is mailbox-attachment-only.
6. **"AI task requests from conversation actors"** — preserved.
7. **"Automated task triggers from scheduled time and file addition"** — **out of scope in PRD.** (See Gap 5.)
8. **"Human approval gates"** — preserved and strengthened.
9. **"CLI console app exposing the same core project, conversation, file, and task commands as the UI"** — preserved but **deferred to M1.** Brief framed CLI as MVP-day-one; PRD treats it as M1.
10. **"MCP server exposing the same core capabilities for AI agents"** — same as 9, deferred to M1.
11. **"EventStore-backed command/query flow"** — preserved.
12. **"Aspire-composed local development/runtime topology"** — preserved.
13. **"Keycloak-backed authentication"** — preserved.

The PRD's increment-sequencing addresses the resource concern (a `[NOTE FOR PM]` at lines ~938–939 explicitly says *"the original PRD (pre-2026-05-28 update) treated the must-have list as a single shippable release. The adversarial review correctly identified that as a 6–12 person-year backlog packaged as one release."*). The three-increment sequencing is a legitimate scoping move, and the PRD is honest about why.

**Gap classification.**
- **Gap 8 (Missing — possibly intentional but not flagged):** Brief MVP item 5 (file ingestion **from user upload**, not only mailbox) is not in M0. The PRD's M0 is mailbox-attachments-only. User-upload may be in M1 or M2 but is not explicitly named anywhere.
- **Gap 9 (Acknowledged):** CLI and MCP have been deferred from MVP-day-one (brief framing) to M1. The PRD explicitly acknowledges this in §MVP at line ~219: *"Deferring CLI/MCP from M0 to M1 weakens the parity thesis between increments."* This is a legitimate sequencing decision and is logged.

### Technical Approach (brief lines 112–118)

**Brief intent.** The brief names **two design risks "[that] deserve early validation"**:
1. *"Mailbox messages need a reliable project association strategy so external email does not pollute the wrong workspace."*
2. *"External participant access must be enforced before task execution or file access, not only at the UI layer."*

The brief's framing is: ChatBot remains an **"orchestrating application over dedicated Hexalith services, not a monolith that owns every domain concern."**

**PRD coverage.**
- Both design risks are preserved and elevated. Risk 1 is the foundation of FR1–FR12, the §Confidence Thresholds, §Risk Classifier, and the entire Association Lifecycle. Risk 2 is the foundation of NFR1, NFR2, NFR7, NFR15a (fail-closed contract).
- The orchestration framing is preserved in §Context Ownership and §Data Governance Surface.

**Gap classification.** Preserved and strengthened.

### Vision (brief lines 120–124)

**Brief intent.** The brief casts a 2–3 year vision: **"a governed agentic collaboration platform: project-aware AI workers, richer scheduled and event-driven automations, multi-channel conversations, task and document intelligence, audit-ready execution history, and reusable MCP tools that let external AI assistants participate safely in enterprise project work."**

Key voice signals:
- *"governed agentic collaboration platform"* — the word "agentic" is the brief's framing for the AI direction.
- *"reusable MCP tools that let external AI assistants participate safely"* — the brief is positioning Hexalith.ChatBot as exporting MCP tools, not just consuming them. This is a different framing from "MCP server exposes our operations for our AI agents."

**PRD coverage.**
- The vision is preserved in §Product Scope → §Vision (Future) and §Innovation section: *"reusable project-aware AI workers, multi-channel conversation capture, governed task execution, audit-ready action history, document intelligence, reusable MCP tools, and consistent command access across human and machine surfaces."*
- The word "agentic" survives only in the §Vision section. The brief's framing — that this is fundamentally an *agentic* product — is preserved but does not pervade the PRD prose.
- The brief's MCP-as-export framing ("MCP tools that let external AI assistants participate safely") is preserved in §Vision but not strongly carried into the FR81a/FR83 MCP scope, which focuses on MCP as a *consumer* surface for governed operations rather than as an *exporter* of reusable tools.

**Gap classification.**
- **Gap 10 (Partial):** The brief's MCP-as-exporter framing ("reusable MCP tools that let external AI assistants participate safely in enterprise project work") is preserved in vision but the FR/NFR catalog treats MCP only as a consumer surface for the same operations as UI/CLI. If reusable MCP tools become a product, the FR catalog needs to evolve. Acceptable for MVP, but worth noting.

## Cross-cutting observations

### Voice and tone signals lost in formalization

The brief uses qualitative, user-facing phrases that the PRD has formalized into FR/NFR language. The following are not gaps in capability but are gaps in *voice that survives outside the implementation team*:

- **"I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context."** (Brief line 47, the user-facing promise.) Not in the PRD. Sales, marketing, pilot recruiting, and stakeholder conversations need this kind of single-sentence anchor; the PRD's Executive Summary is a six-paragraph governance argument.
- **"Project-first AI context"** as a tagline (brief line 54). The PRD has the substance ("AI as a governed project actor") but the brief's three-word framing is gone.
- **"Email as a first-class collaboration channel"** (brief line 55). The PRD says the same thing in 100 words.
- **"Automation with auditability"** (brief line 58). The PRD has the substance across FR55, NFR49, NFR49a, NFR50, NFR50a but the brief's pithy framing is gone.

### Over-formalization

The PRD is generally *not* over-faithful to brief language. The opposite: the PRD has consistently chosen formal precision over the brief's prose voice. The one place where the PRD over-reproduces brief language at the cost of clarity is the Executive Summary, which now contains both:
- The brief's governance argument (preserved).
- A `[NOTE FOR PM]` acknowledging the product name is misleading.
- A self-disclaimer (line 70: *"This is the product team's working thesis pending pilot validation per the Measurable Outcomes section, not an external market claim — see §Market Context & Competitive Landscape for the explicit disclaimer."*).

These three together make the Executive Summary read defensively. The brief read confidently. This is not a correctness gap — it's a tonal one introduced by the validation-finding remediation.

### Stakeholders explicitly named

The brief names no specific human stakeholders (no Jerome, no pilot tenant, no first customer). The PRD adds named author "Jerome" (line 53) and named ownership roles (System Architect, Test Architect, Product Lead, Security Engineer). Owner-role formalization is an improvement and not a gap.

### Pain points explicitly named

The brief names six concrete pain points that survive in the PRD only in journey narratives:
1. "context is scattered, attachments are duplicated, actions are implicit, and project state is hard to audit" — survives as UJ1, UJ3.
2. "spend time reconstructing decisions, finding the latest document, and translating conversation into executable work" — survives as UJ2.
3. AI assistants "lack a durable project boundary" — survives as System Journey + Innovation section.
4. "which files are authoritative, who is allowed to participate, which task was requested, what changed" — survives as FR23, FR55 audit fields.
5. "whether the action was triggered by a schedule, a file arrival, a mailbox message, or a user instruction" — *partially missing* because three of those four trigger types are out of MVP scope.
6. "AI stays useful but unreliable for enterprise execution" without a governed workspace — survives as the §Innovation thesis.

## Summary of gaps (consolidated)

| # | Gap | Brief location | PRD coverage | Severity |
|---|---|---|---|---|
| 1 | "Chatbot UI" framing not in M0 (no conversational chat surface; instead conversation view + review + approval) | Executive Summary, MVP item 1, Solution bullet 2 | §MVP Increment M0 + `[NOTE FOR PM]` at line ~307 | Acknowledged but not flagged as a stakeholder-expectation risk |
| 2 | Generic email integration narrowed to M365/Exchange only, gated by A1 | Executive Summary, MVP item 3 | §MVP scope note + A1 | Acknowledged |
| 3 | Three of four automation triggers (schedule, file-add, user-instruction) out of MVP | The Problem, Solution bullet 4, Differentiator 5 | §MVP Out-of-Scope, §Growth | Acknowledged but trigger-roadmap not committed |
| 4 | User-facing promise sentence ("I can collaborate simply…") not in PRD | Brief line 47 | Not preserved | Missing — voice/anchor gap |
| 5 | Brief MVP commitments to multi-channel chat UI and full automation triggers demoted without `.decision-log.md` entry | MVP section | §MVP Out-of-Scope | Missing — process gap |
| 6 | Task completion rate as success metric not in PRD; no replacement business metric | Success Criteria measure 1 | Not preserved (task lifecycle out of MVP) | Missing — success metric gap |
| 7 | Document management automation percentage as success metric not in PRD | Success Criteria measure 3 | Capability captured (FR29–FR34) but no success-rate metric | Missing — success metric gap |
| 8 | User-upload file ingestion not explicitly in M0 (mailbox-only) | Brief MVP item 5 | M0 attachment capture is mailbox-only | Partial — not flagged |
| 9 | CLI and MCP deferred from MVP-day-one to M1 | Brief MVP items 9, 10 | §Increment M1; explicitly logged at line ~219 | Acknowledged |
| 10 | MCP-as-exporter framing ("reusable MCP tools for external AI assistants") not carried into FR/NFR catalog | Vision | Preserved in §Vision only | Partial — acceptable for MVP |

## Recommendations

1. **Re-introduce the user-voice anchor.** Add the brief's user-facing promise ("I can collaborate simply with others on a project or subject, and the AI can help move the work forward without losing context") to the Executive Summary or to the §Vision section as a quoted user voice. This is the kind of artifact that survives a sales conversation; FR/NFR catalogs do not. (Addresses Gap 4.)

2. **Record the deliberate scoping departures in `.decision-log.md`.** The PRD has made legitimate scoping moves (multi-channel chat UI deferred; generic email gated; three of four trigger types deferred; CLI/MCP deferred to M1; user-upload not explicit; task completion rate not measured). These are correct decisions but they are departures from the brief's commitments. The PRD's `[NOTE FOR PM]` blocks acknowledge some of these; `.decision-log.md` should hold the others as named decisions with rationale. (Addresses Gaps 1, 2, 3, 5, 8.)

3. **Add at least one MVP-scale completion-success metric.** The brief's success metric 1 (task completion rate) and metric 3 (document management automation rate) both intend to measure *whether the system actually completes governed work*. The PRD has NFR46 rubber-stamp-rate and §Measurable Outcomes for association accuracy, but no metric for "approved AI actions that completed successfully" or "attachments correctly stored in target folder without manual handling." Adding one MVP-scale completion-success metric (anchored to the M0 vertical loop: e.g., "≥ X% of M0 approved AI actions execute and project as expected") would preserve the brief's success-outcome intent without requiring full task lifecycle. (Addresses Gaps 6, 7.)

4. **Note the post-MVP trigger roadmap.** Brief Solution bullet 4 ("Trigger automated tasks by schedule, by adding a file, or by a request from a conversation actor") is currently 25% delivered in MVP. The post-MVP trigger roadmap (schedule-triggered → M3+, file-add-triggered → M3+) should be explicit in §Growth Features so the brief's commitment is visible as a roadmap rather than as deferred scope. (Addresses Gap 3.)

5. **Re-inject voice in the Executive Summary.** The current Executive Summary reads defensively (governance argument + `[NOTE FOR PM]` + self-disclaimer). Consider a shorter, more confident opening paragraph that re-uses the brief's pithy framings ("Project-first AI context", "Email as a first-class collaboration channel", "Automation with auditability") before the governance argument begins. The substance is correct; the tone has become apologetic. (Quality, not gap.)

## What the PRD does *better* than the brief

- **FR81a shared command pipeline as architectural invariant.** The brief framed parity as a goal; the PRD makes parity a property of the architecture. This is a meaningful upgrade.
- **Fail-closed contract (NFR15a).** The brief said "fail closed when association is ambiguous." The PRD has enumerated every code path that can write durable state and the fail-closed condition for each. This is the kind of thing the brief should have asked for and didn't.
- **Three-increment M0/M1/M2 sequencing with named team sizing.** The brief implicitly treated MVP as one shippable release. The PRD's `[NOTE FOR PM]` at line ~938 acknowledges this was a 6–12 person-year backlog packaged as one release and sequences it honestly. This is a real planning improvement.
- **Explicit assumptions A1–A11 with owners and revisit conditions.** The brief had implicit assumptions; the PRD has named them.
- **Data Governance Surface table.** The brief said "ChatBot does not own records." The PRD correctly identifies that ChatBot *does* own substantial derived state (associations, candidate rankings, evidence snapshots, AI action proposals, approval records, projections, policy snapshots, vector indexes) and treats those as first-class durable records with retention, redaction, isolation obligations. This is a correction the brief did not see.

## Verdict

The PRD is a faithful and substantially stronger derivation of the brief on every governance, technical, and quality dimension. The losses are primarily in **voice, user-facing framing, and brief-committed scope reductions that should be tracked in `.decision-log.md`** rather than only acknowledged in `[NOTE FOR PM]` blocks. The three success-metric gaps (task completion rate, document management automation rate, user-upload file ingestion) are real but tractable additions.

The PRD does not over-faithfully reproduce brief language at the cost of clarity. The opposite: it has consistently chosen formal precision over the brief's prose voice. The recommendation is to re-inject some of that prose voice deliberately, not to revert the formalization.
