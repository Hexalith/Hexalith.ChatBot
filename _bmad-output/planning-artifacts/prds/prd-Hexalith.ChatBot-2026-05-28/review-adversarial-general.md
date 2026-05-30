# Adversarial Review (Cynical) - Hexalith.ChatBot PRD

**Reviewed artifact:** `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md` (~1260 lines)
**Reviewer stance:** Cynical senior reviewer. No false balance. Looking for what will fail, what is hand-waved, and what looks like theater.
**Date:** 2026-05-28

---

## Verdict

A 1,260-line incantation of governance vocabulary that promises a fully governed, fail-closed, cross-surface, multi-tenant, audit-perfect, AI-mediated, idempotent email-to-project platform — and then calls it an MVP delivered in one release. The PRD repeats the same five risk-management adjectives in every section while quietly punting nearly every quantitative target to "tenant policy," "evaluation dataset," "operating baseline," or "depends on A1-A9." It has a clear thesis and a usable lifecycle model, but the so-called "minimum slice" (line 213) is wider than most teams ship in two quarters, and the precision/recall, latency, RPO/RTO, adoption, and accessibility numbers are presented as commitments without a single referenced baseline measurement. This is not yet a buildable PRD — it is a maximalist policy manifesto that hopes architecture and stories will silently absorb the scope it refuses to cut.

---

## Findings

### Critical

#### C1. "Minimum Release Slice" is wider than the rest of the PRD admits — the MVP is not an MVP
**Section:** "Minimum Release Slice" (lines 213-229); "Must-Have Capabilities" (lines 865-893)
**Severity:** Critical

The "minimum" slice (lines 215-225) enumerates nine workstreams: M365/Exchange mailbox ingestion, deterministic association, ambiguous association UI with confirm/reject/defer/correct, attachment governance with quarantine, task intent detection, AI risk classification, approval routing, allowlisted command execution, outbound draft/send with sender authority, UI review workflow, CLI **or** MCP parity with contract tests, and a defined set of audit events. The "must-have capabilities" list (lines 867-893) then expands this into 25 bullets including "strict tenant isolation across UI, CLI, MCP, workers, M365 events, service clients, AI actors, projections, indexes, and audit views," Keycloak integration, Aspire composition, dependency failure handling for seven dependency classes, and "security and isolation acceptance tests across [nine] actor types." This is not a vertical slice; it is the entire product. The PRD's own risk section concedes "the release is broad for a single MVP" (line 942) and then refuses to cut anything because "trust requires the full vertical path." Trust may require it; engineering capacity does not produce it on a single release. Without an explicit, ruthless sequencing of which subset is shippable in increment 1 versus increments 2-N, this PRD will either slip indefinitely or quietly drop fail-closed behavior under deadline pressure — which the PRD itself names as the worst possible outcome (line 943).

#### C2. Precision/recall targets have no provenance and no dataset behind them
**Section:** "Association quality outcomes" (lines 141-142); A9 (line 1159)
**Severity:** Critical

Line 141 commits to "at least `95% precision` and `90% recall` for non-ambiguous messages" and "`0` critical false-positive associations involving unauthorized projects" against "seeded evaluation datasets." These numbers appear with no reference to a baseline measurement, no statement of dataset size, no class balance, no labeling protocol, no inter-rater agreement, and no statement of who owns the dataset. A9 (line 1159) reveals the dataset itself is still an assumption: "can be built from consented, redacted, or synthetic examples." So the acceptance threshold is anchored to a dataset that does not exist yet, will be built by an unnamed party, and will be calibrated by the same team that has to hit the threshold — a textbook circular acceptance criterion. "0 critical false-positives" is mathematically achievable on a one-row dataset. This is not a measurable outcome; it is theater. Either commit to who builds the dataset, when, with what sampling frame, or stop quoting precise percentages.

#### C3. T_high and T_low are fictional knobs with no operating range
**Section:** "Technical Success" (line 120); RBAC mention (line 624); FR9 (line 1018)
**Severity:** Critical

