# Validation Report — Hexalith.ChatBot

- **PRD:** `_bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md`
- **Rubric:** `.claude/skills/bmad-prd/assets/prd-validation-checklist.md`
- **Run at:** 2026-05-28T15:30:00+02:00
- **Grade:** Poor

## Overall verdict

After two prior validation cycles the PRD has hardened into a usable contract — lifecycle states are reconciled, the FR/NFR catalog is contiguous and cross-referenced through the Traceability table, the minimum release slice names an executable first increment, and the Open Assumptions table is properly indexed and inline-linked. What still holds back the rubric review from "strong" is that zero `[NOTE FOR PM]` callouts and zero Open Questions mean every trade-off reads as resolved (smoothing tensions to neutral), and several FRs at the heart of the user experience (FR23, FR26, FR27, FR42, FR67, FR76, FR77) still specify capability in adjectives rather than testable consequence — story authors will invent the missing detail at story time.

The adversarial review materially shifts the picture. It reads the same artifact as a 1,260-line policy manifesto whose "minimum slice" is wider than most teams ship in two quarters, whose load-bearing safety controls (fail-closed, allowlisted commands, risk classification, cross-surface parity) are described as system properties rather than enforced invariants, and whose quantitative targets — 95% precision, 90% recall, 0 critical false-positives, RPO ≤ 15 min, RTO ≤ 4 hr, pilot adoption thresholds — anchor to a "tenant policy" / "evaluation dataset" / "operating baseline" that does not yet exist. Six Critical findings name a single underlying pattern: the named team (one frontend engineer, one CLI/MCP engineer, one security engineer) cannot ship the named release, and the PRD repeatedly defers unresolved decisions to artifacts it does not produce. The grade is set **Poor** because critical-severity findings are present; the rubric's positive verdict on spec craft and the adversarial verdict on buildability are both real, and both must be reconciled before architecture starts.

## Dimension verdicts

- Decision-readiness — adequate
- Substance over theater — adequate
- Strategic coherence — strong
- Done-ness clarity — thin
- Scope honesty — strong
- Downstream usability — adequate
- Shape fit — strong

## Findings by severity

### Critical (6)

**[Adversarial]** — C1. "Minimum Release Slice" is wider than the rest of the PRD admits — the MVP is not an MVP (§ lines 213–229; 865–893)
Nine workstreams expanded into 25 must-haves including tenant isolation across nine actor types, Keycloak, Aspire, dependency failure handling for seven dependency classes, security tests across nine actor types. The PRD admits "broad for a single MVP" then refuses to cut. Will either slip or quietly drop fail-closed under deadline.
Fix: sequence the slice into three or more increments — Increment 1: deterministic association + UI review + audit lookup + UI only.

**[Adversarial]** — C2. Precision/recall targets have no provenance and no dataset behind them (§ line 141; A9 line 1159)
"95% precision / 90% recall / 0 critical false-positives" against an evaluation dataset that A9 admits doesn't exist yet. Circular acceptance criterion.
Fix: commit named owner, sampling protocol, target size, refresh cadence — or delete the numbers from Success Criteria.

**[Adversarial]** — C3. T_high and T_low are fictional knobs with no operating range (§ line 120; FR9 line 1018)
Score function unspecified, score domain unspecified, calibration protocol unspecified, no guardrail on bad-default deployments or threshold changes. Decision log "clarification" is "tenant policy" — the punt, not the resolution.
Fix: specify score domain, safe-default initial values, calibration protocol against evaluation dataset, guardrail on threshold changes.

**[Adversarial]** — C4. "Fail closed" is used 20+ times but has multiple silent escape hatches (§ examples lines 116, 154, 472, 476, 595, 597, 716, 768, 771, 1109, 1184)
NFR15 lets non-security-sensitive transitions silently mutate state when audit is down. NFR22 + line 771 collapse approval routing during AI outage. Line 476 lets non-risky actions skip audit; "risky" is behaviorally classified, so a wrongly-classified action skips audit. Token, not contract.
Fix: enumerate every code path that can write durable state; define "fail closed" at each as an invariant, not a behavioral test.

