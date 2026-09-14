# Hexalith / Fluent UI V5 Conformance Review

## Overall assessment

**Partially conformant; no critical finding.** The UX package makes the required Microsoft Blazor Fluent UI v5 → Hexalith.FrontComposer inheritance binding, rejects local theme redefinition, requires the FrontComposer route/page shell, and documents the sole legitimate accordion carve-out. Two high-impact documentation defects remain: some canonical component bases do not exist in the pinned Fluent UI v5 package, and accordion ownership is not specified at the surface level closely enough to guarantee one accordion with one item per sibling titled section.

This is a documentation conformance review, not proof that the running application conforms. Statements about live-route, asset, browser, or computed-style acceptance are requirements until supported by the runtime evidence hierarchy in `implementation-conformance-addendum-2026-07-17.md` §Evidence hierarchy (lines 72–77).

## Findings

### High — Canonical base mappings name unavailable or non-canonical components

`DESIGN.md` maps Correction progress and Retention and export request to `FluentProgress` (`DESIGN.md`, frontmatter `components`, lines 71–73 and 95–97), and Queue filter bar to `FluentToolbar` plus unspecified “Fluent input controls” (lines 116–118). Against the repository's pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1` (`references/Hexalith.Builds/Props/Directory.Packages.props`, lines 226–227), API inspection finds `FluentProgressBar`, not `FluentProgress`, and no `FluentToolbar` type. `FrontComposer sheet` is likewise not a concrete component identity (`DESIGN.md`, lines 113–115).

**Impact:** A consumer treating the frontmatter as the canonical implementation map, as required by `DESIGN.md` §Components (lines 167–169) and `EXPERIENCE.md` §Component Patterns (lines 77–81), can select types that do not compile or invent a local primitive.

**Required correction:** Replace `FluentProgress` with the exact pinned v5 component (`FluentProgressBar`). Resolve Queue filter bar and Review dialog/sheet to named, available Fluent/FrontComposer components, or explicitly document the no-equivalent fallback, semantics, and allowed custom layout. Replace category phrases such as “Fluent input controls” with the concrete controls required by the pattern.

### High — The single-accordion rule lacks implementable surface/item ownership

The Hexalith rule requires one `FluentAccordion` and one `FluentAccordionItem` per sibling titled section (`references/Hexalith.AI.Tools/hexalith-ux-instructions.md` §Page sections, lines 43–51). The spines repeat the one-accordion/default-expanded principle (`DESIGN.md` §Layout & Spacing, lines 149–155; `EXPERIENCE.md` §Information Architecture, lines 55–59), but they do not name the owning accordion or its items for each surface. Instead, individual section components such as Source evidence, AI summary, Why this project, AI proposal panel, and Audit timeline each map directly to `FluentAccordion` (`DESIGN.md`, lines 35–43, 62–64, and 89–91). No canonical mapping names `FluentAccordionItem`.

**Impact:** A literal implementation can create multiple sibling or nested accordions while still appearing to follow each component mapping, contrary to the required single surface-level accordion. Because the package is intentionally spine-only and calls its IA/component tables the implementation reference (`EXPERIENCE.md` §Foundation, line 36), downstream stories cannot recover the intended grouping from a mock.

**Required correction:** Add a surface-composition table for every page, dialog, and detail panel that can contain two or more sibling titled sections. For each, identify the single owning `FluentAccordion`, every `FluentAccordionItem`, the primary/default-expanded item, and content outside the accordion. Change section-level bases to `FluentAccordionItem` where they participate in that owner. Preserve Association Review as the only carve-out.

### Medium — The implementation reference points to a stale Fluent v5 contingency baseline

`DESIGN.md` calls `references/Hexalith.FrontComposer/docs/fluent-ui-v5-contingency.md` the implementation reference (`DESIGN.md` §Brand & Style, line 125). That file states the current pin is `5.0.0-rc.2-26098.1` and repeatedly instructs restoration to RC2 (`fluent-ui-v5-contingency.md`, lines 5, 42, 97–100, and 209–212), while the shared package catalog pins RC5 (`references/Hexalith.Builds/Props/Directory.Packages.props`, lines 226–227).

**Impact:** The named implementation reference can send a developer to the wrong API surface or rollback version; this is especially material while the canonical component map already contains version-invalid names.