Lines 119-121 introduce confidence thresholds `T_high` and `T_low` as tenant or deployment policy. FR9 (line 1018) lets tenant admins configure them. Nothing in the PRD specifies the underlying scoring function, the domain of the score, how a tenant admin would calibrate against precision/recall targets (C2) without statistical training, what happens if a deployment ships with bad defaults, or who validates a tenant's threshold change before it goes live. "Configurable and auditable" (line 120) is not a control — it is an off-ramp. A misconfigured `T_high` of 0.1 will auto-associate everything; the PRD never names a guardrail. The decision log records `T_high`/`T_low` ownership was "clarified" — the clarification is "tenant policy." That is the punt, not the resolution.

#### C4. "Fail closed" is repeated 20+ times but has multiple silent escape hatches
**Section:** Throughout — examples lines 116, 154, 472, 476, 595, 597, 716, 768, 771, 1109, 1184
**Severity:** Critical

Fail-closed behavior is the PRD's primary safety promise. Yet:
- NFR15 (line 1184) says invalid state transitions produce "an audit event when audit storage is available; if audit storage is unavailable, security-sensitive transitions must fail closed." So **non-security-sensitive** invalid transitions are allowed to silently mutate state when audit is down. Who decides which is which? On what surface? At what code path?
- NFR22 (line 1191) says "Non-AI review, association, approval, retry, and audit workflows must continue during AI provider outage." Fine — but combined with the line 771 promise that "association, audit, authorization, and manual resolution workflows remain usable" without AI, this means risk classification (FR39, line 1071) cannot run, and therefore approval routing cannot run, and therefore the "approval-required" risk class collapses to "human guesses without classifier." The PRD never specifies the degraded-mode policy.
- Line 476 says "Audit-write failure must fail closed for risky actions." This implicitly authorizes non-risky actions to proceed without audit. "Risky" is defined behaviorally (line 966), not statically — so a wrongly-classified action can write to project state with no audit because the classifier said "low-risk."
- FR68 (line 1109) says the system can fail closed on a list of conditions but says nothing about what happens when those conditions are partially resolvable.

The phrase "fail closed" is used as a token, not a contract. Each occurrence needs a code-path-level definition.

#### C5. Cross-surface parity is enforced by aspiration, not by an invariant
**Section:** "Cross-surface parity outcomes" (lines 161-167); FR81-FR86 (lines 1125-1130); "Parity Boundary" (lines 723-728)
**Severity:** Critical

The PRD promises that UI, CLI, and MCP "expose the same core governed operations" with "equivalent authorization outcomes, state transitions, audit behavior, and redaction semantics." There is no enforcement mechanism named beyond "contract tests" (line 224) and "automated parity tests" (line 167). The PRD describes parity as a quality the system *has* rather than as an invariant enforced by a shared backend. There is no architectural commitment that the three surfaces share a single command pipeline — only that they produce equivalent outcomes. Equivalent outcomes are tested by sample; they are not guaranteed by structure. The first time CLI gains a feature UI lacks (or vice versa), parity drifts and contract tests catch some fraction of the drift. For a security-critical surface, "we'll test for parity" is not equivalent to "we cannot construct a non-parity code path." This is exactly the kind of invariant nothing enforces — it has to be encoded at the architectural layer, not promised at the PRD layer.

#### C6. The "single release" containing all eight user journeys plus the system journey is unbuildable on the named team
**Section:** "Resource Requirements" (line 847); "Core User Journeys Supported" (lines 851-863)
**Severity:** Critical

Line 847 describes the team as: "product ownership, a system architect, backend/service engineers, a frontend engineer, a CLI/MCP engineer, a security/identity engineer, a QA/test architect, and DevOps support." Singular frontend engineer. Singular CLI/MCP engineer. Singular security engineer. The release must deliver eight user journeys plus the system journey, twenty-seven commands (lines 647-673), fourteen queries (lines 679-692), nine lifecycle states with documented transitions, three first-class surfaces with parity, WCAG 2.2 AA accessibility (NFR60, line 1247), full audit reconstructability, RPO ≤ 15 min / RTO ≤ 4 hours (NFR56, line 1240), seeded evaluation datasets, pilot with 4 consecutive weeks and 10 governed AI reviews (lines 175-180). For one frontend engineer. There is no team-load reconciliation anywhere. The decision log claims "MVP sequencing within the single release" was addressed, but sequencing-within-a-release is not the same as cutting scope. This is a 6-12 person-year backlog packaged as one release.

---

### High

#### H1. "Magic constants" appear with no derivation
**Section:** lines 176-180, 1172, 1196-1199, 1221, 1240-1241
**Severity:** High

