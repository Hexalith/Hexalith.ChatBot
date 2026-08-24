# Story 1.1e Standalone Validation Evidence — 2026-08-23

Evidence recorder: GPT-5 Codex

## Authorized baseline-repair supplement — 2026-08-23

Jerome explicitly authorized the cross-repository baseline repair needed to make the
Story 1.1e graph mutually compatible. The authorization covers the narrow
repository-owned source, test, configuration, and eventual gitlink changes proved
below. It does not authorize staging, committing, pushing, fetching, recursive
submodule initialization, dependency removal from a solution, local package-version
overrides, or weaker analyzer/build/test gates. No such prohibited action was taken.

Outcome: **PREPARED AND FUNCTIONALLY GREEN, BUT NOT AN IMMUTABLE AC5 BASELINE.**

The compatible source set is complete and validated, but several owning repositories
still contain uncommitted changes. Consequently their future commit IDs, their direct
dependency gitlinks, and the final ChatBot root commit do not yet exist. The final
exact-commit standalone matrix and umbrella run remain correctly blocked; Story 1.1e
stays `in-progress`.

### Prepared source graph

| Repository | Source baseline used | Prepared state |
| --- | --- | --- |
| ChatBot | `12a6b02ede0e1353b8fb031cfb81e3d39ea0f8da` | Story/dossier edits only; final gitlinks not yet immutable |
| Builds | `2f46aaee2ecb0b3f121d50ab8cc58601901046f4` | Uncommitted timeout-fixture stabilization, `FsCheck` 3.3.3 authority row, regenerated 285-package audit |
| EventStore | `516f2489f6586d35eee58f1158a840c404632637` | No source repair |
| Tenants | `d3527c84175d1dd2910bf7e186957b18da22bd96` | No source repair |
| FrontComposer | `c4df029050cb241f74cafd04a01f7718eae1ec0c` | Jerome committed the validated rc5 compatibility content while this run was active; ChatBot does not yet pin it |
| Folders | `154215c60438a5dae14f660609f7f181c818091f` | No source repair |
| Conversations | `5e6c621b160f93c30a40d6d6ec24fc7191be2f12` | No source repair |
| Projects | `fefdb06bf89683e1d690d0b570306ae7758780d1` | No source repair |
| Parties | `3d3abef4279e41cf0025870152e3fc597e26f872` | Uncommitted package-cold-build and rc5 bUnit test repairs |
| Memories | `003fd21488d60307cd932a3139f69319a25cea66` | Uncommitted StackExchange.Redis 3.1.13 production/test compatibility repairs |
| Commons | `5ff390a46685c72145de2337893f71ec8bc6a62c` | No source repair |
| Timesheets | `cfd9e62fbcf35081138b9ecf9ea192de7b9d5fda` | Uncommitted analyzer-workaround removal and target-graph test-host/topology repairs |
| PolymorphicSerializations | `93bcc44a65cd42fcc4558de8f8a8e4d523486157` | Uncommitted strict using placement and explicit CLS assembly contract |
| Works | `d9b0f110c29c815b1b2a75f59911e868b01e9c5a` | Uncommitted migration from 52 local versions to Builds authority plus sibling-graph compatibility |

`Works` is included because it is root-declared by the current ChatBot `.gitmodules`
and is therefore independently subject to AC1/AC2. No `.gitmodules` file or `.slnx`
membership changed. Validation clones used only direct sibling dependency checkouts;
all dependency-owned nested gitlinks remained uninitialized.

### Repair details

- Builds now owns `FsCheck` `3.3.3`, preserving Works' prior effective version rather
  than silently upgrading it. The generated package-version audit covers 285 packages,
  140 families, and one configured source. The Dapr process-tree fixture retains the
  production timeout behavior and changes only its dedicated child-PID scenario from
  one to three seconds.
- FrontComposer consumes the user-requested latest Fluent V5 packages,
  `5.0.0-rc.5-26219.1`, and its tests account for rc5 virtualized-grid and transient
  provider-refresh rendering. No product virtualization behavior changed.
- Parties completes bUnit's rc5 clipboard setup, cold-builds Commons.UniqueIds before
  package tests, and removes a stale exact EventStore package assertion while retaining
  evaluated catalog/nonblank checks.
- PolymorphicSerializations enforces `inside_namespace:error` locally, mechanically
  conforms its source, and declares the code-generator assembly non-CLS-compliant.
  No analyzer was suppressed.
