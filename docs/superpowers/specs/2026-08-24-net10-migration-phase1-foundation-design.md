# Accord.NET Framework: Migration to .NET 10

## Context

This repository is a fork (`igor-zhylin/framework`, upstream `accord-net/framework`)
of the Accord.NET Framework, version 3.8.2-alpha — a large multi-module machine
learning / statistics / computer vision framework for .NET. It currently targets a
mix of `.NET Framework 3.5` (legacy non-SDK-style `.csproj`) and
`netstandard2.0;netstandard1.4;net47` (modern SDK-style `.csproj`, present
alongside the legacy ones as a `(NETStandard)`-suffixed duplicate).

The goal is to migrate the framework to run on **.NET 10**.

## Overall scope decisions

These apply to the whole migration, not just Phase 1:

- **Windows-only / native-dependent modules are out of scope**: `Accord.Video.DirectShow`,
  `Accord.Video.VFW`, `Accord.Audio.DirectSound`, `Accord.Video.Kinect`,
  `Accord.Imaging.WPF`, `Accord.Controls*` (WinForms), `Accord.Video.Ximea`, and
  anything depending on `SharpDX`/`TeRK`. Migration is focused on the
  cross-platform core.
- **Single target framework**: modules are retargeted to `net10.0` only. No
  multi-targeting (`netstandard2.0`, `net47`, etc.) is retained — this fork has
  no external consumers depending on the old TFMs.
- **Tests are part of the definition of done**: each phase's corresponding
  `Unit Tests/Accord.Tests.*` projects are migrated and must pass via
  `dotnet test` before the phase is considered complete.
- **Accord.Imaging depends on `System.Drawing`**, which is Windows-only from
  .NET 6+. It stays in scope (it's part of the "core"), but will be ported off
  `System.Drawing` onto `SixLabors.ImageSharp` in its own later phase, since
  that's a substantial rewrite of public API surface (methods take/return
  `Bitmap` today).

## Phased decomposition

The module dependency graph (via `ProjectReference`) naturally layers as:

```
Layer 0: Accord.Core
Layer 1: Accord.Math.Core, Accord.IO           (depend only on Core)
Layer 2: Accord.Math, Accord.Fuzzy, Accord.DataSets
Layer 3: Accord.Statistics, Accord.Genetic, Accord.Text
Layer 4: Accord.MachineLearning, Accord.Neuro   (top of the graph)
Layer 5: Accord.Imaging                         (ImageSharp rewrite, separate spec)
```

Proposed phases, each its own spec → plan → implementation cycle:

1. **Foundation** (this spec) — `Accord.Core`, `Accord.Math.Core`, `Accord.Math`,
   `Accord.IO` + their tests → `net10.0`.
2. **Numeric core** — `Accord.Fuzzy`, `Accord.DataSets` (`Accord.Math` was pulled
   forward into Phase 1) + tests.
3. **Statistics/Genetic/Text** — `Accord.Statistics`, `Accord.Genetic`,
   `Accord.Text` + tests.
4. **Learning** — `Accord.MachineLearning`, `Accord.Neuro` + tests.
5. **Imaging rewrite** — `Accord.Imaging`: `System.Drawing` → `ImageSharp` +
   tests. Largest, riskiest phase; gets its own design pass when reached.

## Phase 1 — Foundation

### Scope

Libraries: `Accord.Core`, `Accord.Math.Core`, `Accord.Math`, `Accord.IO`.

`Accord.Math` is pulled into Phase 1 (rather than Phase 2, despite being a
"Layer 2" module) because both `Accord.Tests.Core` and `Accord.Tests.IO`
carry a `ProjectReference` to it — those test projects won't build on net10.0
otherwise. Since it's being touched anyway, its own dedicated test project
(`Accord.Tests.Math`) is migrated and run in this phase too.

Test projects: `Accord.Tests.Core`, `Accord.Tests.Math`, `Accord.Tests.IO`.

Out of scope for Phase 1: `Accord.Fuzzy`, `Accord.DataSets`, and everything
above Layer 2 — those don't block Phase 1 tests and move to Phase 2+.

### Target-framework mechanics

Each module currently has two `.csproj` files:
- `X.csproj` — legacy, non-SDK-style, targets `.NET Framework 3.5`.
- `X (NETStandard).csproj` — SDK-style (`Sdk="Microsoft.NET.Sdk"`), multi-targets
  `netstandard2.0;netstandard1.4;net47` (or similar).