A non-exhaustive list of unprovenanced numbers:
- "`4` consecutive weeks", "`2` monitored project mailbox patterns", "`5` active projects", "`70%`", "`40%`", "`30%`", "`10` governed AI action reviews" (lines 176-180) — pilot success thresholds with no anchor to prior pilot data, no statistical power analysis, no link to renewal/expansion behavior.
- "5 minutes" max staleness for cache, "60 seconds" for revocation (NFR6, line 1172) — no derivation, no link to a threat model.
- "p95 response target of 2 seconds" (NFR24), "10 seconds p95" candidate generation (NFR25), "5 seconds p95" CLI/MCP (NFR26), "30 seconds" timeout (NFR26), "100 items" page size (NFR27) — no link to user research, throughput model, or baseline measurement.
- "7 days" subscription expiry alert, "5 minutes" audit lag threshold, "2 business days" approval aging (NFR43, line 1221) — picked from a hat.
- "RPO ≤ 15 minutes", "RTO ≤ 4 hours" (NFR56, line 1240), "4 hours" projection rebuild (NFR57, line 1241) — no anchor to data class criticality.

The PRD treats every number as a default that "tenant or deployment profile" can override (A4, line 1154). That is the punt. A "default" with no derivation is a guess wearing a number.

#### H2. "Tenant policy" is the universal escape hatch
**Section:** A4, A5, A6 (lines 1154-1156); NFR9, NFR23 (line 1175, 1195); many others
**Severity:** High

The PRD defers to "tenant policy" or "tenant or deployment profile" for: confidence thresholds, AI provider telemetry/training/retention/region (A5), audit retention (A6), low-risk AI permission, residency boundaries, performance baselines, notification routing, retry policy, rate limits, accessibility-related thresholds, consent capture rules, RBAC restrictions, and more. There is no master list of policy knobs and no policy schema. Without a policy contract:
1. A tenant admin must configure a system whose policy surface is implicit and undocumented.
2. Security and architecture cannot validate that the policy schema is closed under safe states.
3. Acceptance tests cannot enumerate the policy combinations under test.
4. "Tenant policy" becomes the place engineering hides every unresolved decision.

Either define the policy schema as an artifact and put it under change control, or stop using "tenant policy" as a hand-wave.

#### H3. Risk classification is described as a system capability without naming the classifier
**Section:** "Task Intent and AI Action Mediation" risk table (lines 1056-1063); FR39 (line 1071)
**Severity:** High

FR39: "The system can classify AI action requests by risk." Nothing in the PRD says what classifies them. Heuristic rules? Tags in the action proposal? LLM classification? A static allowlist mapped to risk classes? The risk table examples (line 1061) include "expose file content in generated output" — to determine whether an action exposes file content, the system must reason about model output, which is non-deterministic. The PRD's own line 462-466 GDPR concern depends on this classifier being correct, and the entire approval workflow depends on it being correct. The decision log does not record this as resolved. The PRD calls it a capability and moves on. If the classifier is wrong, the entire approval gate is theater.

#### H4. "Allowlisted commands" is the load-bearing security control with no allowlist
**Section:** Lines 199, 205, 211, 226, 502, 883, A8 (line 1158)
**Severity:** High

"Allowlist of project collaboration commands" appears repeatedly as the safety boundary for AI execution. The PRD does not name a single command in this allowlist. A8 (line 1158) records it as an assumption: "A fixed allowlist of project collaboration commands is sufficient for MVP AI action execution." The command catalog at lines 647-673 contains 27 commands, but several of them — `ExecuteApprovedProjectCommand`, `SendApprovedProjectEmail`, `CreateOutboundProjectEmailDraft`, `StoreEmailAttachmentInProjectFolder` — are *the* commands an AI would invoke. So either (a) the allowlist is the whole command list, in which case the allowlist concept is decorative, or (b) the allowlist is a subset that is unspecified. The acceptance tests cannot be written against an unspecified subset.

#### H5. The "evaluation dataset" is the load-bearing validation artifact and nobody owns it
**Section:** A9 (line 1159); lines 141, 170-172, 1139
**Severity:** High