- Memories uses the StackExchange.Redis 3.1.13 supported exception signatures and the
  atomic `StringDeleteAsync(RedisKey, ValueCondition, CommandFlags)` API. The only
  reflection is a centralized test helper for `RedisServerException`'s public
  constructor, whose first parameter is an experimental enum; production contains no
  reflection or `SER007`/`SER301` suppression.
- Timesheets removes its consumer analyzer exception, limits its sibling-package
  architecture scan to repository-owned project trees, and fully replaces both the
  `IReadModelStore` and concrete `DaprReadModelStore` registrations in the isolated HTTP
  test host.
- Works imports Builds through standalone/sibling/umbrella paths, removes all 52 local
  version rows and warning-as-error bypasses, and resolves direct dependencies and
  AppHost metadata through either an initialized nested checkout or a direct sibling.
  Its obsolete EventStore JSON-misbinding characterization now asserts the target SDK's
  correct Web-JSON binding.

### Compatibility matrix

| Repository | Authority / build | Test result | Classification |
| --- | --- | --- | --- |
| Builds | 285-entry catalog; six-project authority; restore and Release build, 0 warnings/errors | 146/146; central 14, audit generator 55, audit validator 60, consumer 16, exception 7, Dapr 29, workflow 20 | GREEN |
| EventStore | authority 50; restore and Release build, 0 warnings/errors | Client 768; Server 3,106 passed +25 documented skips | GREEN |
| Tenants | authority 17; restore and Release build, 0 warnings/errors | Server 747/747 | GREEN |
| Commons | authority 20; restore and Release build, 0 warnings/errors | 356/356 | GREEN |
| PolymorphicSerializations | authority 4; restore and Release build, 0 warnings/errors | 15/15 | GREEN |
| Projects | authority 23; canonical CI restore and Release build, 0 warnings/errors | 1,754/1,754 across eight projects | GREEN |
| FrontComposer | rc5 focused lanes and Release solution build, 0 warnings/errors | non-Shell 1,934 green; Shell 2,647/2,648 | FUNCTIONALLY GREEN; sole failure is immutable Builds-SHA lockstep pending a Builds commit |
| Folders | package-authority/story lanes green | 3,765 unit tests green; full governance 129/131 | PACKAGE GREEN; two unrelated planning-integrity failures |
| Conversations | package-authority/story lanes green | 2,072 passed /15 failed | PACKAGE GREEN; failures are unrelated planning/working-tree integrity checks |
| Parties | authority 30; restore and Release build, 0 warnings/errors; package cold-output lanes green | unit 1,751; CI 37; UI 329; focused GDPR 4; topology 35 +6 documented skips | STORY GREEN; full domain 555/559 has four unrelated planning/gitlink failures |
| Memories | authority 29; restore and Release build, 0 warnings/errors | 4,819 passed +1 documented submodule-guard skip | GREEN |
| Timesheets | authority 15; restore and canonical Release build, 0 warnings/errors | 788 passed +4 documented integration/performance skips | GREEN |
| Works | authority 13; restore and Release build, 0 warnings/errors | 686 passed +4 documented Aspire-infrastructure skips | GREEN |

Timesheets and Works each completed solution-level `dotnet pack -c Release
--no-build --no-restore`; each produced five packages, promoted dependencies were
inspected from the `.nuspec` files, and neither lane reported `NU1109`.

The initial focused Timesheets integration rebuild emitted one `MSB3491` warning when
the same EventStore source project was reached twice concurrently. The canonical
solution rebuild immediately afterward completed with zero warnings and zero errors;
the warning was not suppressed.

### Commands and integrity

The canonical command shape for every prepared repository was:

```text
pwsh -NoProfile -File <Builds authority/fixture validator>
dotnet restore <canonical.slnx> --force-evaluate
dotnet build <canonical.slnx> -c Release --no-restore
dotnet test <each repository-owned test project> -c Release --no-build --no-restore
dotnet pack <canonical.slnx> -c Release --no-build --no-restore -o <temporary output>
```

Repository-specific direct xUnit v3 execution and existing category exclusions were
used where the repository's test platform required them. Every owning repository and
the ChatBot root pass `git -c core.whitespace=cr-at-eol diff --check`. No nested
submodule is live, no tracked `.gitmodules` changed, no solution project was removed,
and no file was staged, committed, pushed, fetched, or remotely updated by this agent.

