---
baseline_commit: 11b0156
---

# Story 1.10: Architecture dependency fitness tests

Status: done

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-05-31. -->

## Story

As an architecture owner,
I want assembly-level (NetArchTest) dependency-direction and adapter-boundary fitness tests that fail and name the forbidden edge,
so that the FR81a "adapters cannot replicate a pipeline stage" invariant and the Contracts ← Client ← Server dependency direction are mechanically enforced at compile-output level — not just by source-text scans or code review — and a future surface that bypasses the spine breaks the build.

## Acceptance Criteria

1. Given the platform convention that pattern enforcement is mechanical (NetArchTest), when the maintained NetArchTest package is added under central package management and the `Hexalith.ChatBot.Architecture.Tests` project loads the **compiled** ChatBot assemblies, then a dedicated assembly-level dependency-fitness test layer runs inside the existing Architecture.Tests suite — the full solution still builds under warnings-as-errors with **no inline package versions** (the version lands in `Directory.Packages.props`, the project carries only a bare `<PackageReference>`), and the new tests run via the in-process xUnit v3 binary (VSTest `dotnet test` is sandbox-blocked). [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines; _bmad-output/planning-artifacts/architecture.md#Tech Stack & Decisions; _bmad-output/planning-artifacts/epics.md#Story 1.1; Directory.Packages.props]

2. Given the inherited module dependency-direction rule (Contracts ← Client ← Server; adapters → Client only), when NetArchTest analyses the loaded assemblies' IL dependencies, then `Hexalith.ChatBot.Contracts` has no dependency on any other ChatBot project assembly, `Hexalith.ChatBot.Client` depends on `Contracts` only (among ChatBot assemblies), `Hexalith.ChatBot.Server` never depends on `Aspire`/`AppHost`/`UI`/any adapter assembly, and every adapter assembly (`*.UI`, and any future `*.Cli`/`*.Mcp`/`*.Workers`) **never depends on `Hexalith.ChatBot.Server`** — a violation fails the test and the assertion message names the offending type(s) and edge. [Source: _bmad-output/planning-artifacts/architecture.md#Structure Patterns; _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries; _bmad-output/planning-artifacts/epics.md#Story 1.10]

3. Given the FR81a adapter-boundary invariant (adapters MUST NOT replicate any pipeline stage — they cannot authorize, classify risk, write audit records, or run idempotency), when the adapter-boundary fitness test runs, then no type in any adapter assembly (`*.UI`/`*.Cli`/`*.Mcp`/`*.Workers`) has an IL-level dependency on the Server governance seams (`IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore`) or on any `…Server.Gateway`/`…Server.Gateway.Stages` namespace — verified at the compiled-dependency level, **complementing (not replacing)** the existing source-token guards in `ScaffoldArchitectureTests`. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a; _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared Command Pipeline; _bmad-output/planning-artifacts/epics.md#Story 1.10]

4. Given the rule that aggregates/projections/domain processors live in `Hexalith.ChatBot.Server` only (the only scanned assembly), when the fitness test runs, then no type **outside** the Server assembly implements `IDomainProcessor`, derives from `EventStoreAggregate<>`, or resides in an `…Operations`/`…Projections` namespace — a violation fails the test and identifies the forbidden type. [Source: _bmad-output/planning-artifacts/architecture.md#Naming Patterns; _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines; Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs]

5. Given FR86 reframes these as invariant-verification tests (a failure is an invariant violation, not a tolerance threshold), when an adapter attempts a forbidden edge, then the fitness test fails **and the failure message identifies the forbidden edge** (the failing type name + the violated rule via NetArchTest's `FailingTypeNames`) — proven by a committed, non-destructive meta-test that feeds the rule machinery a known-non-conforming input and asserts `result.IsSuccessful == false` with the offending type surfaced; and the new layer is **forward-safe**: adapter assemblies added later (`*.Cli`/`*.Mcp`/`*.Workers`) are auto-discovered without editing the test. All pre-existing Architecture/Server/Conformance/Contracts/Client/UI/Integration tests stay green. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/.decision-log.md (FR86 reframed as verification); _bmad-output/planning-artifacts/epics.md#Story 1.10; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

## Tasks / Subtasks

- [x] Add the maintained NetArchTest dependency under central package management (AC: 1)
  - [x] Add `<PackageVersion Include="NetArchTest.eNhancedEdition" Version="1.3.7" />` to `Directory.Packages.props` (recommended new `Testing` group entry). **Prefer the maintained `NetArchTest.eNhancedEdition` fork** — the original `NetArchTest.Rules` (BenMorris) is unmaintained since 2021 and is .NET Standard 2.0 only; the eNhanced fork keeps the identical `NetArchTest.Rules` namespace/fluent API, fixes bugs, and tracks current .NET. At implementation time, **verify the chosen version actually parses net10.0 assemblies** (it depends on Mono.Cecil ≥ 0.11.6; load a ChatBot assembly and run one rule). If the fork is unacceptable, `NetArchTest.Rules 1.3.0` is the fallback — same API — but confirm Cecil parses net10.0 IL first. Record the chosen package + version in the Dev Agent Record. [Source: Directory.Packages.props; https://www.nuget.org/packages/NetArchTest.eNhancedEdition/1.3.7; https://github.com/NeVeSpl/NetArchTest.eNhancedEdition]
  - [x] Add a bare `<PackageReference Include="NetArchTest.eNhancedEdition" />` (NO inline `Version=`) to `tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj`. The `ProjectFilesShouldNotUseInlinePackageVersions` test already enforces this — do not regress it. [Source: tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs (ProjectFilesShouldNotUseInlinePackageVersions)]
- [x] Make the compiled ChatBot assemblies loadable by the test project (AC: 1, 2, 4)
  - [x] Add `ProjectReference`s from `Architecture.Tests` to the assemblies NetArchTest must inspect — at minimum `Contracts`, `Client`, `Server`, `UI`, plus `Hexalith.EventStore.Client` (for `IDomainProcessor`/`EventStoreAggregate<>` identification) via `$(HexalithEventStoreRoot)`. Today this csproj has **zero** project references (the existing tests work purely by reading repo files), so this is a real change. A `ProjectReference` guarantees the target DLL is built and copied to the test output directory so NetArchTest can load it. [Source: tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj; tests/Hexalith.ChatBot.Server.Tests/Hexalith.ChatBot.Server.Tests.csproj (reference pattern)]
  - [x] **Do NOT add `InternalsVisibleTo` for `Architecture.Tests`** and do NOT weaken any `internal` modifier. NetArchTest rules operate on namespace-name strings and assembly references, not on type accessibility — the internal governance seams stay internal. [Source: src/Hexalith.ChatBot.Server/Hexalith.ChatBot.Server.csproj (InternalsVisibleTo lists only Server.Tests + IntegrationTests); src/Hexalith.ChatBot.Server/Gateway/Stages/IRiskClassifier.cs]
  - [x] Anchor each assembly via a **stable public type**, never `typeof(Program)` (the Blazor/web `Program` is an implicit/internal top-level class): e.g. `typeof(RecordGovernedNote).Assembly` (Contracts), `typeof(ChatBotClient).Assembly` (Client), `typeof(GovernedOperationAggregate).Assembly` (Server), a public UI type's `.Assembly` (UI). [Source: src/Hexalith.ChatBot.Contracts/Commands/RecordGovernedNote.cs; src/Hexalith.ChatBot.Client/ChatBotClient.cs; src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- [x] Add the IL-level dependency-direction fitness tests (AC: 2)
  - [x] New test class (e.g. `DependencyDirectionFitnessTests` in `tests/Hexalith.ChatBot.Architecture.Tests/`) using `NetArchTest.Rules` fluent API. Assert the **negative/forbidden** edges at IL level: `Types.InAssembly(serverAsm).Should().NotHaveDependencyOnAny("Hexalith.ChatBot.UI", "Hexalith.ChatBot.Aspire", "Hexalith.ChatBot.AppHost")`; each adapter assembly `.Should().NotHaveDependencyOn("Hexalith.ChatBot.Server")`; `Contracts` `.Should().NotHaveDependencyOnAny("Hexalith.ChatBot.Client","Hexalith.ChatBot.Server")`; `Client` `.Should().NotHaveDependencyOn("Hexalith.ChatBot.Server")`. Assert each with `var r = …GetResult(); r.IsSuccessful.ShouldBeTrue(BuildMessage(r))` where `BuildMessage` includes `r.FailingTypeNames`. [Source: _bmad-output/planning-artifacts/architecture.md#Structure Patterns; _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
  - [x] Keep the **positive exact-edge** direction check where it already lives — the source-token/XML `ProjectReferencesShouldFollowContractsClientServerDirection` and `ChatBotUiAdapterMustDependOnlyOnClientFacadeAndNeverServerInternals` in `ScaffoldArchitectureTests` already assert the precise allowed `ProjectReference` set. NetArchTest's `OnlyHaveDependenciesOn` is noisy against BCL/third-party, so use NetArchTest for the forbidden-edge (IL) layer and leave the exact-allowed-set assertion to the existing XML test. Do not duplicate or weaken it. [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs (ProjectReferencesShouldFollowContractsClientServerDirection, ChatBotUiAdapterMustDependOnlyOnClientFacadeAndNeverServerInternals)]
- [x] Add the IL-level adapter-boundary fitness tests (AC: 3)
  - [x] Assert no adapter-assembly type has an IL dependency on a `…Server.Gateway` / `…Server.Gateway.Stages` namespace. Because `IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore` are `internal` to `.Server`, an adapter literally cannot compile-reference them — so the **namespace-dependency** assertion is the meaningful IL check (it also catches transitive/indirect leaks the source-token scan misses). Keep the existing source-token guards (`GatewayStageSeamsShouldRemainInternalToServer`, `SurfaceAdaptersShouldNotWriteAuditRecordsDirectly`, `SurfaceAdaptersMustNotReferenceGatewayIdempotencyStages`) — they cover not-yet-compiled future projects by file scan. [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
  - [x] Discover adapter assemblies **dynamically** so the rule is forward-safe: scan the test output directory (`AppContext.BaseDirectory`) for `Hexalith.ChatBot.{UI,Cli,Mcp,Workers}.dll`, `Assembly.LoadFrom` each present one, and run the boundary rules over all of them. Today only `UI` exists; when `Cli`/`Mcp`/`Workers` are added they are auto-covered with no test edit (the ProjectReference for not-yet-existing projects is added when those stories land). [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure (Cli/Mcp/Workers are [M1]/[M0] future)]
- [x] Add the aggregate/projection placement fitness test (AC: 4)
  - [x] Assert that across all loaded ChatBot assemblies **except Server**, no type `ImplementsInterface(typeof(IDomainProcessor))`, derives from `EventStoreAggregate<>`, or `ResideInNamespace` matching `…Operations`/`…Projections`. `IDomainProcessor` is `Hexalith.EventStore.Client.Handlers.IDomainProcessor`; `EventStoreAggregate<TState>` is `Hexalith.EventStore.Client.Aggregates` (public). Deriving from an open generic is awkward in the fluent API — prefer the `IDomainProcessor`-implements check plus the namespace check; if needed, fall back to a reflection `IsAssignableTo`/base-type walk over `asm.GetTypes()`. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Handlers/IDomainProcessor.cs; Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs; src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs; src/Hexalith.ChatBot.Server/Projections/]
- [x] Prove the failure path identifies the forbidden edge (AC: 5)
  - [x] Add a committed, non-destructive **meta-test** (e.g. `FitnessRuleFailureSurfacesTheForbiddenEdge`) that applies one rule to a deliberately-non-conforming input and asserts detection — e.g. `Types.InAssembly(serverAsm).That().ResideInNamespace("…Operations").Should().NotHaveDependencyOn("Hexalith.EventStore")` (Server's aggregate DOES depend on EventStore) → assert `result.IsSuccessful == false` and `result.FailingTypeNames` contains `GovernedOperationAggregate`. This proves the machinery surfaces the offending type **without committing a real architecture violation**. Do NOT introduce an actual violating type or a skipped/quarantined assembly. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.10 (test fails and identifies the forbidden edge); _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/.decision-log.md (FR86 = verification, failure = invariant violation)]
- [x] Verify locally (AC: all)
  - [x] `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false`
  - [x] `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` — warnings-as-errors, no inline package versions; full solution 0 warnings / 0 errors.
  - [x] Run the affected test binaries directly (Architecture first; then a full sweep of the other ChatBot test projects to prove no regression). VSTest `dotnet test` is blocked in the sandbox (socket `Permission denied`, as in Stories 1.8/1.9) — build each test csproj and run the compiled xUnit v3 binary directly: `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests`. Record the exact commands + counts in Debug Log References. [Source: _bmad-output/implementation-artifacts/1-9-first-governed-command-end-to-end-with-surface-origin-attribution.md#Debug Log References]

## Dev Notes

### Implementation Intent

This is a **test-only, behavior-preserving** story. It adds the missing **assembly/IL-level (NetArchTest)** fitness layer the architecture mandates ("Pattern enforcement (mechanical, not review-by-eyeball): NetArchTest"). It must NOT change any `src/` runtime behavior. The only production-tree edit is `Directory.Packages.props` (a `PackageVersion` entry — configuration, not behavior). Everything else lands in `tests/Hexalith.ChatBot.Architecture.Tests/`. [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines]

**Why this story exists even though boundary tests already pass.** `tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs` (21 tests, all green) already enforces a lot of the same intent — but it does so by **reading source text and `.csproj`/`.slnx` XML** (regex over `*.cs` files, `XDocument` over project files). That is a real and useful guard, but it is **not** what the epic AC asks for: "**When NetArchTest runs** … dependency-direction edges hold … and the dependency fitness test **fails and identifies the forbidden edge**." NetArchTest analyses the **compiled IL** (via Mono.Cecil), which catches dependency edges a source-token scan cannot — transitive/indirect references, type usages that don't appear as a literal token, and assembly-reference leaks. The two layers are **complementary**; keep both. [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs; _bmad-output/planning-artifacts/epics.md#Story 1.10]

### The genuine added value vs. the existing file-scan tests (read carefully)

The four governance seams (`IRiskClassifier`/`IApprovalGate`/`IAuditWriter`/`IIdempotencyStore`) are already `internal` to `.Server` — so an adapter **cannot compile** a direct reference to them; the compiler is the first enforcer. NetArchTest's job is therefore NOT to re-prove the impossible direct reference, but to assert the **expressible-at-IL** invariants:

1. **Assembly dependency direction** — `UI`/future-`Cli`/`Mcp`/`Workers` IL carries **no `AssemblyReference` to `Hexalith.ChatBot.Server`** (and Contracts/Client stay low); a future adapter that pulls Server in transitively breaks here even if no Server token appears in its source.
2. **Namespace-dependency edges** — no adapter type depends on a `…Server.Gateway`/`…Server.Gateway.Stages` namespace.
3. **Type placement** — `IDomainProcessor` / `EventStoreAggregate<>` / `…Operations` / `…Projections` types exist **only** in the Server assembly.
4. **Failure names the edge** — NetArchTest's `PolicyResult`/`TestResult.FailingTypeNames` is what makes AC5 ("identifies the forbidden edge") real; thread it into the assertion message.

Do **not** rewrite or delete the existing file-scan tests to "replace them with NetArchTest" — they cover **not-yet-compiled future projects** (a `Cli`/`Mcp` source file can be scanned before that project is even in the build), and they assert source-only facts (no `DateTime.Now`, no legacy lifecycle literals, exact `ProjectReference` sets, submodule policy). Those stay. [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

### NetArchTest API specifics (so you don't fight the fluent API)

- Namespace is `NetArchTest.Rules` for **both** the original and the eNhanced fork — the fork is a near-drop-in (not 100% backwards compatible on a few edge methods; verify the exact method names against the installed version).
- Core shape:
  ```csharp
  TestResult result = Types.InAssembly(adapterAssembly)
      .Should()
      .NotHaveDependencyOn("Hexalith.ChatBot.Server")
      .GetResult();
  result.IsSuccessful.ShouldBeTrue(/* message including */ string.Join(", ", result.FailingTypeNames ?? []));
  ```
- `HaveDependencyOn`/`NotHaveDependencyOn` match by **namespace prefix string** — so they work against `internal` types too (you name the namespace, not the type). Use `NotHaveDependencyOnAny(...)` for multiple forbidden roots.
- `Types.InAssembly(...)` / `Types.InAssemblies(IEnumerable<Assembly>)` — pass the loaded adapter assemblies. `.That().ResideInNamespace("…")`, `.ImplementInterface(typeof(IDomainProcessor))` are the predicates for the placement rule.
- The eNhanced fork adds extras (Slices API, `AreImmutable`, etc.) — **not needed here**; keep the rule set minimal and legible.

### Loading the assemblies (the one real gotcha)

The test project must **reference** (ProjectReference) each assembly it inspects so the DLL is in `bin/.../net10.0/`. Anchor by a **public** type (`typeof(GovernedOperationAggregate).Assembly`), never `typeof(Program)` (implicit top-level `Program` is internal/compiler-generated and not reliably reachable). For the forward-safe adapter discovery, scan `AppContext.BaseDirectory` for `Hexalith.ChatBot.{UI,Cli,Mcp,Workers}.dll` and `Assembly.LoadFrom` each present file — that way the rule covers `UI` today and auto-covers `Cli`/`Mcp`/`Workers` when those projects are added (their ProjectReference is added in the story that creates them). [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs (RepositoryRoot uses AppContext.BaseDirectory)]

### Architecture Guardrails

- **No runtime/behavior change.** Do not touch `src/**/*.cs`. Do not change any `internal`→`public`, add `InternalsVisibleTo`, reorder gateway stages, or alter DI. The only `src`-tree edit allowed is `Directory.Packages.props`. [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines]
- **Central package management only.** New package version goes in `Directory.Packages.props`; the csproj gets a bare `<PackageReference>` with no `Version=`. `ProjectFilesShouldNotUseInlinePackageVersions` (existing) and `RootConfigurationShouldPinSdkTargetFrameworkAndCentralPackages` will fail if you regress this. [Source: Directory.Packages.props; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- **Complement, never regress, the 21 existing Architecture tests.** Add new test classes/methods; do not edit existing assertions except to add (not remove) coverage. If you rename `ScaffoldArchitectureTests` for clarity, keep every existing `[Fact]` intact. [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- **Metadata-only / no leakage** still applies to any assertion message you emit (use type names and rule names; never embed file contents, secrets, or paths beyond the relative type/namespace identifiers NetArchTest returns). [Source: _bmad-output/planning-artifacts/architecture.md#Format Patterns]
- **Submodules.** EventStore is a root-level submodule reached via `$(HexalithEventStoreRoot)`; use only root-declared submodules, never `--recursive`, never initialize nested submodules. [Source: CLAUDE.md#Git Submodules]
- **Reuse stable vocabularies / fixtures.** xUnit v3 (3.2.2) `[Fact]` + Shouldly (4.3.0); no new mocking/assertion library beyond NetArchTest. Mirror the existing test conventions in `ScaffoldArchitectureTests` (static facts, `RepositoryRoot()` helper). [Source: Directory.Packages.props; tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]

### Scope Boundary at M0 (read carefully)

- This is the **NetArchTest** fitness layer only (epic Story 1.10). It is **not** the differential-conformance harness (Story 1.11 — same-semantic-intent across UI/CLI/MCP shims → identical event sequence + state-store end-state) and **not** the cross-tenant isolation harness (Story 1.12 — nine-actor negative tests). Do not build runtime parity/isolation here. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.11; _bmad-output/planning-artifacts/epics.md#Story 1.12]
- `Cli`/`Mcp` adapter projects do **not exist yet** ([M1], Epic 5). The IL rules can only inspect assemblies that are built (today: `UI`). Make the adapter discovery dynamic so the future projects are auto-covered; the file-scan forward-guards in `ScaffoldArchitectureTests` already cover the not-yet-compiled case. Do not scaffold `Cli`/`Mcp` here. [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure; _bmad-output/planning-artifacts/epics.md#Epic 5]

### Previous Story Intelligence

- **Story 1.9** extended `ScaffoldArchitectureTests` with the UI-depends-only-on-Client rule, the seam-stays-internal rule, the single-dispatch-site rule, the spine-allowlist registration guard, and the `ProjectReferences…Direction` exact-edge assertion. These are the source-text/XML baseline this story layers IL-level NetArchTest checks **on top of** — read them first so you don't duplicate intent or contradict them. The Server now hosts the real `GovernedOperationAggregate` (Pattern A, `: EventStoreAggregate<GovernedOperationState>` → `IDomainProcessor`) and `GovernedOperation*` projections — these are the exact types the AC4 placement rule must confirm live **only** in `.Server`. [Source: _bmad-output/implementation-artifacts/1-9-first-governed-command-end-to-end-with-surface-origin-attribution.md#File List; src/Hexalith.ChatBot.Server/Operations/GovernedOperationAggregate.cs]
- **Sandbox note (Stories 1.8/1.9):** VSTest `dotnet test` fails with a socket `Permission denied`; run the compiled xUnit v3 in-process binary directly instead. Expect the same. NetArchTest runs in-process (Cecil reads the DLLs in the output dir), so the direct-binary approach works unchanged. [Source: _bmad-output/implementation-artifacts/1-9-...md#Debug Log References]
- **Verify Mono.Cecil parses net10.0 IL.** Both NetArchTest packages depend on Mono.Cecil (≥ 0.11.6 available). Cecil is generally forward-compatible, but newer runtime metadata can occasionally trip an older Cecil. The first thing to run after wiring the package is a single trivial rule against a ChatBot assembly to confirm the assembly loads and `GetResult()` returns without a Cecil exception — before writing the full rule set. If Cecil chokes on net10.0, that is the gating risk; note it and fall back per AC1. [Source: https://www.nuget.org/packages/Mono.Cecil/; https://www.nuget.org/packages/NetArchTest.eNhancedEdition/1.3.7]
- Current dirty worktree: `_bmad-output/story-automator/orchestration-1-20260530-160445.md` is unrelated automation output — do not revert or overwrite it. [Source: git status]

### Testing Requirements

- xUnit v3 (3.2.2) + Shouldly (4.3.0) + NetArchTest (the only new dependency). New test classes live in `tests/Hexalith.ChatBot.Architecture.Tests/`. [Source: Directory.Packages.props]
- **Every NetArchTest assertion must surface `FailingTypeNames` on failure** (AC5). A bare `result.IsSuccessful.ShouldBeTrue()` with no message is insufficient — the failure must name the forbidden edge. [Source: _bmad-output/planning-artifacts/epics.md#Story 1.10]
- **Add the meta-test** proving detection works (feed a known-violating input → assert `IsSuccessful == false` + the offending type is named). Without it, a silently-misconfigured rule (e.g. a typo'd namespace that matches nothing → always "passes") would give false confidence. [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/.decision-log.md (FR86 verification)]
- **Must stay green (regression):** all 21 existing `ScaffoldArchitectureTests`, plus the full ChatBot suite (`DaprAccessControlConformanceTests`, `ContractSpineOracleTests`, `CommandGatewayTests` incl. the ordered 10-path inventory, UI/Integration/Server/Contracts/Client). This story adds tests; it must not flip any existing one. [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs; tests/Hexalith.ChatBot.Conformance.Tests/]
- **Build gate:** full solution builds 0 warnings / 0 errors under warnings-as-errors with no inline package versions (AC1). [Source: src/.../Directory.Build.props (TreatWarningsAsErrors); Directory.Packages.props]

### Out of Scope

- No differential-conformance harness or parity oracle (Story 1.11); no cross-tenant isolation harness (Story 1.12); no tenant-scoped fixture/evaluation scaffold (Story 1.13).
- No `Cli`/`Mcp`/`Workers` project scaffolding (Epic 5 / future M0–M1). No runtime/behavior change in `src/` beyond the `Directory.Packages.props` version entry.
- No rewrite/removal of the existing source-text/XML architecture tests. No weakening of `internal` visibility, no new `InternalsVisibleTo`, no inline package versions, no nested/recursive submodule operations, no hand-editing generated client files.

### References

- [Source: .agents/skills/bmad-create-story/SKILL.md#Create Story Workflow]
- [Source: .agents/skills/bmad-create-story/template.md]
- [Source: .agents/skills/bmad-create-story/checklist.md]
- [Source: CLAUDE.md#Git Submodules]
- [Source: _bmad-output/implementation-artifacts/sprint-status.yaml#development_status]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: First Safe Governed Action & Command Spine]
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.10: Architecture dependency fitness tests]
- [Source: _bmad-output/planning-artifacts/architecture.md#Structure Patterns]
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines]
- [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/prd.md#FR81a]
- [Source: _bmad-output/planning-artifacts/prds/prd-Hexalith.ChatBot-2026-05-28/addendum.md#Shared Command Pipeline (architectural invariant for FR81a)]
- [Source: tests/Hexalith.ChatBot.Architecture.Tests/ScaffoldArchitectureTests.cs]
- [Source: src/Hexalith.ChatBot.Server/Gateway/Stages/ (IRiskClassifier/IApprovalGate/IAuditWriter/IIdempotencyStore — internal)]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs; .../Handlers/IDomainProcessor.cs]
- [Source: https://www.nuget.org/packages/NetArchTest.eNhancedEdition/1.3.7]
- [Source: https://github.com/NeVeSpl/NetArchTest.eNhancedEdition]
- [Source: https://www.nuget.org/packages/NetArchTest.Rules/1.3.0]

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) — BMAD dev-story workflow.

### Debug Log References

Sandbox note (as in Stories 1.8/1.9): VSTest `dotnet test` is blocked; built each test csproj and ran the compiled xUnit v3 in-process binary directly. NetArchTest runs in-process (Cecil reads the DLLs in the output dir), so the direct-binary approach works unchanged.

**Package / Cecil verification (gating risk).** Queried nuget: `NetArchTest.eNhancedEdition` 1.3.7 pulls Mono.Cecil 0.11.5 (below the ≥0.11.6 flagged for net10.0); chose **1.4.5**, which brings **Mono.Cecil 0.11.6**. Confirmed via the committed canary `FitnessRuleMachineryTests.NetArchTestParsesNet10AssembliesWithoutCecilFailure` that Cecil loads and inspects a net10.0 ChatBot assembly with no exception.

**API note.** The eNhanced fork's surface differs from the original NetArchTest: `Should()`/`ShouldNot()` return `Condition`; there is **no singular `NotHaveDependencyOn`** (use `NotHaveDependencyOnAny(...)`), and `TestResult` exposes `FailingTypes` (each `IType` carries `FullName`/`Name`) — **not** `FailingTypeNames`. `NetArchTest.Rules.TestResult` also clashes with `Xunit.TestResult`, so it is aliased. Assertion messages thread the offending type name(s) from `FailingTypes` (AC5).

Commands (run from repo root):
- `dotnet restore Hexalith.ChatBot.slnx -m:1 /nr:false` → restored, exit 0.
- `dotnet build Hexalith.ChatBot.slnx --no-restore -m:1 /nr:false` → **Build succeeded, 0 Warning(s), 0 Error(s)** (warnings-as-errors; no inline package versions).
- Compiled-binary test sweep (all green):
  - Architecture: **Total 31** (21 pre-existing `ScaffoldArchitectureTests` + 10 new fitness tests), 0 failed. *(QA 2026-05-31: now **33** with the 2 `FitnessDiscoveryTests` non-vacuity guards — see QA Results.)*
  - Conformance: 3 · Server: 113 · Contracts: 66 · Client: 13 · UI: 8 · ServiceDefaults: 3 · Testing: 1 — all 0 failed.
  - Aspire: 2 · AppHost: 3 — 0 failed.
  - IntegrationTests: 3 (1 skipped — the Tier-3 Aspire E2E is env-gated on `HEXALITH_CHATBOT_TIER3`/Docker/DAPR, skipped by design, not a regression).
  - Sweep total: **248 tests, 0 failures, 1 env-gated skip** *(updated from 246 — the QA pass added 2 `FitnessDiscoveryTests`, Architecture 31→33; re-verified by the senior review sweep 2026-05-31).*

### Completion Notes List

- Test-only, behavior-preserving. The only production-tree edit is `Directory.Packages.props` (a `PackageVersion` entry — configuration, not behavior). No `src/**/*.cs` changed; no `internal`→`public`, no new `InternalsVisibleTo`, no inline package versions, no submodule changes.
- Added the assembly/IL-level (NetArchTest/Mono.Cecil) fitness layer **on top of** — never replacing — the 21 source-text/XML `ScaffoldArchitectureTests`; both layers stay green and complementary.
- **Package chosen:** `NetArchTest.eNhancedEdition` **1.4.5** (maintained fork, identical `NetArchTest.Rules` API) under central package management; bare `<PackageReference>` (no inline `Version=`) in the test csproj — `ProjectFilesShouldNotUseInlinePackageVersions` stays green.
- **AC1:** package added centrally; test csproj now ProjectReferences Contracts/Client/Server/UI + EventStore.Client (was zero refs) so Cecil can load the compiled DLLs from the output dir.
- **AC2:** `DependencyDirectionFitnessTests` — IL forbidden-edge checks: Contracts ↛ Client/Server; Client ↛ Server; Server ↛ UI/Aspire/AppHost; every adapter ↛ Server.
- **AC3:** `AdapterBoundaryFitnessTests` — no adapter type has an IL dependency on `…Server.Gateway` / `…Server.Gateway.Stages`. Adapter assemblies are discovered **dynamically** from `AppContext.BaseDirectory` (`Hexalith.ChatBot.{UI,Cli,Mcp,Workers}.dll`) so future Cli/Mcp/Workers are auto-covered with no test edit (forward-safe, AC5).
- **AC4:** `AggregatePlacementFitnessTests` — across every loaded ChatBot assembly **except** Server (EventStore.Client deliberately excluded), no type implements `IDomainProcessor` (NetArchTest), derives from `EventStoreAggregate<>` (reflection base-walk for the open generic), or resides in a `…Operations`/`…Projections` namespace.
- **AC5:** `FitnessRuleMachineryTests.FitnessRuleFailureSurfacesTheForbiddenEdge` — non-destructive meta-test feeds a deliberately-false rule against a true fact (Server.Operations *does* depend on `Hexalith.EventStore`); asserts `IsSuccessful == false` and that the failure names `GovernedOperationAggregate`. Proves detection works and the failure identifies the forbidden edge — no real violation/quarantined assembly introduced.
- Existing source-token guards (`GatewayStageSeamsShouldRemainInternalToServer`, `SurfaceAdaptersShouldNotWriteAuditRecordsDirectly`, `SurfaceAdaptersMustNotReferenceGatewayIdempotencyStages`, the exact-edge `ProjectReferences…Direction` / UI-adapter rules) were left fully intact — they cover not-yet-compiled future projects by file scan.

### File List

- `Directory.Packages.props` (modified — added `NetArchTest.eNhancedEdition` 1.4.5 PackageVersion in the Testing group)
- `tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj` (modified — added ProjectReferences to Contracts/Client/Server/UI + EventStore.Client and a bare NetArchTest PackageReference)
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessAssemblies.cs` (new — public-type-anchored assembly resolution + dynamic, forward-safe adapter discovery)
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessRule.cs` (new — failure-message helper that names the forbidden edge from `TestResult.FailingTypes`)
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/DependencyDirectionFitnessTests.cs` (new — AC2)
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AdapterBoundaryFitnessTests.cs` (new — AC3)
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/AggregatePlacementFitnessTests.cs` (new — AC4)
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessRuleMachineryTests.cs` (new — AC5 meta-test + net10.0 Cecil canary)
- `tests/Hexalith.ChatBot.Architecture.Tests/Fitness/FitnessDiscoveryTests.cs` (new, QA gap — non-vacuity guards: `AdapterDiscoveryIsNotVacuous` + `AdapterDiscoveryIncludesTheUiAdapter`; see QA Results)

## QA Results

### qa-generate-e2e-tests — gap analysis (2026-05-31)

**Discovered gap (auto-applied): adapter fitness rules could pass vacuously.** AC3's `AdapterBoundaryFitnessTests` and the adapter forbidden-edge checks in AC2 iterate `FitnessAssemblies.Adapters`, which is populated by *dynamic* discovery (scan `AppContext.BaseDirectory` for `Hexalith.ChatBot.{UI,Cli,Mcp,Workers}.dll`). If that discovery ever returned an empty set — a dropped `ProjectReference`, a renamed adapter, an output-dir copy regression — the `Types.InAssemblies([])` rules would assert over nothing and **pass with zero coverage**, giving false confidence. This is exactly the AC5/FR86 "a silently no-op rule is a false pass" failure mode the meta-test guards for the *machinery*, but the discovery input itself was unguarded. The existing AC tests did not cover it.

**Fix applied:** added `FitnessDiscoveryTests` (2 facts) pinning the discovery input as non-vacuous — `AdapterDiscoveryIsNotVacuous` (≥1 adapter found) and `AdapterDiscoveryIncludesTheUiAdapter` (UI, the only adapter today, is present). A dropped `ProjectReference` now fails loudly instead of silently hollowing out the adapter rules. Forward-safe discovery is unchanged; future Cli/Mcp/Workers remain auto-covered.

**Compile-failure correction (QA test as delivered did not build):** `FitnessDiscoveryTests` invoked `FitnessAssemblies.Adapters()` as a method, but `Adapters` is a property (`FitnessAssemblies.cs:43`) → `CS1955: Non-invocable member ... cannot be used like a method` at lines 30 and 45, failing the Architecture.Tests build (warnings-as-errors). Fixed by dropping the parentheses (`FitnessAssemblies.Adapters`). No other file touched; the property-vs-method shape of `FitnessAssemblies` was kept as authored.

**Verification (compiled xUnit v3 in-process binary; VSTest `dotnet test` sandbox-blocked):**
- `dotnet build tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj --no-restore -m:1 /nr:false` → **Build succeeded, 0 Warning(s), 0 Error(s)**.
- `./tests/Hexalith.ChatBot.Architecture.Tests/bin/Debug/net10.0/Hexalith.ChatBot.Architecture.Tests` → **Total: 33, Failed: 0, Skipped: 0** (the prior 31 + the 2 new non-vacuity guards). No existing test regressed.

## Senior Developer Review (AI)

**Reviewer:** Jerome · **Date:** 2026-05-31 · **Outcome:** ✅ Approve (auto-fixes applied)

**Scope reviewed:** `Directory.Packages.props`, `tests/Hexalith.ChatBot.Architecture.Tests/Hexalith.ChatBot.Architecture.Tests.csproj`, and the 7 `Fitness/*.cs` files. `_bmad-output/` and automation artifacts excluded per workflow.

**Verification performed (not assumed):**
- Full solution build under warnings-as-errors → **0 warnings / 0 errors** (AC1 build gate confirmed).
- Re-ran the full compiled-binary test sweep: Architecture **33**, Conformance 3, Server 113, Contracts 66, Client 13, UI 8, ServiceDefaults 3, Testing 1, Aspire 2, AppHost 3, IntegrationTests 3 → **248 tests, 0 failed, 1 env-gated Tier-3 skip**.
- Confirmed every rule's targets are real (not typo'd → non-vacuous): namespaces `Hexalith.ChatBot.Server.Gateway`, `…Gateway.Stages`, `…Server.Operations` exist; anchor types `RecordGovernedNote`/`ChatBotClient`/`GovernedOperationAggregate` are public; `IDomainProcessor`/`EventStoreAggregate<>` are public in EventStore.Client.
- AC2 design split holds: the IL layer asserts forbidden edges; the existing XML `ProjectReferencesShouldFollowContractsClientServerDirection` still asserts Client's exact ref set = `[Contracts]` (and Contracts = empty). All 21 pre-existing `ScaffoldArchitectureTests` intact.
- File List cross-checked against `git status` — accurate; no undocumented source changes.

**AC coverage:** AC1–AC5 all implemented and verified. No CRITICAL or HIGH findings; no task marked `[x]` was found incomplete.

**Findings & auto-fixes applied (this review):**
- 🟡 MEDIUM — Removed dead member `FitnessAssemblies.EventStoreClient` (never referenced; its doc comment misstated the reason the EventStore.Client assembly is present — the csproj `ProjectReference` is the real reason) and the now-unused `using Hexalith.EventStore.Client.Aggregates;`.
- 🟢 LOW — Corrected the stale "Sweep total: 246" in Debug Log References to **248** (the QA pass had added 2 tests without updating the sweep total).
- 🟢 LOW — Normalized `Fitness/FitnessDiscoveryTests.cs` to match its six sibling Fitness files: removed the lone copyright header, moved `using`s outside the file-scoped namespace (per `.editorconfig csharp_using_directive_placement = outside_namespace`), switched to `public static class` / `public static void` (suite convention), and dropped redundant `using System.Linq;`/`using Xunit;`.

Post-fix re-verification: Architecture.Tests rebuild **0W/0E**, binary **Total 33, 0 failed**. No behavior change; the 2 non-vacuity guards still pass.

## Change Log

- 2026-05-31: Created Story 1.10 context (assembly/IL-level NetArchTest dependency-direction + adapter-boundary + aggregate/projection-placement fitness tests, layered on top of the existing source-text/XML `ScaffoldArchitectureTests`; maintained `NetArchTest.eNhancedEdition` added under central package management; failure messages must name the forbidden edge per FR86; forward-safe adapter discovery for future Cli/Mcp/Workers). Test-only, behavior-preserving. Status set to ready-for-dev.
- 2026-05-31: Implemented Story 1.10. Added `NetArchTest.eNhancedEdition` 1.4.5 (Mono.Cecil 0.11.6, parses net10.0) centrally; wired Architecture.Tests ProjectReferences (Contracts/Client/Server/UI + EventStore.Client). Added the IL-level fitness layer in `tests/.../Fitness/`: dependency-direction (AC2), adapter-boundary with dynamic forward-safe adapter discovery (AC3), aggregate/projection placement (AC4), and a non-destructive failure-path meta-test + net10.0 Cecil canary (AC5) — each failure surfaces the offending type via `TestResult.FailingTypes`. Existing 21 `ScaffoldArchitectureTests` and all other source-token guards left intact. Full solution builds 0 warnings / 0 errors under warnings-as-errors; compiled-binary sweep = 246 tests, 0 failures (Architecture 21→31), 1 env-gated Tier-3 E2E skip. Status set to review.
- 2026-05-31: QA `qa-generate-e2e-tests` gap pass. Discovered the adapter fitness rules could pass *vacuously* if dynamic adapter discovery returned empty (dropped ProjectReference / output-dir regression) — unguarded input to AC2/AC3. Auto-applied `Fitness/FitnessDiscoveryTests.cs` (2 non-vacuity guards). Corrected its compile failure (`CS1955` — property `FitnessAssemblies.Adapters` invoked as a method at lines 30, 45; dropped the `()`). Architecture.Tests rebuild 0W/0E; compiled binary **Total 33, 0 failed** (31 → 33). See QA Results.
- 2026-05-31: Senior Developer Review (AI). Re-verified the build (full solution 0W/0E) and the full compiled-binary sweep (**248 tests, 0 failed, 1 env-gated skip**). Auto-fixed 1 MEDIUM (removed dead `FitnessAssemblies.EventStoreClient` + unused using) and 2 LOW (stale sweep count 246→248; normalized `FitnessDiscoveryTests.cs` style to match sibling Fitness files). Post-fix Architecture.Tests 0W/0E, **Total 33, 0 failed**. No CRITICAL findings → Status set to done. See Senior Developer Review (AI).