The dataset is named as the basis for precision/recall (C2), pilot validation (line 171), regression history (FR92, line 1139), and "audit field presence for 100% of security-sensitive ... events in the validation dataset" (NFR50, line 1231). Owner of A9 is listed as "QA / Product" — a shared ownership that means nobody owns it. There is no specification of: dataset cardinality, label taxonomy, source mailbox provenance, redaction protocol, periodic refresh cadence, drift handling, or the protocol for adding adversarial examples. Without these, NFR50's "100%" target is a tautology and the precision/recall targets are unfalsifiable.

#### H6. "Audit completeness" is invoked as a metric but never defined
**Section:** Lines 159, 162, 466, 795, NFR50 (line 1231)
**Severity:** High

The PRD repeatedly promises measurable audit completeness across an enumerated list of event types. It does not define the metric. Is it "fraction of events that carry all required fields"? "Fraction of operations that produced at least one audit event"? "Fraction of operations whose audit chain can reproduce the operation"? NFR50 promises "100% required field presence" on the evaluation dataset — fine, but that is a field-presence test, not a completeness test, and it measures completeness in the test corpus, not in production.

#### H7. Idempotency is promised across heterogeneous operations with no idempotency-key contract
**Section:** NFR13 (line 1182); FR90 (line 1137); line 698
**Severity:** High

NFR13 requires idempotency for "mailbox intake, attachment capture, association decisions, approvals, command execution, outbound communication, notifications, and audit projection ... with a stable idempotency key, replay window, conflict response, and the same final observable state for repeated equivalent inputs." Each of these operation classes has different identity semantics — mailbox events have message IDs, approvals have decision IDs, outbound emails have draft IDs, notifications have... what? The PRD doesn't say what an idempotency key is composed of, how long the replay window lasts (a day? a year? per workflow?), or what "equivalent inputs" means when, say, a duplicate approval comes in with a different actor at a different time. "Equivalent inputs" is doing enormous work in this sentence.

#### H8. Approval fatigue is acknowledged but the mitigation is hand-waved
**Section:** Line 547; NFR46 (line 1224)
**Severity:** High

The PRD admits approval fatigue as a "second [innovation] risk" (line 547) and again at NFR46: "The system must prevent approval fatigue by prioritizing, grouping, and suppressing duplicate or low-value notifications according to tenant policy." Prioritization heuristic? Grouping criteria? Suppression policy? Tenant policy again (see H2). If approval fatigue is a primary failure mode of governed AI (and it is), it deserves a designed flow, not a one-line NFR. The very first time a user has 40 approvals in their queue, they will rubber-stamp them, and the governance promise dies that day.

#### H9. The lifecycle table claims `Rejected` and `Failed` are terminal "unless reprocessed" — that is not terminal
**Section:** Lines 380-392
**Severity:** High

The Canonical state definitions table marks `Rejected` and `Failed` as "Yes, unless reprocessed." That is the definition of a non-terminal state. `Skipped` is marked "Yes" with the same reprocess loophole described in line 392 ("Skipped items are terminal unless an authorized reprocess command creates a new workflow instance"). Saying "this is terminal unless someone restarts it" reduces "terminal" to "currently inactive." A real lifecycle requires either (a) marking these as terminal and forcing reprocess to create a new workflow instance with a new ID, or (b) admitting they are non-terminal and modeling the reprocess transition. The PRD does both at once.

#### H10. WCAG 2.2 AA + screen-reader review + keyboard-only review for a complex governance UI on one frontend engineer
**Section:** NFR60 (line 1247); team list line 847
**Severity:** High

NFR60 commits to WCAG 2.2 AA "expectations" with automated checks plus keyboard-only and screen-reader review of ambiguous association, approval, retry/failure, audit lookup, and authorization-denied workflows. The complexity of the ambiguous-association UI alone (multi-candidate ranked list with evidence snippets, confidence bands, reject/defer/correct/escalate actions, audit trail, redaction) is a multi-month accessibility effort. The team has one frontend engineer (C6). Either the bar is theater or the team is wrong; both cannot be true.

#### H11. "No admin/debug bypass in MVP" combined with required tenant-admin operational dashboards is unworkable
**Section:** Line 694; FR53, FR67-FR75
**Severity:** High