### Immutable landing order and remaining gate

The following commits must be created by an authorized committer before AC5 can be
rerun at exact immutable identities:

1. Builds: catalog, deterministic audit, and Dapr fixture.
2. PolymorphicSerializations: analyzer conformance and code-generator CLS contract,
   then pin the new Builds commit.
3. Works: authority wrapper, strict dependency graph, and compatibility tests, then
   pin the new Builds/Polymorphic commits.
4. FrontComposer: a follow-up dependency-gitlink/lockstep commit on top of
   `c4df029050cb241f74cafd04a01f7718eae1ec0c` that pins the new Builds commit.
5. Parties and Memories: their compatibility repairs and direct dependency pins.
6. Timesheets: its compatibility repairs and direct Builds/Polymorphic/Works pins.
7. Any remaining consumer dependency-gitlink updates required to select the already
   validated EventStore/Tenants/Commons/Projects/FrontComposer/Folders/Conversations
   content.
8. ChatBot: pin the resulting immutable consumer graph and record that root commit.

Only after those commit IDs exist can the exact standalone matrix be regenerated and
the unchanged ChatBot restore/build/package-authority/UI/architecture/integration stage
run. Future hashes cannot be reported honestly before those commits are created.

## Historical frozen-baseline attempt

The remainder of this dossier preserves the earlier `bd652e3` frozen-baseline attempt.
Its red classifications explain why Jerome authorized the repair above; they are not
the result of the prepared target graph.

Recorded ChatBot baseline: `bd652e3c61ebfa0202f6a1fdb696759637a21bca`

Outcome: **FAIL — final ChatBot umbrella validation is not authorized.**

This dossier records the required isolated-checkout attempt against the exact pin matrix frozen by Story 1.1e. It does not reuse evidence from nested checkouts under the live ChatBot worktree. Every standalone clone was detached at the expected SHA, had an empty `git rev-parse --show-superproject-working-tree`, and had empty `git status --short` output before validation.

## Identity and isolation

| Role | ChatBot gitlink | Repository URL | Expected and standalone `HEAD` | Before validation |
| --- | --- | --- | --- | --- |
| Authority | `references/Hexalith.Builds` | `https://github.com/Hexalith/Hexalith.Builds.git` | `e4ae82df6cfcc6511a32fc2ce100070d7924f119` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.EventStore` | `https://github.com/Hexalith/Hexalith.EventStore.git` | `afcc167e0c539b09ecad978a58da2f756123f34e` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.Tenants` | `https://github.com/Hexalith/Hexalith.Tenants.git` | `f03b474d836a3465e311c48e90c75ae1e755ef45` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.FrontComposer` | `https://github.com/Hexalith/Hexalith.FrontComposer.git` | `550cb0602d506d9fd008a8c09f2cca6b328ec1e3` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.Folders` | `https://github.com/Hexalith/Hexalith.Folders.git` | `cfe830b410bce6e04308ea67c3492eca6bc8bdfd` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.Conversations` | `https://github.com/Hexalith/Hexalith.Conversations.git` | `6e8cf8a6605142808d1afede3f2d0e29541f0e08` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.Projects` | `https://github.com/Hexalith/Hexalith.Projects.git` | `fca2bfc050f7a581a467aaa3921e2aa61f249e72` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.Parties` | `https://github.com/Hexalith/Hexalith.Parties.git` | `b316dab5cf27d9f80a662b5b3cd6c5e2569adfd7` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.Memories` | `https://github.com/Hexalith/Hexalith.Memories.git` | `f474db156c372aad4ab243c13a669c35d78b49e6` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.Commons` | `https://github.com/Hexalith/Hexalith.Commons.git` | `ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.Timesheets` | `https://github.com/Hexalith/Hexalith.Timesheets.git` | `441f02509cfd43c888e2d4317a167b41657208b4` | detached, empty superproject, clean |
| Consumer | `references/Hexalith.PolymorphicSerializations` | `https://github.com/Hexalith/Hexalith.PolymorphicSerializations.git` | `a5dd24f5e66324d18241de7d5521ee124eab4877` | detached, empty superproject, clean |

The ChatBot baseline was also cloned independently at `bd652e3c61ebfa0202f6a1fdb696759637a21bca` and passed the same detached/empty-superproject/clean identity checks. It was not built or tested because the standalone gate below is red.