**Correction after dependency check (during plan-writing):** modules outside
Phase 1 scope (`Accord.Statistics`, `Accord.Fuzzy`, `Accord.DataSets`,
`Accord.Genetic`, `Accord.Text`, `Accord.MachineLearning`, `Accord.Neuro`,
`Accord.Imaging`, ...) hold `ProjectReference`s to the exact current paths of
the four Phase-1 `(NETStandard)` csproj files (e.g.
`..\Accord.Core\Accord.Core (NETStandard).csproj`). Renaming or deleting those
files now — before those modules are themselves migrated — would break their
builds and any solution file that includes both migrated and not-yet-migrated
projects. So for each of the four Phase-1 modules (and their three test
projects), Phase 1 makes **additive, in-place** changes only:

- Add `net10.0` to the existing SDK-style `X (NETStandard).csproj` as the
  **only** `<TargetFramework>` (singular element, replacing the
  `<TargetFrameworks>` list) — the file keeps its current name/path so
  not-yet-migrated modules' `ProjectReference`s keep resolving *by path*.

  **Further correction (discovered while executing Task 1 of the plan, user
  ruling recorded there):** keeping the file path stable is necessary but
  not sufficient. NuGet's project-reference compatibility check (`NU1201`)
  rejects a `ProjectReference` from a `netstandard2.0`/`netstandard1.4`/`net47`-targeting
  project to a project that no longer offers any of those TFMs — verified
  empirically: retargeting `Accord.Core` to `net10.0`-only broke
  `Accord.Statistics (NETStandard).csproj`'s build (`dotnet build` → 9×
  `NU1201`), and transitively every other not-yet-migrated Phase 2-5
  NETStandard-track module. Two fixes exist — multi-target Phase 1's
  libraries as `net10.0;netstandard2.0;netstandard1.4;net47` during the
  transition (fully non-disruptive, more implementation work), or accept the
  breakage and let each later phase fix its own build when it migrates
  (simpler now, leaves the repo's non-Phase-1 modules non-buildable for a
  while). The user chose to **accept the breakage** rather than
  multi-target. Phase 1 proceeds with the single-`net10.0`-target approach
  as originally written in this section; Phase 2-5 modules will not build
  again until their own migration phases land.
- The legacy non-SDK-style `X.csproj` (net35) is left untouched for now —
  neither deleted nor referenced by anything new. It becomes dead weight to
  be removed in a final repo-wide cleanup phase once every module is on
  net10.0 and the old TFM track has no remaining consumers.
- Collapse each module's TFM-conditioned `<Choose>/<When>/<Otherwise>` blocks:
  since there's now exactly one `TargetFramework`, replace the whole `Choose`
  with the unconditional content of whichever branch net10.0 needs (see
  per-package decisions below) — this also means `#if`/`#else` conditional
  compilation in `.cs` files does not need touching now; feature-matrix
  `DefineConstants` (below) keep it on the same code paths it already
  exercises today under `netstandard2.0`.
- In the shared MSBuild props (`Sources/Accord.NET (NETStandard).targets`,
  `Unit Tests/Accord.Tests (NETStandard).targets`), **add** a new
  `net10.0`-conditioned `PropertyGroup` alongside the existing ones — do not
  remove or edit the `netstandard2.0`/`netstandard1.4`/`net47`/`netcoreapp*`
  branches, which Phase 2-5 modules still depend on until they're migrated.
  The new `net10.0` branch defines the same constants the `netstandard2.0`
  branch already does (`NETSTANDARD;NO_EXCEL;NO_CODE_PROVIDER;NO_BITMAP` for
  library projects) so Phase-1 code takes the exact same already-proven
  `#if NETSTANDARD` branches it does today. The test-targets file's
  `RuntimeIdentifier` pinning to `win7-x64` (via a `TargetFramework !=
  netcoreapp1.1/2.0` condition) must explicitly exclude `net10.0` too, since
  that pin is inappropriate for cross-platform test execution.
- No solution file changes needed — file paths/names are unchanged, so
  existing `.sln` entries keep resolving.
- Package references: bump/drop as needed for net10.0 compatibility
  (`System.ComponentModel.Annotations` is droppable — `System.ComponentModel.DataAnnotations`
  is in the shared framework now; `System.Threading.Thread`/`System.Threading.Tasks`/`System.IO.Compression`
  are droppable — all in the BCL for net10.0, no package or `<Reference>`
  needed). `SharpZipLib`/`SharpZipLib.NETStandard` in `Accord.IO` is dropped
  entirely for the `net10.0` branch — confirmed by grep that nothing in
  `Accord.IO` actually has a real `using ICSharpCode.SharpZipLib...`
  directive; the one file whose doc-comment mentions it
  (`Compression/LzwInputStream.cs`) is a self-contained port that doesn't
  depend on the package. The reference was already dead weight.