Line 694 declares "There is no admin/debug bypass in MVP." Excellent. But the same PRD requires tenant admins to operate mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, audit projection (FR67), and to review degraded mailbox status (FR53). Reviewing "authorization failures" without an authorization-bypass surface means a tenant admin can see "user X failed to access resource Y" without being able to see resource Y. That is fine in principle but creates a complex permission model for admin tools that the PRD doesn't describe. The PRD waves "tenant-scoped" at the problem and moves on.

---

### Medium

#### M1. "Source context" date pin is a future-fragility hazard
**Section:** Lines 88-90
**Severity:** Medium

The PRD pins itself to "Hexalith module planning context available in the repository on 2026-05-28" and warns that if "any sibling bounded-context contract, PRD, or project-context artifact changes materially after this date, architecture must re-check ChatBot integration assumptions." Given that the repo has nine sibling submodules (Conversations, Projects, Folders, Parties, Tenants, EventStore, FrontComposer, Memories, Commons), this is effectively a guarantee that re-check is required by the time architecture starts. The PRD does not define what "materially" means, who triggers the re-check, or how the re-check feeds back into the PRD. A bound that nobody enforces is not a bound.

#### M2. "Hexalith.ChatBot does not own core records" — but it owns derived state nobody else owns
**Section:** Lines 64-65; lines 428-435; lines 580-589
**Severity:** Medium

The PRD repeatedly claims ChatBot owns "orchestration concerns" and "AI-mediated collaboration workflows" and references everything else by ID. But association decisions, candidate rankings, evidence snapshots, association-correction events, AI action proposals, approval records, command-surface attribution, derived projections, policy snapshots, and the entire association lifecycle are owned by ChatBot — and these are durable, security-sensitive records. The "we don't own records" framing is true only by definition (ChatBot owns ChatBot's records, not Projects' records). It is misleading because it minimizes the actual data-governance surface that has to be implemented.

#### M3. "Capability described at the noun level" — examples throughout
**Section:** FR21-FR34, FR55-FR63, FR67-FR80
**Severity:** Medium

A representative sample of FRs that read like topic headings rather than acceptance contracts:
- FR22: "The system can represent associated email, participants, attachments, decisions, approvals, failures, and AI outcomes in the project context." (line 1037) — that is seven first-class concerns in one bullet.
- FR55: "The system can produce audit records for security-sensitive association, participant, file, approval, command, AI, retry, and duplicate-handling events." (line 1093) — eight event families in one FR.
- FR67: "The system can expose mailbox health, unresolved-party queues, ambiguous-association queues, approval queues, retry failures, duplicate suppression, authorization failures, and audit projection status." (line 1108) — eight dashboards in one FR.
- FR74: thirteen capabilities in one bullet (disable, quarantine, rate-limit × mailbox sources, service clients, AI actors, command capabilities × four trigger conditions).

The PRD signals "Each FR group must be decomposed into acceptance scenarios" (line 986). Until that decomposition exists, every one of these is a story-creation hazard.

#### M4. Cross-tenant cache pollution is mentioned once and not addressed
**Section:** Line 593
**Severity:** Medium

Line 593 lists "cache entries, vector/index artifacts, integration tokens, background jobs" among things that must be tenant-isolated. Vector index artifacts and embedding stores have well-known multi-tenancy traps (shared collections, accidental cross-tenant nearest-neighbor returns, prefix-leak side channels). The PRD names the concern in one sentence and addresses it nowhere. NFR11 (line 1177) reiterates the zero-tolerance promise. The promise is fine; the implementation is unaddressed.

#### M5. "Sender authority" is invoked repeatedly without a definition
**Section:** Lines 486, 738, 1083, 1085, FR48
**Severity:** Medium

FR48 (line 1083) distinguishes "draft-only, authenticated-user send, shared-mailbox send, send-on-behalf, and approved service-send authority." That is a five-class taxonomy of outbound authority that maps to Microsoft Graph permission models that themselves vary by tenant configuration. The PRD names the taxonomy and stops. Who maps a given outbound action to an authority class? What happens when a user has send-on-behalf in M365 but not the equivalent ChatBot grant? The PRD says "must be explicit" without saying how.

#### M6. "Visible failure states" do not define a UX contract
**Section:** FR66, FR69, FR76-FR79; NFR17, NFR39
**Severity:** Medium

The PRD repeatedly promises visible failure states. It does not say what "visible" means. A row in a queue? A toast? An email notification? A row in a queue *and* an email *and* an audit event? The notification routing (FR73) is configurable, which means tenants will turn it off and "visible" will become "queryable if you know where to look." That violates the spirit of FR68's fail-closed promise.