## Root-dependency proof

For each repository with root submodules, the exact initialization form was:

```text
git submodule update --init -- <the explicit pathspecs listed for that row below>
```

The disposable clones' untracked Git configuration redirected the declared remote URLs to already-present local source clones; no tracked URL, `.gitmodules`, or gitlink changed. No `--recursive` or `--remote` option was used.

- `Hexalith.Builds`: no root submodules.
- `Hexalith.EventStore`: `references/Hexalith.Tenants@f03b474d836a3465e311c48e90c75ae1e755ef45`, `references/Hexalith.AI.Tools@991e8ea1b39bfb8170aea9a6857c25c7c69176c1`, `references/Hexalith.Commons@ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c`, `references/Hexalith.Builds@ed7cea8e1f943b4c47a454a0e8f462f0fae9891d`, `references/Hexalith.FrontComposer@550cb0602d506d9fd008a8c09f2cca6b328ec1e3`, `references/Hexalith.PolymorphicSerializations@a5dd24f5e66324d18241de7d5521ee124eab4877`, `references/Hexalith.Memories@f474db156c372aad4ab243c13a669c35d78b49e6`; 7/7 direct paths initialized and 25/25 dependency-owned nested paths remained uninitialized.
- `Hexalith.Tenants`: `references/Hexalith.EventStore@409731baef9ed974f715f00a2f048f9ba486cb3f`, `references/Hexalith.Commons@ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c`, `references/Hexalith.AI.Tools@991e8ea1b39bfb8170aea9a6857c25c7c69176c1`, `references/Hexalith.FrontComposer@550cb0602d506d9fd008a8c09f2cca6b328ec1e3`, `references/Hexalith.Builds@ed7cea8e1f943b4c47a454a0e8f462f0fae9891d`, `references/Hexalith.PolymorphicSerializations@a5dd24f5e66324d18241de7d5521ee124eab4877`, `references/Hexalith.Memories@f474db156c372aad4ab243c13a669c35d78b49e6`; 7/7 direct paths initialized and 25/25 nested paths remained uninitialized.
- `Hexalith.FrontComposer`: `references/Hexalith.EventStore@689f71bf696246ab271956a3a1c218d6e51386fb`, `references/Hexalith.Tenants@088232a7255698e20105594d9e0ef12a0f09c73e`, `references/Hexalith.Commons@ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c`, `references/Hexalith.Builds@ffa1662829b28d1d90554980c87f23bd9d4e25e7`, `references/Hexalith.PolymorphicSerializations@f977018abdd34de93c82ed050b746e4e30b0a960`, `references/Hexalith.AI.Tools@991e8ea1b39bfb8170aea9a6857c25c7c69176c1`, `references/Hexalith.Memories@1a557ca3c7a50c7fe0db2dedfd1af2d3b21fe83b`, `references/Hexalith.Parties@b316dab5cf27d9f80a662b5b3cd6c5e2569adfd7`; 8/8 direct paths initialized and 32/32 nested paths remained uninitialized.
- `Hexalith.Folders`: `references/Hexalith.Tenants@088232a7255698e20105594d9e0ef12a0f09c73e`, `references/Hexalith.AI.Tools@991e8ea1b39bfb8170aea9a6857c25c7c69176c1`, `references/Hexalith.EventStore@0031425988382e6383807837fa98c34ad435af18`, `references/Hexalith.FrontComposer@3289f9fc12e6c7d3f1683366ef849b0002483339`, `references/Hexalith.Memories@6779f7cb833341f5bbd070810d8540c72d324076`, `references/Hexalith.Commons@ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c`, `references/Hexalith.Builds@ffa1662829b28d1d90554980c87f23bd9d4e25e7`, `references/Hexalith.PolymorphicSerializations@f977018abdd34de93c82ed050b746e4e30b0a960`; 8/8 direct paths initialized and 32/32 nested paths remained uninitialized.
- `Hexalith.Conversations`: `references/Hexalith.AI.Tools@f265a1721e013e68399a47bca7152265701ef594`, `references/Hexalith.EventStore@1ae201752e5807050c7107e58cb3a1f1b3ab5b0c`, `references/Hexalith.Projects@e4ba1aa608f7caaa1214aef6117e98bb722598ca`, `references/Hexalith.Folders@1ea2c61525b119ac71e50ca083e4ed897da9563f`, `references/Hexalith.Tenants@28630b94a7b4931dcd6796eb50ad1c21b092055d`, `references/Hexalith.FrontComposer@0a84e818b0ce220f291510ad094340f7296bb488`, `references/Hexalith.Parties@a35b1515b81d00dd9c58de5988a30f3c620d3d60`, `references/Hexalith.Memories@0208bc4f35712feb923379629daa3a69be69ed19`, `references/Hexalith.Commons@b03469b13408530bb757d3d02279c2d772ee4848`, `references/Hexalith.Builds@9708e242e6334469f839670761fe61633dae8ce4`; 10/10 direct paths initialized and 57/57 nested paths remained uninitialized.
- `Hexalith.Projects`: `references/Hexalith.AI.Tools@991e8ea1b39bfb8170aea9a6857c25c7c69176c1`, `references/Hexalith.EventStore@095b85b4fb4bacae3bd16450dbda4044c53079ad`, `references/Hexalith.Tenants@088232a7255698e20105594d9e0ef12a0f09c73e`, `references/Hexalith.FrontComposer@550cb0602d506d9fd008a8c09f2cca6b328ec1e3`, `references/Hexalith.Conversations@6e8cf8a6605142808d1afede3f2d0e29541f0e08`, `references/Hexalith.Folders@6d392d71dad3344b82ec6c1c93dd64a05347e1f5`, `references/Hexalith.Parties@b316dab5cf27d9f80a662b5b3cd6c5e2569adfd7`, `references/Hexalith.Commons@ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c`, `references/Hexalith.Builds@ffa1662829b28d1d90554980c87f23bd9d4e25e7`, `references/Hexalith.Memories@e6164c8b9bef9c9f67ec2fd95100055f8084cab3`; 10/10 direct paths initialized and 57/57 nested paths remained uninitialized.
- `Hexalith.Parties`: `references/Hexalith.EventStore@8ce84653099240245caca7a113f77d0c4a688c6b`, `references/Hexalith.Memories@02fe7932a0ad7c506cb754d406b234a5d00d3125`, `references/Hexalith.FrontComposer@afb39847f313b41266635149baafb602362f1e8e`, `references/Hexalith.Tenants@2d85e35a2646df9c0e2ccc2cfae295269bfd166d`, `references/Hexalith.AI.Tools@991e8ea1b39bfb8170aea9a6857c25c7c69176c1`, `references/Hexalith.Commons@05f0d9f4f6360bef9c33f14396b0c0c74cbd6864`, `references/Hexalith.Builds@c177c66af5d3f509328c2f568dc0737fe9f89e4e`, `references/Hexalith.PolymorphicSerializations@8d0a3cf530246260015519cf0d9ed3e4220278a7`; 8/8 direct paths initialized and 32/32 nested paths remained uninitialized.
- `Hexalith.Memories`: `references/Hexalith.Commons@ea1fc4551dcaf8ee63fd562d77dfe0f18c57a94c`, `references/Hexalith.EventStore@bccc25601ae8226290324bf2adfbce69bcfc40cf`, `references/Hexalith.AI.Tools@991e8ea1b39bfb8170aea9a6857c25c7c69176c1`, `references/Hexalith.Tenants@088232a7255698e20105594d9e0ef12a0f09c73e`, `references/Hexalith.FrontComposer@550cb0602d506d9fd008a8c09f2cca6b328ec1e3`, `references/Hexalith.Builds@ffa1662829b28d1d90554980c87f23bd9d4e25e7`, `references/Hexalith.PolymorphicSerializations@a5dd24f5e66324d18241de7d5521ee124eab4877`; 7/7 direct paths initialized and 25/25 nested paths remained uninitialized.
- `Hexalith.Commons`: `references/Hexalith.Builds@1a15a0caf3fa77b67fdc9e46e436264d9109a833`, `references/Hexalith.PolymorphicSerializations@a468af2aee4255e5ff147acb3dd5b1cd327b292c`; 2/2 direct paths initialized and its one valid nested gitlink remained uninitialized.
- `Hexalith.Timesheets`: `Hexalith.Commons@d976d5639172e07b8dea3b74a0bfe3be5d65e0d7`, `Hexalith.AI.Tools@993169659a7aa8f1b1dc8444a49d876bbb7175f7`, `references/Hexalith.Builds@f0750ca703cc3ada6eb25050cb6b287e83ce3938`, `Hexalith.Tenants@e202f3b3c50949f9ac6da432d4f26f78b999d0e0`, `Hexalith.EventStore@35ff5eff17ac271a82b9e74b5ba66f11fc593465`, `Hexalith.FrontComposer@ade7b2fef609bf6761fbf0eab9abe3d77bd62013`, `Hexalith.PolymorphicSerializations@db291e8bbcebe506d808fa01a1b1a3b583b26a15`, `Hexalith.Projects@688bda86c7e1a9254bda3fe9230ac7531ff369ae`, `Hexalith.Conversations@8c320bda6dfbec649cbc4f1473a26d11fb8c7cf3`, `Hexalith.Works@f2259daab922096113262fc9e0a5588182918e0a`, `Hexalith.Parties@e1d4ee2241480e1d6a70ea19ab4054270d5237a9`; 11/11 direct paths initialized. All 61 valid dependency-owned nested gitlinks remained uninitialized. One dependency `.gitmodules` entry named `Hexalith.Builds` is stale and is not a tracked gitlink, so the nested audit reports 61/62 rather than silently treating it as initialized.
- `Hexalith.PolymorphicSerializations`: `references/Hexalith.Builds@598f5063f13dccbaa1251d8af6a8a72ad5820c20`; 1/1 direct path initialized and it declares no nested submodules.