**[Adversarial]** — C5. Cross-surface parity is enforced by aspiration, not by an invariant (§ lines 161–167; FR81–FR86; lines 723–728)
"Contract tests" + "automated parity tests" enforce parity. Parity is a quality the system has, not an invariant the architecture forbids violating. Drifts the first time a surface gains a feature another lacks.
Fix: require a single shared command pipeline at the architectural layer. Parity becomes structural, not test-outcome.

**[Adversarial]** — C6. "Single release" with all eight journeys is unbuildable on the named team (§ line 847; lines 851–863)
Singular frontend, singular CLI/MCP, singular security engineer to deliver 8 journeys, 27 commands, 14 queries, 9 lifecycle states, 3 surfaces with parity, WCAG 2.2 AA, RPO ≤ 15min / RTO ≤ 4hr, evaluation datasets, pilot. No team-load reconciliation. 6–12 person-year backlog packaged as one release.
Fix: reconcile team to scope or scope to team. Grow the team or shrink the release.

### High (12)

**[Done-ness clarity]** — Soft outcomes on user-facing FRs at the value moment (§ FR23, FR26, FR27, FR42, FR67, FR76, FR77 — lines 1038, 1041–1042, 1074, 1108, 1117–1118)
FRs specify capability but not testable consequence. Story authors will invent field lists and visual treatments at story time.
Fix: add explicit acceptance bullets under each FR, or extend the Functional Acceptance Guidance matrix to include FR21–FR28.

**[Adversarial]** — H1. "Magic constants" appear with no derivation (§ lines 176–180, 1172, 1196–1199, 1221, 1240–1241)
Pilot thresholds, cache staleness, p95 latencies, page sizes, RPO/RTO, projection rebuild — no derivation, no baseline measurement, no link to user research.
Fix: cite the baseline / research / throughput model for each, or label as starter values to be calibrated in pilot.

**[Adversarial]** — H2. "Tenant policy" is the universal escape hatch (§ A4–A6 lines 1154–1156; NFR9, NFR23; many others)
No master list of policy knobs and no schema. Admins configure an undocumented surface; security can't validate safe states; tests can't enumerate combinations.
Fix: author a Tenant Policy Schema as a first-class artifact with allowed values and safe defaults, under change control.

**[Adversarial]** — H3. Risk classification is a system capability without naming the classifier (§ lines 1056–1063; FR39 line 1071)
Heuristic? Tags? LLM? Static allowlist? The entire approval gate depends on this classifier being correct.
Fix: name the classifier mechanism, its error rate as a first-class risk, the misclassification fallback, audit chain when classification disagrees with reviewer action.

**[Adversarial]** — H4. "Allowlisted commands" is the load-bearing security control with no allowlist (§ lines 199, 205, 211, 226, 502, 883; A8 line 1158)
27-command catalog contains the ones an AI would invoke — either the allowlist is the whole catalog (decorative) or a subset (unspecified).
Fix: enumerate the MVP allowlist explicitly as a versioned artifact under change control.

**[Adversarial]** — H5. The "evaluation dataset" is load-bearing and nobody owns it (§ A9 line 1159; lines 141, 170–172, 1139)
Shared ownership ("QA / Product") = nobody owns it. No cardinality, label taxonomy, source provenance, redaction, refresh cadence, adversarial-example protocol.
Fix: single named owner; sampling frame, target size, label taxonomy, refresh cadence, adversarial-example protocol before pilot.

**[Adversarial]** — H6. "Audit completeness" is invoked but never defined (§ lines 159, 162, 466, 795; NFR50 line 1231)
Field-presence ≠ completeness. NFR50's 100% target is a tautology on the test corpus.
Fix: define audit completeness as a production observable (e.g., "fraction of state-mutating operations whose audit chain reconstructs the operation end-to-end").

**[Adversarial]** — H7. Idempotency promised across heterogeneous operations with no key contract (§ NFR13 line 1182; FR90 line 1137; line 698)
"Equivalent inputs" does enormous work. Key composition, replay window, equivalence rule, conflict response unspecified per operation class.
Fix: define per operation class; put the resulting contract in the addendum.