**Required correction:** Refresh the contingency document to the current shared pin and APIs, or replace the DESIGN.md reference with a current, version-bound FrontComposer/Fluent v5 component reference. Keep central package configuration authoritative.

### Medium — Reuse-over-hand-rolling is not fully restated in the UX implementation contract

The governing instruction excludes raw CSS, HTML, JavaScript, and third-party components whenever an equivalent Fluent/FrontComposer component exists (`references/Hexalith.AI.Tools/hexalith-ux-instructions.md` §Reuse over hand-rolling, lines 10–17). The spines explicitly cover raw CSS and inherited styling (`DESIGN.md` §Brand & Style, lines 125–127; §Layout & Spacing, lines 149–151), while the binding addendum explicitly prohibits only four raw interactive HTML elements and theme/control recreation (`implementation-conformance-addendum-2026-07-17.md` §Visual and component inheritance, lines 27–30). JavaScript widgets and third-party component substitution are not named.

**Impact:** The organization-wide baseline still governs, but the local, self-described binding implementation source is incomplete and its conformance guards can pass an equivalent third-party or JavaScript widget unless another rule catches it.

**Required correction:** Restate the full equivalence rule in the binding addendum and require the Fluent-control guard to reject equivalent third-party/JavaScript controls, with explicit reviewed exceptions only when no FrontComposer or Fluent v5 equivalent exists.

### Low — One indirect source registry is not reproducible as written

All six sources/supplements declared directly by `DESIGN.md` and `EXPERIENCE.md` resolve (`DESIGN.md`, lines 7–15; `EXPERIENCE.md`, lines 6–14). However, the declared Product Brief source lists repository inputs without the actual `references/` prefix, and two listed Party/Tenant brief paths are absent from the checkout (`product-brief-Hexalith.ChatBot.md`, frontmatter `inputs`, lines 6–14).

**Impact:** This does not change the present Fluent/FrontComposer decision, but it prevents a clean provenance replay of one declared upstream source.

**Required correction:** Normalize the input paths to current repository-relative locations and mark unavailable historical inputs as archived with a resolvable immutable reference or as unavailable evidence.

## Confirmed conformance

- Both spines bind the correct visual inheritance chain and the single FrontComposer shell with `FcPageLayout` and `FcPageHeader` (`DESIGN.md`, lines 123–127 and 149–151; `EXPERIENCE.md`, lines 19–23).
- The inherited-token discipline is explicit: no local palette, type ramp, radius, or spacing scale is declared; color, typography, shape, spacing, elevation, density, and focus remain owned by FrontComposer/Fluent (`DESIGN.md`, lines 125–139 and 157–165).
- No hard-coded theme colors or legacy Fluent v4/FAST token names appear in the spines or supplements. The allowed `.fluent-layout { display: grid; }` rule is layout-only and paired with live computed-style acceptance (`DESIGN.md`, line 151; `implementation-conformance-addendum-2026-07-17.md`, lines 27–30).
- The binding supplement requires Fluent/FrontComposer reuse, prohibits raw interactive controls, prohibits module-owned page chrome, and keeps Fluent-control and FrontComposer-layout guards separate and non-vacuous (`implementation-conformance-addendum-2026-07-17.md`, lines 27–38).
- The accordion rule covers page-like surfaces and expands primary content by default. Association Review is a legitimate single-workflow carve-out, keeps candidates and safe actions visible together, and places complementary evidence/source metadata in an accordion; no other carve-out is documented (`DESIGN.md`, line 155; `EXPERIENCE.md`, line 57; `implementation-conformance-addendum-2026-07-17.md`, lines 37–44).
- Component pattern names match one-to-one across DESIGN and EXPERIENCE, and EXPERIENCE uses `{components.*}` token references rather than duplicating visual values (`DESIGN.md`, lines 167–206; `EXPERIENCE.md`, lines 77–116).
- Direct source, supplemental-source, source-proposal, architecture, streaming-ADR, and FrontComposer-reference targets inspected for this lens exist in the checkout. The remaining provenance defect is limited to the Product Brief's indirect input registry noted above.

## Finding counts

| Severity | Count |
|---|---:|
| Critical | 0 |
| High | 2 |
| Medium | 2 |
| Low | 1 |
| **Total** | **5** |