## Package-governance results

From standalone `Hexalith.Builds`:

| Command | Result |
| --- | --- |
| `pwsh -NoLogo -NoProfile -File Tools/test-authoritative-package-catalog.ps1` | PASS, 48 governed values |
| `pwsh -NoLogo -NoProfile -File Tools/test-central-package-version-validator.ps1` | PASS, exit 0 |
| `pwsh -NoLogo -NoProfile -File Tools/test-consumer-package-authority-validator.ps1` | PASS, 16 scenarios |
| `pwsh -NoLogo -NoProfile -File Tools/test-package-version-exception-validator.ps1` | PASS, 7 scenarios |
| `pwsh -NoLogo -NoProfile -File Tools/test-dapr-package-version-validator.ps1` | FAIL, 28/29; the one-second timeout case did not record its child PID before termination |
| `DOTNET_ROLL_FORWARD=Major /tmp/hexalith-story-1-1e-QgMzyn2f/tools/pwsh-7.5.2/pwsh -NoLogo -NoProfile -File Tools/test-dapr-package-version-validator.ps1` | Same FAIL, 28/29 under PowerShell 7.5.2; all other timeout/deadlock/release-order cases pass |
| `pwsh -NoLogo -NoProfile -File Tools/validate-central-package-versions.ps1 Props/Directory.Packages.props` | PASS, 283 catalog entries |
| `pwsh -NoLogo -NoProfile -File Tools/validate-dapr-package-versions.ps1 Props/Directory.Packages.props` | PASS, 8 unique `Dapr.*` identities at `1.18.4` |
| `pwsh -NoLogo -NoProfile -File Tools/validate-package-version-exceptions.ps1 -InventoryPath Tools/package-version-exceptions.json -CatalogPath Props/Directory.Packages.props` | PASS, exact 15-entry exception allowlist |
| `pwsh -NoLogo -NoProfile -File Tools/validate-consumer-package-authority.ps1 . Props/Directory.Packages.props` | PASS, 6 Builds projects |