#### M7. "Stable identifiers across bounded contexts" — yet ChatBot does not control identifier minting
**Section:** Lines 492-493, 588
**Severity:** Medium

ChatBot uses IDs minted by Projects, Parties, Folders, Tenants, EventStore. The PRD promises that decisions stay correlatable via these IDs. Nothing in the PRD addresses what happens when a sibling context renames, splits, merges, or deprecates an ID — a known hazard in long-lived bounded-context products. Without an explicit ID-evolution contract, audit records pinned to a Project ID that was later split into two projects become irreproducible.

#### M8. "Replay" and "simulation" are promised at the same surface as production
**Section:** FR95 (line 1142); NFR69 (line 1259)
**Severity:** Medium

FR95 says the system can "simulate or replay representative mailbox events for authorized QA or support investigation without sending external communication or mutating production project state." NFR69 says "replay artifacts must be explicitly labeled and tenant-scoped." The mechanism that prevents replay from sending external mail or mutating state is not named. A flag on the command? An interception in the outbound adapter? A separate test tenant? "Without mutating production state" is exactly the kind of safety property that needs an architectural enforcement mechanism, not an FR clause.

#### M9. The "correction" path may invalidate AI context but the invalidation cost is hidden
**Section:** Journey 4 (lines 302-310); FR91 (line 1138)
**Severity:** Medium

Journey 4 promises that when Priya corrects a wrong association, "the system ... refreshes or invalidates AI context indexes." This is one sentence describing a potentially very expensive operation: every derived index, every cached prompt context, every search index entry, every vector embedding that touched the corrected association must be invalidated. The cost is hidden inside a noun-level FR (FR91, "rebuild derived projections from source records"). On a real system, this is a multi-minute to multi-hour background job per correction; the PRD treats it as a UI action.

#### M10. "External party participation that preserves normal email collaboration" hides a phishing/spoofing problem
**Section:** Lines 296-300; Journey 3
**Severity:** Medium

Journey 3 says the system fails closed on "sender spoofing/mismatch." Email sender spoofing detection requires DMARC/DKIM/SPF chain validation, header inspection, and operates against an adversary who controls the sending domain. The PRD names "sender spoofing/mismatch" in one phrase. The MVP must run against real corporate email and so will encounter forwarded threads from `outlook.com` aliases, mailing-list rewrites, on-behalf-of headers, and external-sender warnings injected by tenant MTAs. None of this is addressed.

#### M11. "MVP must define" appears as a deflection
**Section:** Lines 462, 466, 484, 487, 638, 783, 787, 805
**Severity:** Medium

The phrase "the MVP must define" appears repeatedly as a stand-in for "we have not decided this yet." Examples: "must define what personal data is captured" (line 462), "must define where tenant project metadata ... is stored" (line 787), "MVP must define service-client permissions" (line 638). The PRD is the place to define these. Saying "the MVP must define X" inside the PRD is the PRD deferring to itself.

#### M12. The eight user journeys force eight UI surfaces with no UI inventory
**Section:** Journeys 1-8 (lines 262-356)
**Severity:** Medium

Each journey implies a distinct UI: project conversation view (J1), ambiguous-association resolution UI (J2), external email path (J3 — no UI), correction UI (J4), tenant-admin configuration UI (J5), CLI (J6), compliance investigation UI (J7), AI action review UI (J8). The PRD describes none of these as designed screens. UX design is not in this PRD; it is referenced as a separate artifact. So the PRD is committing to eight UIs without any UX scoping, which leaves the frontend engineer (C6) implementing against journey prose.

---

### Low

#### L1. "Hexalith.ChatBot" is the product name and the conversation is about email — naming mismatch
**Section:** Title
**Severity:** Low

The product is called "ChatBot," and the MVP is exclusively about email. The mismatch will create internal confusion (every new hire will ask "where is the chat?"). Either rename the product or make the chat surface a first-class MVP concern. The PRD currently has it both ways: line 60 talks about "chatbot UI" as the third parity surface, but nothing in the FRs describes a conversational UI in detail.

#### L2. "Aspire" is named as a dependency but never described
**Section:** Lines 88, 753, 847, 889
**Severity:** Low