**[Adversarial]** — H8. Approval fatigue acknowledged but mitigation is hand-waved (§ line 547; NFR46 line 1224)
Prioritization heuristic, grouping criteria, suppression policy unspecified. First time a user has 40 approvals queued, they rubber-stamp; governance promise dies.
Fix: design the queue/grouping flow as a first-class FR with concrete grouping criteria and a per-user notification rate ceiling.

**[Adversarial]** — H9. Rejected/Failed are claimed terminal "unless reprocessed" — that is not terminal (§ lines 380–392)
PRD does both at once: terminal and non-terminal.
Fix: pick one. Recommended: terminal states are terminal; reprocess creates a new workflow instance with a new ID and an audit link to the predecessor.

**[Adversarial]** — H10. WCAG 2.2 AA + screen-reader + keyboard review on one frontend engineer (§ NFR60 line 1247; team list line 847)
Multi-month accessibility effort on a complex governance UI vs. one frontend engineer. Either the bar is theater or the team is wrong; both cannot be true.
Fix: resolve via C6. Commit external accessibility consultancy, or narrow NFR60 to increment-1 surfaces with explicit deferrals.

**[Adversarial]** — H11. "No admin/debug bypass" + required tenant-admin operational dashboards is unworkable (§ line 694; FR53, FR67–FR75)
Admins must operate eight queues / dashboards without bypassing authorization. Creates a complex admin permission model the PRD does not describe.
Fix: design the tenant-admin permission model explicitly as its own FR group — what admins can see vs. operate on, with audit obligations.

### Medium (17)

**[Decision-readiness]** — No live tensions surfaced (§ Executive Summary → Scope)
Zero `[NOTE FOR PM]` callouts and zero Open Questions across the entire document.
Fix: add 2–3 `[NOTE FOR PM]` callouts at real tensions (line 211 parity vs. velocity, line 1073 approval-required vs. fatigue, line 229 vertical slice sequencing).

**[Substance over theater]** — Adjective-only NFRs at user-trust hot spots (§ NFR40, NFR42, NFR44, NFR46, NFR48 — lines 1218, 1220, 1222, 1224, 1226)
"Enough next-action guidance," "preserve user trust," "runbook-ready," "preventing approval fatigue," "evidence freshness indicators must exist" — adjective-only.
Fix: attach at least one observable threshold to each.

**[Done-ness clarity]** — Task-intent FRs lack a data contract (§ FR35–FR38, lines 1067–1070)
Record fields, detection mechanism, recall/precision threshold unspecified.
Fix: add 1–2 lines to FR35 specifying minimum record fields and reference NFR68 for the precision/recall bar.

**[Done-ness clarity]** — NFR60 "core UI review workflows" unenumerated (§ line 1247)
"Core" is not enumerated; accessibility testing over- or under-covers.
Fix: enumerate the in-scope screens.

**[Downstream usability]** — Glossary missing load-bearing terms (§ Glossary, lines 953–968)
"Policy snapshot," "Operating baseline" / "Tenant or deployment profile," "Idempotency key," and the disambiguation of "Scoped AI context" / "AI context package" vs "Context package" missing.
Fix: add these terms; clarify whether "Scoped AI context" and "Context package" are synonymous.

**[Adversarial]** — M1. Source-context date pin is a future-fragility hazard (§ lines 88–90)
Nine sibling submodules makes re-check effectively guaranteed by architecture-start. "Materially" undefined; nobody triggers the re-check.
Fix: define "material change," name the trigger role, describe how re-check feeds back.

**[Adversarial]** — M2. ChatBot owns derived state nobody else owns — but the framing minimizes the surface (§ lines 64–65; 428–435; 580–589)
Association decisions, candidate rankings, evidence snapshots, AI action proposals, approval records, projections, policy snapshots, lifecycle — all durable, security-sensitive.
Fix: replace the "doesn't own records" framing with an explicit Data Governance Surface section listing ChatBot's first-class durable records.