The consumer validator was then run from the authority checkout against each isolated consumer root and that root's actual direct Builds catalog:

| Consumer | Result |
| --- | --- |
| EventStore | PASS, 48 projects |
| Tenants | PASS, 17 projects |
| FrontComposer | PASS, 23 projects |
| Folders | PASS, 32 projects |
| Conversations | FAIL, 17 errors: its direct Builds pin does not evaluate `CentralPackageVersionOverrideEnabled=false` |
| Projects | PASS, 23 projects |
| Parties | PASS, 29 projects |
| Memories | PASS, 29 projects |
| Commons | FAIL, 21 errors: override protection is absent and the catalog lacks `Microsoft.Extensions.Diagnostics.Abstractions` |
| Timesheets | FAIL, 34 errors: override protection is absent and required central rows are missing, including `MinVer`, NSubstitute, and MVC testing rows |
| PolymorphicSerializations | FAIL, 4 errors: its direct Builds pin does not evaluate override protection as false |

No resolved-graph claim is made for a row that failed authority or restore. The previously recorded resolved-graph and pack subtasks remain historical evidence; this attempt does not relabel them as results at a different pin.

## Canonical repository and focused-test results

All restore commands used `-p:NuGetAudit=false` where the repository did not already disable auditing. Builds used serialized warning-as-error Release builds (`-warnaserror -m:1 /nr:false`). The same serialization was used for consumer Release builds where applicable.