Aspire appears as an MVP integration and a development/runtime composition tool. It is never explained. A reviewer outside Hexalith would not know it is .NET Aspire. Not load-bearing but a documentation gap.

#### L3. "FrontComposer" appears only in classification context
**Section:** Line 88
**Severity:** Low

Hexalith.FrontComposer is named once as a dependency but does not appear in the integration list (lines 745-755) or anywhere in FRs/NFRs. Either it is needed and missing from the integration list, or it is not needed and should be removed from the classification context.

#### L4. Audit "tamper-evident" is asserted without a mechanism
**Section:** NFR49 (line 1230)
**Severity:** Low

Tamper-evidence has well-defined mechanisms (hash chains, append-only WORM stores, signed envelopes). The PRD asserts the property without naming the mechanism. Architecture can fill this in, but PRDs that promise "tamper-evident" without naming a class of mechanism set up the team to ship a database with an "updated_at" column and call it audit.

#### L5. Glossary skips load-bearing terms
**Section:** Glossary (lines 953-969)
**Severity:** Low

The glossary defines "Actor," "AI actor," "Approval," etc. It does not define "fail closed," "low-risk," "approval-required," "policy snapshot," "operating baseline," "MVP parity set," or "evaluation dataset." Every one of these terms is load-bearing and used dozens of times. A glossary that omits the most important terms is a glossary that performs definition without delivering it.

#### L6. "Hexalith.ChatBot does not own X" appears in three places with three different lists
**Section:** Lines 64-65, 428-435, 580-589
**Severity:** Low

Each enumeration of ChatBot ownership boundaries differs slightly. Line 65 says "core project records, files, parties, identity, or event history." Lines 428-435 add "audit/compliance" as a separately-owned context. Lines 580-589 break this out further. None of the three lists exactly matches the others. Pick one and reuse it.

#### L7. "MCP" is used without expansion
**Section:** Throughout
**Severity:** Low

Model Context Protocol is never expanded or referenced. Any reviewer outside the AI-tooling community will not know what MCP is. The PRD treats it as a first-class surface; readers deserve a one-line definition.

#### L8. The PRD claims a "differentiating moment" but the differentiation thesis is technical, not market
**Section:** Lines 70-72; line 520
**Severity:** Low

Line 520 explicitly disclaims competitive market claims. Line 70 then asserts a "differentiating moment." Without market validation, a "differentiating moment" is a self-assessment. This is not a blocker but it is theater.

#### L9. The "innovation" section is largely a restatement of the requirements section
**Section:** "Innovation & Novel Patterns" (lines 508-552)
**Severity:** Low

The innovation framing ("AI as a governed project actor") is exactly what the FR section already requires. The innovation section adds no new constraints; it restates the same governance vocabulary at higher abstraction. Either delete it or use it to assert something genuinely new about the product.

---

## Counts

| Severity | Count |
| --- | --- |
| Critical | 6 |
| High | 11 |
| Medium | 12 |
| Low | 9 |
| **Total** | **38** |

---

## Top recommendations (cynical, not balanced)

1. **Cut the MVP.** Sequence the "minimum slice" (line 213) into three or more increments. Increment 1 covers deterministic association + UI review + audit lookup + one surface (UI). CLI/MCP parity, AI mediation, and outbound send move to later increments. Stop pretending this is one release.
2. **Build the evaluation dataset before quoting numbers.** Either commit a named owner, a sampling protocol, a target size, and a refresh cadence, or delete the precision/recall/false-positive numbers from the success criteria.
3. **Specify the policy schema as a first-class artifact.** Every "tenant policy" reference should resolve to a key in a documented schema with allowed values and safe defaults. Until that exists, "tenant policy" is a hand-wave.
4. **Make fail-closed an invariant, not a vibe.** Enumerate every code path that can write durable state and define what "fail closed" means at each one. NFR15's "security-sensitive" carve-out is a backdoor.
5. **Name the AI action classifier.** Heuristic? Tag-based? LLM? Hybrid? Whatever it is, name it and accept its error rate as a first-class risk. Right now the entire approval gate depends on a classifier nobody owns.
6. **Reconcile team to scope or scope to team.** The named team cannot ship the named release. Either grow the team or shrink the release.

---

## Halt conditions

None met — content is non-empty and findings are substantive.