- Keep existing build settings: `TreatWarningsAsErrors=True`,
  `AllowUnsafeBlocks=True`, `SignAssembly=False`. Expect and fix new warnings
  surfaced by a modern Roslyn/C# compiler version that weren't caught before.

### Serializer replacement

`Accord.IO.Serializer` (despite the namespace, the type lives in
`Sources/Accord.Core/Serializer.cs`) is the framework-wide model save/load API.
It is implemented entirely on
`System.Runtime.Serialization.Formatters.Binary.BinaryFormatter`, which is
non-functional starting .NET 9 (the runtime no longer supports
`Serialize`/`Deserialize`).

Decision: **reimplement on `System.Text.Json`**, in Phase 1, now — not deferred.

- No new external dependency; `System.Text.Json` is part of the net10.0 BCL.
- Public API surface is preserved: `Serializer.Save<T>(obj, path)`,
  `Serializer.Save<T>(obj, stream, compression)`, `Serializer.Load<T>(path)`,
  `Serializer.Load<T>(stream, compression)`, byte-array overloads,
  `DeepClone<T>`, and `SerializerCompression.GZip` (still implemented as a
  wrapping `GZipStream` around the JSON payload).
- **File format changes.** Old `.accord`/`.bin` files saved by the
  `BinaryFormatter`-based serializer will not load. Already accepted — no
  backward file-format compatibility is required for this fork.
- The vast majority of `[Serializable]`-marked types across these four
  modules (ranges, points, distance metrics, matrix decompositions, priority
  queues, trees) are plain data classes with public or straightforwardly
  accessible fields/properties — these map to `System.Text.Json` with little
  or no per-type adapter code.
- Two known special cases, handled individually as encountered:
  - `TwoWayDictionary` and `ParallelLearningBase` use the classic
    `[OnDeserialized]` callback attribute (`System.Runtime.Serialization`),
    which `System.Text.Json` does not invoke automatically — these need to
    implement `IJsonOnDeserialized` (or equivalent) instead.
  - `LineSearchFailedException` has a custom `ISerializable.GetObjectData`
    override. Exception serialization is not a priority path through
    `Serializer`; best-effort, not blocking.
- `Serializer.GetBinder`/`GetSurrogate` (the `SerializationBinderAttribute` /
  `SurrogateSelectorAttribute` extension points used for cross-version model
  migration) are `BinaryFormatter`-specific concepts with no direct
  `System.Text.Json` equivalent. Confirmed (via grep) that no type in these
  four modules is decorated with either attribute — they have no consumers
  outside their own definitions and `Serializer.cs` itself. Since no legacy
  files need to load either, this extension mechanism (the attributes,
  `GetBinder`, `GetSurrogate`) is deleted outright rather than ported or kept
  dead.
- `SerializerTest.cs`: the 6 active tests are adapted to assert against the
  new `System.Text.Json`-based round-trip instead of being marked `Ignore`.

### Testing

- Consolidate `Accord.Tests.Core`, `Accord.Tests.Math`, `Accord.Tests.IO`
  csproj the same way (single `net10.0` SDK-style project).
- Bump test tooling off 2017-era versions: `Microsoft.NET.Test.Sdk` (currently
  `15.3.0`), `NUnit` (currently `3.8.1`), `NUnit.Console` (currently `3.7.0`)
  to current net10.0-compatible versions.
- `dotnet test` across all three projects is green — this is the completion
  gate for Phase 1.

### Out of scope / explicitly deferred

- `Accord.Fuzzy`, `Accord.DataSets` and everything in Layers 3-5 → later
  phases.
- Restoring backward file-format compatibility for `Serializer` with old
  `BinaryFormatter`-saved files → not planned at all (accepted trade-off).
- Any Windows-only / native-dependent module.
- Deleting the legacy net35 `.csproj` files and dropping the `(NETStandard)`
  suffix from the four Phase-1 modules → deferred to a final repo-wide
  cleanup phase, after every module is migrated (see "Correction after
  dependency check" above — doing it now would break not-yet-migrated
  modules' `ProjectReference`s).