| Repository | Exact command/result summary |
| --- | --- |
| Builds | `dotnet restore Hexalith.Builds.slnx -p:NuGetAudit=false` PASS; `dotnet build Hexalith.Builds.slnx --configuration Release --no-restore -warnaserror -m:1 /nr:false` PASS, 0 warnings/errors; Evidence 11/11, Module 52/52, Tooling Integration 1/1 (64/64 total); reusable workflow fixtures 20/20. Row remains red because the required Dapr fixture is 28/29. |
| EventStore | `dotnet restore Hexalith.EventStore.slnx -p:NuGetAudit=false` PASS; serialized warning-as-error Release build PASS, 0 warnings/errors; Client 680/680; Server 2,788 passed, 25 skipped (2,813 total). |
| Tenants | `dotnet restore Hexalith.Tenants.slnx -p:NuGetAudit=false` PASS; serialized warning-as-error Release build PASS, 0 warnings/errors; Server 738/738. |
| FrontComposer | `dotnet restore Hexalith.FrontComposer.slnx -p:NuGetAudit=false` PASS; `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore -warnaserror -m:1 /nr:false` PASS, 0 warnings/errors. Required solution test with `--filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` produced 4,179 passed and 2 failed. After `npm ci`, a focused rerun cleared the semantic-release analyzer failure; the remaining analyzer-policy test fails against stale identifier inventory `count=6196`, SHA-256 `43d0f3531ded807bf856fd7e78e12b7900c1ddf696cebd76842ac3d99fee03ba`. |
| Folders | `dotnet restore Hexalith.Folders.slnx -p:NuGetAudit=false` PASS. Required Release package-mode build fails with 185 compile errors because the pinned published EventStore/Memories/Tenants/FrontComposer packages do not expose APIs used by this source. The repository-documented Debug command `dotnet build Hexalith.Folders.slnx --no-restore -m:1 /nr:false` passes with 0 warnings/errors; focused Server 565/565 and UI 521/521 pass. This does not replace the failed Release evidence. |
| Conversations | `dotnet restore Hexalith.Conversations.slnx -p:Configuration=Release -p:NuGetAudit=false` PASS; `DiffEngine_Disabled=true dotnet build Hexalith.Conversations.slnx --configuration Release --no-restore -warnaserror -m:1 /nr:false` PASS, 0 warnings/errors. Release tests: Contracts 618/618, Server 610/610, core 185/185, Admin Web 14/14, AppHost 7/7, Client 29/29, Integration 9/9, ServiceDefaults 7/7; Conformance 398/400 with two unrelated planning-prefix/artifact-hash drift failures. Authority is also red at this direct Builds pin. |
| Projects | `dotnet restore Hexalith.Projects.CI.slnx -p:HexalithCommonsRoot=/tmp/hexalith-story-1-1e-QgMzyn2f/Hexalith.Projects/references/Hexalith.Commons -p:NuGetAudit=false` PASS; corresponding serialized warning-as-error Release build PASS, 0 warnings/errors; all eight CI test projects pass: CLI 13, MCP 23, Integration 19, Client 114, core 656, UI 140, Contracts 164, Server 568 (1,697 total). |
| Parties | `dotnet restore Hexalith.Parties.slnx -p:NuGetAudit=false` FAILS with NU1506. Direct EventStore, Commons, and Polymorphic commits still contain local `PackageVersion` rows which duplicate the catalog imported from Parties' newer direct Builds pin. A focused Client restore reproduces the same Commons duplicates. Build and test lanes were not run after restore failed. |
| Memories | `dotnet restore Hexalith.Memories.slnx -p:Configuration=Release -p:WarningsNotAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904 -p:NuGetAudit=false` PASS; serialized Release build PASS, 0 warnings/errors. The first build attempt exhausted `/tmp` inodes while copying Web specimen files; after removing only generated artifacts from completed disposable checkouts, the identical rerun passed. EventStore 129/129, Web 492/492, Server 2,742 passed with 1 intentional submodule-guard skip. |
| Commons | `dotnet restore Hexalith.Commons.slnx -p:NuGetAudit=false` FAILS NU1010 because direct Builds `1a15a0c` lacks the central `Microsoft.Extensions.Diagnostics.Abstractions` row. The unaffected `Hexalith.Commons.Tests` project builds with 0 warnings/errors and passes 200/200; this does not replace the failed solution restore. |
| Timesheets | `DOTNET_CLI_HOME=/tmp/hexalith-story-1-1e-QgMzyn2f/dotnet-cli-home dotnet restore Hexalith.Timesheets.slnx` FAILS NU1506. Its direct Commons and Polymorphic commits retain local `Microsoft.Extensions.Diagnostics.Abstractions`/`MinVer` rows which conflict with the imported catalog. Build and focused tests were not run after restore failed. The permitted Works checkout is exactly `f2259daab922096113262fc9e0a5588182918e0a`. |
| PolymorphicSerializations | `dotnet restore Hexalith.PolymorphicSerializations.slnx -p:NuGetAudit=false` PASS; `dotnet build Hexalith.PolymorphicSerializations.slnx --configuration Release --no-restore -warnaserror -m:1 /nr:false` FAILS with 26 deterministic `IDE0065` diagnostics at the pinned source commit; the emitted test assembly passes 15/15. |