**[Adversarial]** — M3. Capability described at the noun level (§ FR21–FR34, FR55–FR63, FR67–FR80)
FR22 has 7 first-class concerns, FR55 has 8 event families, FR67 has 8 dashboards, FR74 has 13 capabilities in one bullet.
Fix: decompose before story creation. Extend the Functional Acceptance Guidance matrix to all FRs above a noun-density threshold.

**[Adversarial]** — M4. Cross-tenant cache pollution mentioned once and not addressed (§ line 593)
Vector index artifacts and embedding stores have well-known multi-tenancy traps. PRD names the concern; addresses it nowhere.
Fix: add an FR/NFR pair covering per-tenant isolation in vector / embedding / cache stores, with a test obligation.

**[Adversarial]** — M5. "Sender authority" is invoked repeatedly without a definition (§ lines 486, 738, 1083, 1085; FR48)
Five-class taxonomy mapped to Microsoft Graph permission models; who maps actions to authority unspecified.
Fix: define the mapping rule and the conflict case (user has M365 send-on-behalf but no ChatBot grant).

**[Adversarial]** — M6. "Visible failure states" do not define a UX contract (§ FR66, FR69, FR76–FR79; NFR17, NFR39)
"Visible" — queue row? toast? email? combination? Notification routing is configurable; tenants will turn it off.
Fix: define visible-state contract as minimum: in-queue + audit event + configurable notification.

**[Adversarial]** — M7. Stable identifiers across bounded contexts, but ChatBot doesn't mint IDs (§ lines 492–493, 588)
What happens when a sibling context renames, splits, merges, or deprecates an ID? Audit records become irreproducible.
Fix: add an ID-evolution contract — required transitions, audit-record migration rules.

**[Adversarial]** — M8. Replay/simulation promised at the same surface as production (§ FR95 line 1142; NFR69 line 1259)
Mechanism preventing replay from sending external mail or mutating state unnamed.
Fix: specify the architectural enforcement (separate test tenant, outbound-adapter interception, command flag) and the verification that distinguishes replay events in audit.

**[Adversarial]** — M9. Correction-path invalidates AI context — cost hidden (§ Journey 4 lines 302–310; FR91 line 1138)
Every derived index, cached prompt context, search index, vector embedding that touched the corrected association must be invalidated. Multi-minute to multi-hour background job per correction; treated as a UI action.
Fix: add an NFR bounding correction-propagation latency and an FR describing the user-facing state during reindex.

**[Adversarial]** — M10. External-party participation hides a phishing/spoofing problem (§ Journey 3 lines 296–300)
"Sender spoofing/mismatch" in one phrase. Real corporate mail has forwarded threads, mailing-list rewrites, on-behalf-of headers, MTA-injected external-sender warnings.
Fix: add an FR group for inbound-message-authenticity checks (DMARC/DKIM/SPF, header inspection, on-behalf-of disambiguation, external-sender posture).

**[Adversarial]** — M11. "MVP must define" appears as a deflection (§ lines 462, 466, 484, 487, 638, 783, 787, 805)
The PRD deferring to itself.
Fix: resolve each "MVP must define" inline, or relocate it to an open-question with an owner and resolution date.

**[Adversarial]** — M12. Eight journeys imply eight UI surfaces with no UI inventory (§ Journeys 1–8 lines 262–356)
Project conversation, ambiguous-association resolution, correction, admin config, CLI, compliance investigation, AI action review — no UI inventory anywhere.
Fix: hand off the inventory to `bmad-ux`; do not let it lurk in journey prose.

### Low (17)

**[Decision-readiness]** — Approval fatigue named but not reconciled with FR41 default
Fix: tighten the FR41/FR52 escape hatch or add a `[NOTE FOR PM]` admitting MVP errs toward fatigue and will tune post-pilot.

**[Substance over theater]** — Innovation section duplicates "What Makes This Special" (§ lines 510–516; 66–72)
Fix: trim or earn the duplication by surfacing a different pattern.

**[Strategic coherence]** — Pilot thresholds are not baselined (§ lines 174–180)
Fix: add an `[ASSUMPTION]` (or extend A9) for a 2–4 week pilot baseline measurement period with a named owner.

**[Strategic coherence]** — No named owner for measuring success metrics (§ Success Criteria, lines 130–180)
Fix: name the responsible role in Validation outcomes.

**[Done-ness clarity]** — FR94 names operational metrics without publication shape (§ line 1141)
Fix: point at NFR28 / NFR37 or specify the exposure surface (Prometheus / OpenTelemetry / dashboard).

**[Scope honesty]** — No `[NOTE FOR PM]` callouts anywhere
Fix: see Decision-readiness finding.

**[Downstream usability]** — Lifecycle state list reorders relative to canonical (§ lines 700–712 vs. 380–391)
Fix: reorder to match the canonical sequence.

**[Downstream usability]** — UJs lack a "key acceptance hooks" extract per journey (§ UJ1–UJ8, lines 262–368)
Fix: optional — add a 3–5 bullet extract at the end of each UJ.

**[Downstream usability]** — FR/NFR cross-references not machine-verified (§ Traceability, lines 972–983)
Fix: machine-validate before architecture starts.

**[Adversarial]** — L1. "Hexalith.ChatBot" product name vs. email-only MVP (§ Title)
Fix: rename or make the chat surface a first-class MVP concern.

**[Adversarial]** — L2. Aspire named as a dependency but never described (§ lines 88, 753, 847, 889)
Fix: one-line definition in the glossary.

**[Adversarial]** — L3. FrontComposer in classification context but not in integration list (§ line 88)
Fix: add to integration list or remove from classification context.

**[Adversarial]** — L4. Audit "tamper-evident" asserted without mechanism (§ NFR49 line 1230)
Fix: name a class of mechanism (hash chain, append-only WORM, signed envelopes).

**[Adversarial]** — L5. Glossary skips load-bearing terms (§ Glossary lines 953–969)
"Fail closed," "low-risk," "approval-required," "policy snapshot," "operating baseline," "MVP parity set," "evaluation dataset" — none glossed.
Fix: add. Cross-references the rubric's Downstream-usability finding.

**[Adversarial]** — L6. ChatBot ownership boundaries listed three different ways (§ lines 64–65, 428–435, 580–589)
Fix: pick one canonical list and reuse it.

**[Adversarial]** — L7. "MCP" never expanded (§ throughout)
Fix: one-line glossary entry.

**[Adversarial]** — L8. "Differentiating moment" claimed against an explicit disclaimer of market validation (§ lines 70–72; 520)
Fix: either soften the claim or cite the validation source.

**[Adversarial]** — L9. Innovation section restates requirements at higher abstraction (§ lines 508–552)
Fix: delete, or use to assert something genuinely new.

## Mechanical notes

- FR IDs contiguous FR1–FR96; NFR IDs contiguous NFR1–NFR70 (spot-check, not machine-verified).
- No inline `[ASSUMPTION]` tags; assumptions centralized as A1–A9 with owners and revisit conditions. A1, A2, A3, A5, A6, A8, A9 are inline-cited; A4 and A7 are platform-baseline and not inline-cited (acceptable).
- Zero `[NOTE FOR PM]` callouts and zero Open Questions — flagged across multiple dimensions as a tension-smoothing pattern.
- UJ protagonists all named (Amira, Marc, Elena, Priya, Nora, Leo, Sofia, plus System Journey).
- Lifecycle terminology stable across §"Shared Workflow Contract" (lines 380–391) and §"Association Lifecycle and States" (lines 700–712); the second list reorders the same states — cosmetic.
- Decision log is sparse — captures normalization and a thin set of major decisions; deeper rationale lives in the PRD body, not the log.
- No `addendum.md` exists. Several findings (idempotency-key contract, policy schema, allowlist enumeration, classifier mechanism) belong in an addendum that does not yet exist.

## Reviewer files

- `review-rubric.md`
- `review-adversarial-general.md`