## Integrity proof

- No tracked `.gitmodules`, `.slnx`, source, catalog, dependency, or gitlink was changed in any standalone checkout.
- No nested dependency submodule was initialized.
- FrontComposer's tests materialized line-ending-only changes in six tracked Pact JSON fixtures. Those generated changes were restored exactly from the detached `HEAD`; final status is clean.
- Generated `bin`, `obj`, `TestResults`, and npm dependency artifacts were removed only from already-completed disposable checkouts when `/tmp` reached inode capacity. This did not affect source or Git evidence.
- Final `git status --short` is empty and `git diff --check` passes for Builds, all eleven consumer checkouts, and the isolated ChatBot baseline clone.
- The live ChatBot worktree and its root-declared submodules were not used for dependency initialization or standalone builds.

## Gate summary

| Repository | Authority | Restore/build | Focused tests | Integrity | Overall |
| --- | --- | --- | --- | --- | --- |
| Builds | Dapr fixture FAIL 28/29 | PASS | PASS 64/64 | PASS | **FAIL** |
| EventStore | PASS | PASS | PASS | PASS | **PASS** |
| Tenants | PASS | PASS | PASS | PASS | **PASS** |
| FrontComposer | PASS | PASS | FAIL 1 governance test after dependency restore | PASS | **FAIL** |
| Folders | PASS | FAIL Release package mode | PASS focused Debug lanes | PASS | **FAIL** |
| Conversations | FAIL | PASS | package-relevant PASS; Conformance FAIL 2/400 | PASS | **FAIL** |
| Projects | PASS | PASS | PASS 1,697/1,697 | PASS | **PASS** |
| Parties | PASS | FAIL NU1506 restore | not run | PASS | **FAIL** |
| Memories | PASS | PASS | PASS | PASS | **PASS** |
| Commons | FAIL | FAIL NU1010 restore | PASS 200/200 unaffected lane | PASS | **FAIL** |
| Timesheets | FAIL | FAIL NU1506 restore | not run | PASS | **FAIL** |
| PolymorphicSerializations | FAIL | FAIL 26 `IDE0065` errors | PASS 15/15 emitted assembly | PASS | **FAIL** |

Signed result: 4/12 standalone rows are fully green at ChatBot baseline `bd652e3c61ebfa0202f6a1fdb696759637a21bca`. The required all-green PASS table does not exist. Per Story 1.1e AC5 and its resumption gate, the final unchanged ChatBot umbrella rerun was not started, the remaining completion subtasks stay unchecked, and story/sprint status stays `in-progress`.

Clearing this gate requires a new authorized ChatBot baseline whose consumer gitlinks and each consumer's direct Builds/dependency pins form a mutually compatible package-authority graph, followed by a full rerun of every changed row. Substituting dependency SHAs, patching a detached checkout, adding local package overrides, weakening analyzers/tests, or changing solution membership would invalidate the evidence contract.
