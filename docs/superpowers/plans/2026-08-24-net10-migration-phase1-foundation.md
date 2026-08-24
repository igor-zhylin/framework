# .NET 10 Migration — Phase 1 (Foundation) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retarget `Accord.Core`, `Accord.Math.Core`, `Accord.Math`, and `Accord.IO` (plus their NUnit test projects) to `net10.0`, replacing the `BinaryFormatter`-based `Serializer` with a `System.Text.Json`-based one.

**Architecture:** Each of the four libraries already has a modern SDK-style `X (NETStandard).csproj` alongside a legacy non-SDK-style `X.csproj` (net35). This plan edits the `(NETStandard)` csproj **in place** — same file name/path, `<TargetFrameworks>` list collapsed to a single `<TargetFramework>net10.0</TargetFramework>` — so `ProjectReference`s from not-yet-migrated modules (Statistics, MachineLearning, Imaging, ...) keep resolving *by path*. Shared MSBuild props files get a new `net10.0`-conditioned `PropertyGroup` added *alongside* their existing TFM branches (never replacing them). Legacy `X.csproj` (net35) files and the `(NETStandard)` suffix are left untouched — cleanup is a final phase after every module is migrated.

**⚠️ Known, accepted trade-off (discovered during Task 1, ruled on by the user):** collapsing `Accord.Core`'s `<TargetFrameworks>` to `net10.0`-only, even in place, breaks every not-yet-migrated Phase 2-5 module's build — NuGet's `NU1201` rejects a `ProjectReference` from a `netstandard2.0`/`netstandard1.4`/`net47`-targeting project to a project that no longer offers any of those TFMs, regardless of file path. This was verified empirically against the real repo (`dotnet build "Sources/Accord.Statistics/Accord.Statistics (NETStandard).csproj"` → 9× `NU1201`). The user chose to **accept this breakage** rather than multi-target Phase 1's libraries during the transition: Phase 2-5 modules will not build again until their own migration phases land. This plan's remaining tasks proceed unchanged (single `net10.0` target, as originally written) — see the SDD ledger's "accept temporary breakage" ruling for the full reasoning.

**Tech Stack:** .NET 10 SDK (already installed: `dotnet 10.0.111`), MSBuild SDK-style `.csproj`, `System.Text.Json` (BCL, no new package), NUnit 4 + NUnit3TestAdapter + Microsoft.NET.Test.Sdk via `dotnet test`.

**Spec:** `docs/superpowers/specs/2026-08-24-net10-migration-phase1-foundation-design.md`

## Global Constraints

- Target framework for touched projects: `net10.0` only (single `<TargetFramework>`, not `<TargetFrameworks>`). This is now known to temporarily break not-yet-migrated modules' builds — an accepted trade-off, not a defect to fix within this plan (see the ⚠️ note above and the ledger).
- Do not rename or delete any `(NETStandard)` csproj file, and do not delete any legacy net35 `.csproj` file — only edit `<TargetFrameworks>`/content in place. (Spec: "Correction after dependency check".)
- Do not edit or remove any existing TFM-conditioned `PropertyGroup` in `Sources/Accord.NET (NETStandard).targets` or `Unit Tests/Accord.Tests (NETStandard).targets` — only add new `net10.0`-conditioned ones.
- `TreatWarningsAsErrors=True` stays on for library projects (inherited from `Sources/Accord.NET (NETStandard).targets`) — fix any warning a task's build surfaces, don't suppress it.
- `Serializer.Save`/`Load`/`DeepClone` public method signatures (names, generic constraints, parameter lists, default values) must stay the same as today — only the implementation changes. File format compatibility with old `BinaryFormatter`-saved files is explicitly NOT required.
- No new external NuGet dependency for the `Serializer` rewrite — `System.Text.Json` is part of the net10.0 BCL.
- Completion gate: `dotnet test` is green for `Accord.Tests.Core`, `Accord.Tests.Math`, `Accord.Tests.IO` (targeting their `(NETStandard).csproj`, now net10.0-only).

---

### Task 1: `Accord.Core` → net10.0, `Serializer` → `System.Text.Json`

**Files:**
- Modify: `Sources/Accord.NET (NETStandard).targets`
- Modify: `Sources/Accord.Core/Accord.Core (NETStandard).csproj`
- Modify: `Sources/Accord.Core/Serializer.cs`
- Delete: `Sources/Accord.Core/Attributes/SerializationBinderAttribute.cs`
- Delete: `Sources/Accord.Core/Attributes/SurrogateSelectorAttribute.cs`

**Interfaces:**
- Produces: `Sources/Accord.NET (NETStandard).targets` now defines `NETSTANDARD;NO_EXCEL;NO_CODE_PROVIDER;NO_BITMAP` for `TargetFramework=='net10.0'` — every later task in this plan relies on this being present before adding `net10.0` to any other library csproj that imports this targets file.
- Produces: `Accord.IO.Serializer.Save<T>(this T obj, ...)` / `Serializer.Load<T>(...)` / `Serializer.DeepClone<T>(this T obj)` — same public signatures as before, backed by `System.Text.Json`. Later tasks (5-7) don't call these directly, but `Accord.Tests.Core`'s `SerializerTest.cs` (unmodified) exercises them.

- [ ] **Step 1: Add the `net10.0` feature-matrix branch to the shared library targets file**

  In `Sources/Accord.NET (NETStandard).targets`, find this existing block:

  ```xml
  <!-- Feature matrix -->
  <PropertyGroup Condition="'$(TargetFramework)'=='netstandard2.0'
                         OR '$(TargetFramework)'=='netstandard1.4'">
    <DefineConstants>NETSTANDARD;NO_EXCEL;NO_CODE_PROVIDER;NO_BITMAP$(DefineConstants)</DefineConstants>
  </PropertyGroup>
  ```

  Immediately after it (still inside the "Feature matrix" section, before the `netstandard1.4`-only block), insert:

  ```xml
  <PropertyGroup Condition="'$(TargetFramework)'=='net10.0'">
    <DefineConstants>NETSTANDARD;NO_EXCEL;NO_CODE_PROVIDER;NO_BITMAP;$(DefineConstants)</DefineConstants>
  </PropertyGroup>
  ```

  Do not touch any other `PropertyGroup` in this file — the `netstandard2.0`/`netstandard1.4`/`net35`/`net40`/`net45`/`net46`/`net462` branches must stay exactly as they are; Phase 2-5 modules still use them.

- [ ] **Step 2: Retarget `Accord.Core (NETStandard).csproj` to net10.0 and drop its now-unnecessary `Choose` block**

  Replace the full contents of `Sources/Accord.Core/Accord.Core (NETStandard).csproj` with:

  ```xml
  ﻿<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
      <AssemblyName>Accord</AssemblyName>
      <RootNamespace>Accord</RootNamespace>
      <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <Import Project="$(SolutionDir)Accord.NET (NETStandard).targets" />

  </Project>
  ```

  (The old `Choose`/`When`/`Otherwise` block picked between the `System.ComponentModel.Annotations` NuGet package and a `System.ComponentModel.DataAnnotations` framework `<Reference>`. On net10.0, `System.ComponentModel.DataAnnotations` is part of the shared framework and needs neither — verified by building a throwaway net10.0 project referencing `System.ComponentModel.DataAnnotations.RangeAttribute` with zero `PackageReference`/`Reference` entries; it built and ran without error.)

- [ ] **Step 3: Build and confirm it fails on `Serializer.cs` with `BinaryFormatter` obsolete-as-error**

  Run: `dotnet build "Sources/Accord.Core/Accord.Core (NETStandard).csproj"`

  Expected: **FAIL**, with an error on each `new BinaryFormatter()` line in `Serializer.cs`, e.g.:
  ```
  error SYSLIB0011: 'BinaryFormatter' is obsolete: 'BinaryFormatter serialization is obsolete and should not be used.' ...
  ```
  (Confirmed by direct probe: compiling a `net10.0` project that constructs `new BinaryFormatter()` fails with this exact diagnostic — it is a compile-time error on net10.0, not just a runtime one, because the BCL marks the type `[Obsolete(..., error: true)]`.) This is expected — it's why Step 4 exists.

- [ ] **Step 4: Replace `Sources/Accord.Core/Serializer.cs` with a `System.Text.Json`-based implementation**

  Replace the full contents of `Sources/Accord.Core/Serializer.cs` with:

  ```csharp
  // Accord Core Library
  // The Accord.NET Framework
  // http://accord-framework.net
  //
  // Copyright © César Souza, 2009-2017
  // cesarsouza at gmail.com
  //
  //    This library is free software; you can redistribute it and/or
  //    modify it under the terms of the GNU Lesser General Public
  //    License as published by the Free Software Foundation; either
  //    version 2.1 of the License, or (at your option) any later version.
  //
  //    This library is distributed in the hope that it will be useful,
  //    but WITHOUT ANY WARRANTY; without even the implied warranty of
  //    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
  //    Lesser General Public License for more details.
  //
  //    You should have received a copy of the GNU Lesser General Public
  //    License along with this library; if not, write to the Free Software
  //    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
  //

  namespace Accord.IO
  {
      using System;
      using System.IO;
      using System.IO.Compression;
      using System.Text.Json;

      /// <summary>
      ///   Model serializer. Can be used to serialize and deserialize (i.e. save and 
      ///   load) models from the framework to and from the disk and other streams.
      /// </summary>
      /// 
      /// <remarks>
      ///   This class serializes objects using <see cref="System.Text.Json"/>. The file
      ///   format is not compatible with files saved by versions of this class that used
      ///   <c>BinaryFormatter</c> (removed from the .NET runtime as of .NET 9).
      /// </remarks>
      /// 
      /// <example>
      /// <para>
      ///   The first example shows the simplest way to use the serializer to persist objects:</para>
      ///   <code source="Unit Tests\Accord.Tests.Core\SerializerTest.cs" region="doc_simple" />
      ///   
      /// <para>
      ///   The second example shows the same, but using compression:</para>
      ///   <code source="Unit Tests\Accord.Tests.Core\SerializerTest.cs" region="doc_compression" />
      /// </example>
      /// 
      public static class Serializer
      {
          const SerializerCompression DEFAULT_COMPRESSION = SerializerCompression.None;

          private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
          {
              IncludeFields = true,
          };

          private static SerializerCompression ParseCompression(string path)
          {
              string ext = Path.GetExtension(path);
              if (ext == ".gz")
                  return SerializerCompression.GZip;
              return SerializerCompression.None;
          }

          /// <summary>
          ///   Saves an object to a stream.
          /// </summary>
          /// 
          /// <param name="obj">The object to be serialized.</param>
          /// <param name="stream">The stream to which the object is to be serialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// 
          public static void Save<T>(this T obj, Stream stream, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              if (compression == SerializerCompression.GZip)
              {
                  using (var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true))
                      JsonSerializer.Serialize(gzip, obj, Options);
              }
              else if (compression == SerializerCompression.None)
              {
                  JsonSerializer.Serialize(stream, obj, Options);
              }
              else
              {
                  throw new ArgumentException("compression");
              }
          }

          /// <summary>
          ///   Saves an object to a stream.
          /// </summary>
          /// 
          /// <param name="obj">The object to be serialized.</param>
          /// <param name="path">The path to the file to which the object is to be serialized.</param>
          /// 
          public static void Save<T>(this T obj, string path)
          {
              Save(obj, path, ParseCompression(path));
          }

          /// <summary>
          ///   Saves an object to a stream.
          /// </summary>
          /// 
          /// <param name="obj">The object to be serialized.</param>
          /// <param name="path">The path to the file to which the object is to be serialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// 
          public static void Save<T>(this T obj, string path, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              path = Path.GetFullPath(path);

              var dir = Path.GetDirectoryName(path);
              if (!Directory.Exists(dir))
                  Directory.CreateDirectory(dir);

              if (compression == SerializerCompression.GZip)
              {
                  if (!path.EndsWith(".gz"))
                      path = path + ".gz";
              }

              using (var fs = new FileStream(path, FileMode.Create))
              {
                  Save(obj, fs, compression);
              }
          }

          /// <summary>
          ///   Saves an object to a stream, represented as an array of bytes.
          /// </summary>
          /// 
          /// <param name="obj">The object to be serialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// 
          public static byte[] Save<T>(this T obj, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              byte[] bytes;
              Save(obj, out bytes, compression);
              return bytes;
          }

          /// <summary>
          ///   Saves an object to a stream.
          /// </summary>
          /// 
          /// <param name="obj">The object to be serialized.</param>
          /// <param name="bytes">The sequence of bytes to which the object has been serialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// 
          public static void Save<T>(this T obj, out byte[] bytes, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              using (var fs = new MemoryStream())
              {
                  Save(obj, fs, compression);
                  fs.Seek(0, SeekOrigin.Begin);
                  bytes = fs.ToArray();
              }
          }

          /// <summary>
          ///   Loads an object from a stream.
          /// </summary>
          /// 
          /// <param name="stream">The stream from which the object is to be deserialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// 
          /// <returns>The deserialized machine.</returns>
          /// 
          public static T Load<T>(Stream stream, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              if (compression == SerializerCompression.GZip)
              {
                  using (var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true))
                      return JsonSerializer.Deserialize<T>(gzip, Options);
              }
              else if (compression == SerializerCompression.None)
              {
                  return JsonSerializer.Deserialize<T>(stream, Options);
              }
              else
              {
                  throw new ArgumentException("compression");
              }
          }

          /// <summary>
          ///   Loads an object from a file.
          /// </summary>
          /// 
          /// <param name="path">The path to the file from which the object is to be deserialized.</param>
          /// 
          /// <returns>The deserialized object.</returns>
          /// 
          public static T Load<T>(string path)
          {
              return Load<T>(path, ParseCompression(path));
          }

          /// <summary>
          ///   Loads an object from a file.
          /// </summary>
          /// 
          /// <param name="path">The path to the file from which the object is to be deserialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// 
          /// <returns>The deserialized object.</returns>
          /// 
          public static T Load<T>(string path, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              path = Path.GetFullPath(path);
              using (var fs = new FileStream(path, FileMode.Open))
                  return Load<T>(fs, compression);
          }

          /// <summary>
          ///   Loads an object from a stream, represented as an array of bytes.
          /// </summary>
          /// 
          /// <param name="bytes">The byte stream containing the object to be deserialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// 
          /// <returns>The deserialized object.</returns>
          /// 
          public static T Load<T>(byte[] bytes, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              using (var fs = new MemoryStream(bytes, false))
                  return Load<T>(fs, compression);
          }

          /// <summary>
          ///   Loads an object from a stream.
          /// </summary>
          /// 
          /// <param name="stream">The stream from which the object is to be deserialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// <param name="value">The object to be read. This parameter can be used to avoid the
          ///   need of specifying a generic argument to this function.</param>
          /// 
          /// <returns>The deserialized machine.</returns>
          /// 
          public static T Load<T>(Stream stream, out T value, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              return value = Load<T>(stream, compression);
          }

          /// <summary>
          ///   Loads an object from a file.
          /// </summary>
          /// 
          /// <param name="path">The path to the file from which the object is to be deserialized.</param>
          /// <param name="value">The object to be read. This parameter can be used to avoid the
          ///   need of specifying a generic argument to this function.</param>
          /// 
          /// <returns>The deserialized object.</returns>
          /// 
          public static T Load<T>(string path, out T value)
          {
              return Load<T>(path, out value, ParseCompression(path));
          }

          /// <summary>
          ///   Loads an object from a file.
          /// </summary>
          /// 
          /// <param name="path">The path to the file from which the object is to be deserialized.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// <param name="value">The object to be read. This parameter can be used to avoid the
          ///   need of specifying a generic argument to this function.</param>
          /// 
          /// <returns>The deserialized object.</returns>
          /// 
          public static T Load<T>(string path, out T value, SerializerCompression compression)
          {
              return value = Load<T>(path, compression);
          }

          /// <summary>
          ///   Loads an object from a stream, represented as an array of bytes.
          /// </summary>
          /// 
          /// <param name="bytes">The byte stream containing the object to be deserialized.</param>
          /// <param name="value">The object to be read. This parameter can be used to avoid the
          ///   need of specifying a generic argument to this function.</param>
          /// <param name="compression">The type of compression to use. Default is None.</param>
          /// 
          /// <returns>The deserialized object.</returns>
          /// 
          public static T Load<T>(byte[] bytes, out T value, SerializerCompression compression = DEFAULT_COMPRESSION)
          {
              return value = Load<T>(bytes, compression);
          }

          /// <summary>
          ///   Performs a deep copy of an object by serializing and deserializing it.
          /// </summary>
          /// 
          /// <typeparam name="T">The type of the model to be copied.</typeparam>
          /// <param name="obj">The object.</param>
          /// 
          /// <returns>A deep copy of the given object.</returns>
          /// 
          public static T DeepClone<T>(this T obj)
          {
              return Load<T>(Save<T>(obj));
          }
      }
  }
  ```

  Notes on what was dropped versus the old `BinaryFormatter`-based file, and why it's safe:
  - The `Save`/`Load` overloads that took an explicit `BinaryFormatter formatter` parameter are gone — confirmed via repo-wide grep that nothing calls `Serializer.Save(obj, someFormatter, ...)` or `Serializer.Load(stream, someFormatter, ...)` anywhere (other files that use `BinaryFormatter` construct and use it directly, bypassing `Serializer` entirely).
  - `GetBinder`/`GetSurrogate` (the `SerializationBinderAttribute`/`SurrogateSelectorAttribute` extension points) are gone — confirmed via grep that no type anywhere in the repo carries either attribute; their only references were their own definitions and `Serializer.cs` itself.
  - The `GetValue<T>(this SerializationInfo info, ...)` extension method is gone — confirmed via grep it has zero callers repo-wide (the only other `GetValue<T>` in the codebase, in `Accord.Math/IO/Mat/MatNode.cs`, is an unrelated same-named instance method, not this extension).
  - The `AppDomain.CurrentDomain.AssemblyResolve` subscribe/unsubscribe dance and the `lock (lockObj)` around `Load` are gone — both existed to support `BinaryFormatter`'s assembly-qualified type resolution during deserialization, which `System.Text.Json` doesn't need (it deserializes into the caller-specified `T` directly; `JsonSerializer`'s static methods are inherently safe to call concurrently as long as the shared `JsonSerializerOptions` instance isn't mutated after first use, which it never is here).

  This design was verified empirically before writing this plan: a throwaway console app with this exact `Save`/`Load` logic correctly round-tripped a `DoubleRange`-shaped struct (private fields + public get/set properties), a `Dictionary<int, string>`, a `byte[]`, and a GZip-compressed payload.

- [ ] **Step 5: Delete the two now-dead attribute classes**

  Delete `Sources/Accord.Core/Attributes/SerializationBinderAttribute.cs`.
  Delete `Sources/Accord.Core/Attributes/SurrogateSelectorAttribute.cs`.

  (Both were `BinaryFormatter`-era extension points for `Serializer.GetBinder`/`GetSurrogate`, which no longer exist after Step 4. Confirmed via grep — see Step 4 notes — that nothing else in the repo references either attribute type.)

- [ ] **Step 6: Build and confirm success**

  Run: `dotnet build "Sources/Accord.Core/Accord.Core (NETStandard).csproj"`

  Expected: **Build succeeded**, 0 Warning(s), 0 Error(s).

  If new warnings appear (e.g. from a newer Roslyn/C# version catching something the old toolchain didn't), fix them in place — don't suppress. Do not proceed to Step 7 until this is a clean build.

- [ ] **Step 7: Commit**

  ```bash
  git add "Sources/Accord.NET (NETStandard).targets" "Sources/Accord.Core/Accord.Core (NETStandard).csproj" "Sources/Accord.Core/Serializer.cs"
  git rm "Sources/Accord.Core/Attributes/SerializationBinderAttribute.cs" "Sources/Accord.Core/Attributes/SurrogateSelectorAttribute.cs"
  git commit -m "Retarget Accord.Core to net10.0, replace BinaryFormatter Serializer with System.Text.Json"
  ```

  (Per this repo's working rules in `CLAUDE.md`: do not add a `Co-Authored-By: Claude` trailer.)

---

### Task 2: `Accord.Math.Core` → net10.0

**Files:**
- Modify: `Sources/Accord.Math.Core/Accord.Math.Core (NETStandard).csproj`

**Interfaces:**
- Consumes: the `net10.0` `PropertyGroup` added to `Sources/Accord.NET (NETStandard).targets` in Task 1, Step 1.
- Consumes: `Sources/Accord.Core/Accord.Core (NETStandard).csproj` now targets `net10.0` (Task 1) — a `ProjectReference` from a `net10.0` project to a `net10.0` project.

- [ ] **Step 1: Retarget the csproj**

  Replace the full contents of `Sources/Accord.Math.Core/Accord.Math.Core (NETStandard).csproj` with:

  ```xml
  ﻿<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
      <AssemblyName>Accord.Math.Core</AssemblyName>
      <RootNamespace>Accord.Math</RootNamespace>
      <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <Import Project="$(SolutionDir)Accord.NET (NETStandard).targets" />

    <ItemGroup>
      <ProjectReference Include="..\Accord.Core\Accord.Core (NETStandard).csproj" />
    </ItemGroup>

    <ItemGroup>
      <None Update="Matrix.Comparisons.tt">
        <Generator>TextTemplatingFileGenerator</Generator>
        <LastGenOutput>Matrix.Comparisons.Generated.cs</LastGenOutput>
        <LastOutputs></LastOutputs>
      </None>
      <None Update="Matrix.Elementwise.tt">
        <Generator>TextTemplatingFileGenerator</Generator>
        <LastGenOutput>Matrix.Elementwise.Generated.cs</LastGenOutput>
        <LastOutputs></LastOutputs>
      </None>
      <None Update="Matrix.Elementwise2.tt">
        <Generator>TextTemplatingFileGenerator</Generator>
        <LastGenOutput>Matrix.Elementwise2.txt</LastGenOutput>
        <LastOutputs>
          .\Matrix.Add.Generated.cs
          .\Matrix.Divide.Generated.cs
          .\Matrix.Multiply.Generated.cs
          .\Matrix.Subtract.Generated.cs
        </LastOutputs>
      </None>
      <None Update="Matrix.Elementwise3.tt">
        <Generator>TextTemplatingFileGenerator</Generator>
        <LastGenOutput>Matrix.Elementwise3.txt</LastGenOutput>
      </None>
    </ItemGroup>

    <ItemGroup>
      <Service Include="{508349b6-6b84-4df5-91f0-309beebad82d}" />
    </ItemGroup>

  </Project>
  ```

  (Only the `PropertyGroup`'s `<TargetFrameworks>netstandard2.0;netstandard1.4;net47</TargetFrameworks>` → `<TargetFramework>net10.0</TargetFramework>` changes; everything else — `ProjectReference`, the T4 `.tt`/generated-file metadata, the `Service` entry — is carried forward unchanged. The `.tt` files are pre-generated; their `.Generated.cs` outputs are already checked into the repo and compile as regular source, so no T4 tooling is required at build time.)

- [ ] **Step 2: Build and confirm success**

  Run: `dotnet build "Sources/Accord.Math.Core/Accord.Math.Core (NETStandard).csproj"`

  Expected: **Build succeeded**, 0 Warning(s), 0 Error(s). Fix any warning in place before proceeding.

- [ ] **Step 3: Commit**

  ```bash
  git add "Sources/Accord.Math.Core/Accord.Math.Core (NETStandard).csproj"
  git commit -m "Retarget Accord.Math.Core to net10.0"
  ```

---

### Task 3: `Accord.Math` → net10.0

**Files:**
- Modify: `Sources/Accord.Math/Accord.Math (NETStandard).csproj`

**Interfaces:**
- Consumes: `Accord.Core` and `Accord.Math.Core` now targeting `net10.0` (Tasks 1-2).

- [ ] **Step 1: Retarget the csproj and collapse the TFM-conditioned `Choose` block**

  In `Sources/Accord.Math/Accord.Math (NETStandard).csproj`:
  1. Change the `PropertyGroup`'s `<TargetFrameworks>netstandard2.0;netstandard1.4;net47</TargetFrameworks>` to `<TargetFramework>net10.0</TargetFramework>`.
  2. Delete this entire `Choose` block:
     ```xml
     <Choose>
       <When Condition="'$(TargetFramework)' == 'netstandard1.4'">
         <ItemGroup>
           <PackageReference Include="System.Threading.Thread" Version="4.3.0" />
           <PackageReference Include="System.Threading.Tasks" Version="4.3.0" />
         </ItemGroup>
       </When>
       <When Condition="'$(TargetFramework)' == 'netstandard2.0'">
         <ItemGroup>
           <PackageReference Include="System.Threading.Thread" Version="4.3.0" />
           <PackageReference Include="System.Threading.Tasks" Version="4.3.0" />
         </ItemGroup>
       </When>
       <When Condition="'$(TargetFramework)' == 'net35' OR '$(TargetFramework)' == 'net40'">
       </When>
       <Otherwise>
         <ItemGroup>
           <Reference Include="System.IO.Compression" />
         </ItemGroup>
       </Otherwise>
     </Choose>
     ```
     Do not replace it with anything — on net10.0, `System.Threading.Tasks`, threading primitives, and `System.IO.Compression` are all part of the shared framework, needing neither a `PackageReference` nor a `<Reference>`.
  3. Leave every other `ItemGroup` in the file (the `ProjectReference`s to `Accord.Core`/`Accord.Math.Core`, all the `.tt`/`Generator`/`DependentUpon` metadata for the `Decompositions\*.tt`, `Matrix\*.tt`, `Vector\*.tt`, `Distance.tt`, `Norm.tt` templates) exactly as-is.

- [ ] **Step 2: Build and confirm success**

  Run: `dotnet build "Sources/Accord.Math/Accord.Math (NETStandard).csproj"`

  Expected: **Build succeeded**, 0 Warning(s), 0 Error(s). Fix any warning in place before proceeding.

- [ ] **Step 3: Commit**

  ```bash
  git add "Sources/Accord.Math/Accord.Math (NETStandard).csproj"
  git commit -m "Retarget Accord.Math to net10.0"
  ```

---

### Task 4: `Accord.IO` → net10.0

**Files:**
- Modify: `Sources/Accord.IO/Accord.IO (NETStandard).csproj`

**Interfaces:**
- Consumes: `Accord.Core` now targeting `net10.0` (Task 1).

- [ ] **Step 1: Retarget the csproj and drop the unused SharpZipLib package reference**

  Replace the full contents of `Sources/Accord.IO/Accord.IO (NETStandard).csproj` with:

  ```xml
  ﻿<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
      <AssemblyName>Accord.IO</AssemblyName>
      <RootNamespace>Accord.IO</RootNamespace>
      <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <Import Project="$(SolutionDir)Accord.NET (NETStandard).targets" />

    <ItemGroup>
      <Compile Remove="Extensions.Matrix.cs" />
      <Compile Remove="Extensions.Table.cs" />
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include="..\Accord.Core\Accord.Core (NETStandard).csproj" />
    </ItemGroup>

  </Project>
  ```

  (Drops the `Choose`/`When`/`Otherwise` block that picked between `SharpZipLib.NETStandard` and `SharpZipLib` — confirmed via grep that no file in `Accord.IO` has a real `using ICSharpCode.SharpZipLib...` directive or any `ICSharpCode.*` type usage; the only mentions are in a doc-comment in `Compression/LzwInputStream.cs`, which is a self-contained port that doesn't depend on the package. The `Compile Remove` for `Extensions.Matrix.cs`/`Extensions.Table.cs` — the two files that do use `BinaryFormatter` directly — is unchanged from today; they were already excluded from this build and stay excluded.)

- [ ] **Step 2: Build and confirm success**

  Run: `dotnet build "Sources/Accord.IO/Accord.IO (NETStandard).csproj"`

  Expected: **Build succeeded**, 0 Warning(s), 0 Error(s). Fix any warning in place before proceeding.

- [ ] **Step 3: Commit**

  ```bash
  git add "Sources/Accord.IO/Accord.IO (NETStandard).csproj"
  git commit -m "Retarget Accord.IO to net10.0, drop unused SharpZipLib reference"
  ```

---

### Task 5: `Accord.Tests.Core` → net10.0 (validates the `Serializer` rewrite)

**Files:**
- Modify: `Unit Tests/Accord.Tests (NETStandard).targets`
- Modify: `Unit Tests/Accord.Tests.Core/Accord.Tests.Core (NETStandard).csproj`

**Interfaces:**
- Consumes: `Accord.Core`, `Accord.Math.Core`, `Accord.Math` all targeting `net10.0` (Tasks 1-3).
- Produces: `Unit Tests/Accord.Tests (NETStandard).targets` now has a `net10.0`-conditioned `PropertyGroup` and excludes `net10.0` from the `RuntimeIdentifier=win7-x64` pin — Tasks 6-7 rely on this being present.

- [ ] **Step 1: Add net10.0 handling to the shared test targets file**

  In `Unit Tests/Accord.Tests (NETStandard).targets`:

  1. Find this block:
     ```xml
     <PropertyGroup Condition="'$(TargetFramework)'=='netcoreapp2.0'
                            OR '$(TargetFramework)'=='netcoreapp1.1'">
       <DefineConstants>NETCORE;NO_CODE_PROVIDER;NO_EXCEL;NO_BITMAP;$(DefineConstants)</DefineConstants>
     </PropertyGroup>
     ```
     Immediately after it, insert:
     ```xml
     <PropertyGroup Condition="'$(TargetFramework)'=='net10.0'">
       <DefineConstants>NETSTANDARD;NO_CODE_PROVIDER;NO_EXCEL;NO_BITMAP;$(DefineConstants)</DefineConstants>
     </PropertyGroup>
     ```
     (`NETSTANDARD`, not `NETCORE` — matching the constant the *library* projects define for `net10.0` in Task 1, Step 1, so test code guarded by `#if NETSTANDARD` takes the same branch as the libraries it's testing.)

  2. Find this block:
     ```xml
     <PropertyGroup Condition="'$(TargetFramework)'!='netcoreapp1.1' 
                           AND '$(TargetFramework)'!='netcoreapp2.0'">
       <RuntimeIdentifier>win7-x64</RuntimeIdentifier>
     </PropertyGroup>
     ```
     Replace its condition so `net10.0` is also excluded from the Windows RID pin:
     ```xml
     <PropertyGroup Condition="'$(TargetFramework)'!='netcoreapp1.1' 
                           AND '$(TargetFramework)'!='netcoreapp2.0'
                           AND '$(TargetFramework)'!='net10.0'">
       <RuntimeIdentifier>win7-x64</RuntimeIdentifier>
     </PropertyGroup>
     ```
     (This is the one existing `PropertyGroup` this plan edits rather than only adding alongside — it's an *exclusion* list, and `net10.0` must join `netcoreapp1.1`/`netcoreapp2.0` in it, or `dotnet test` would try to restore/run as `win7-x64`-specific, breaking cross-platform test execution. Phase 2-5 TFMs — `netcoreapp2.0`, `net47` — are unaffected: their membership in this condition doesn't change.)

  3. Leave every other `PropertyGroup`/`ItemGroup` in this file unchanged.

- [ ] **Step 2: Retarget the csproj and bump test tooling package versions**

  Replace the full contents of `Unit Tests/Accord.Tests.Core/Accord.Tests.Core (NETStandard).csproj` with:

  ```xml
  ﻿<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
      <RootNamespace>Accord.Tests.Core</RootNamespace>
      <AssemblyName>Accord.Tests.Core</AssemblyName>
      <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <Import Project="$(SolutionDir)../Unit Tests/Accord.Tests (NETStandard).targets" />

    <ItemGroup>
      <ProjectReference Include="..\..\Sources\Accord.Math.Core\Accord.Math.Core (NETStandard).csproj" />
      <ProjectReference Include="..\..\Sources\Accord.Math\Accord.Math (NETStandard).csproj" />
    </ItemGroup>

    <ItemGroup>
      <Folder Include="Properties\" />
    </ItemGroup>

    <ItemGroup>
      <PackageReference Update="Microsoft.NET.Test.Sdk" Version="17.12.0" />
      <PackageReference Update="NUnit" Version="4.2.2" />
      <PackageReference Update="NUnit3TestAdapter" Version="4.6.0" />
      <PackageReference Update="NUnit.Console" Version="3.7.0" />
    </ItemGroup>

  </Project>
  ```

  (Added a `PackageReference Update` for `NUnit3TestAdapter` — previously only inherited the shared targets file's baseline `3.8.0`; explicitly pinning `4.6.0` here matches the `NUnit 4.2.2` major version. Left `NUnit.Console` at its current `3.7.0` — confirmed via a throwaway `net10.0` project that this version restores cleanly alongside `NUnit 4.2.2`/`NUnit3TestAdapter 4.6.0`, and it isn't part of the `dotnet test` execution path anyway. The three version numbers for `Microsoft.NET.Test.Sdk`/`NUnit`/`NUnit3TestAdapter` were verified by actually running `dotnet test` against a throwaway `net10.0` NUnit project with these exact versions before writing this plan — it restored and a sample `[Test]` passed.)

- [ ] **Step 3: Run the tests and confirm they pass**

  Run: `dotnet test "Unit Tests/Accord.Tests.Core/Accord.Tests.Core (NETStandard).csproj"`

  Expected: **Passed!** for all tests in the project, including all 6 active tests in `SerializerTest.cs` (`base_test`, `compression_test`, `compression_test_auto_load`, `compression_test_auto_save`, `compression_test_override`, `compression_test_stream`) — these exercise `Serializer.Save`/`Load` on `DoubleRange`, `Dictionary<int, string>`, and `byte[]`, all of which round-trip correctly through the `System.Text.Json`-based implementation from Task 1. (`SerializerTest.large_array_test` stays `[Ignore("this feature has been removed")]` — unrelated pre-existing skip, not part of this migration.)

  `SerializerTest.cs` itself needs **no code changes** — only `Serializer.cs`'s implementation (Task 1) changed; the test file's calls to `Serializer.Save`/`Load` compile and behave the same from the caller's point of view.

  If any other (non-`SerializerTest`) test in this project fails for a reason unrelated to serialization (e.g. a new warning-as-error, a behavior difference in a newer BCL), fix it before proceeding — don't mark it `Ignore` to make the run green.

- [ ] **Step 4: Commit**

  ```bash
  git add "Unit Tests/Accord.Tests (NETStandard).targets" "Unit Tests/Accord.Tests.Core/Accord.Tests.Core (NETStandard).csproj"
  git commit -m "Retarget Accord.Tests.Core to net10.0, bump NUnit test tooling"
  ```

---

### Task 6: `Accord.Tests.Math` → net10.0

**Files:**
- Modify: `Unit Tests/Accord.Tests.Math/Accord.Tests.Math (NETStandard).csproj`

**Interfaces:**
- Consumes: the `net10.0` handling added to `Unit Tests/Accord.Tests (NETStandard).targets` in Task 5, Step 1.
- Consumes: `Accord.Core`/`Accord.Math.Core`/`Accord.Math`/`Accord.IO` all targeting `net10.0` (Tasks 1-4). Note this test project also references `Accord.Statistics` — **not yet migrated** (Phase 3). Its `(NETStandard).csproj` still multi-targets `netstandard2.0;netstandard1.4;net47`, which is compatible as a dependency of a `net10.0` project (a `netstandard2.0`-targeting library can be referenced by a `net10.0` consumer), so this does not block the build.

- [ ] **Step 1: Retarget the csproj and bump test tooling package versions**

  In `Unit Tests/Accord.Tests.Math/Accord.Tests.Math (NETStandard).csproj`:
  1. Change `<TargetFrameworks>netcoreapp2.0;net47</TargetFrameworks>` to `<TargetFramework>net10.0</TargetFramework>`.
  2. Replace the final `ItemGroup` (test tooling package versions) from:
     ```xml
     <ItemGroup>
       <PackageReference Update="Microsoft.NET.Test.Sdk" Version="15.3.0" />
       <PackageReference Update="NUnit" Version="3.8.1" />
       <PackageReference Update="NUnit.Console" Version="3.7.0" />
     </ItemGroup>
     ```
     to:
     ```xml
     <ItemGroup>
       <PackageReference Update="Microsoft.NET.Test.Sdk" Version="17.12.0" />
       <PackageReference Update="NUnit" Version="4.2.2" />
       <PackageReference Update="NUnit3TestAdapter" Version="4.6.0" />
       <PackageReference Update="NUnit.Console" Version="3.7.0" />
     </ItemGroup>
     ```
  3. Leave the `ProjectReference`s and the large `Resources\*.csv`/`Resources\mat\*.mat` `None`/`CopyToOutputDirectory` `ItemGroup` exactly as-is.

- [ ] **Step 2: Run the tests and confirm they pass**

  Run: `dotnet test "Unit Tests/Accord.Tests.Math/Accord.Tests.Math (NETStandard).csproj"`

  Expected: **Passed!** for all tests. If any test fails for a reason unrelated to this migration (e.g. a pre-existing flaky/environment-dependent test), investigate and note it — don't silently mark it `Ignore`.

- [ ] **Step 3: Commit**

  ```bash
  git add "Unit Tests/Accord.Tests.Math/Accord.Tests.Math (NETStandard).csproj"
  git commit -m "Retarget Accord.Tests.Math to net10.0, bump NUnit test tooling"
  ```

---

### Task 7: `Accord.Tests.IO` → net10.0

**Files:**
- Modify: `Unit Tests/Accord.Tests.IO/Accord.Tests.IO (NETStandard).csproj`

**Interfaces:**
- Consumes: the `net10.0` handling added to `Unit Tests/Accord.Tests (NETStandard).targets` in Task 5, Step 1.
- Consumes: `Accord.Core`/`Accord.Math.Core`/`Accord.Math`/`Accord.IO` all targeting `net10.0` (Tasks 1-4).

- [ ] **Step 1: Retarget the csproj and bump test tooling package versions**

  Replace the full contents of `Unit Tests/Accord.Tests.IO/Accord.Tests.IO (NETStandard).csproj` with:

  ```xml
  ﻿<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
      <RootNamespace>Accord.Tests.IO</RootNamespace>
      <AssemblyName>Accord.Tests.IO</AssemblyName>
      <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

    <Import Project="$(SolutionDir)../Unit Tests/Accord.Tests (NETStandard).targets" />

    <ItemGroup>
      <Compile Remove="ExcelReaderTest.cs" />
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include="..\..\Sources\Accord.IO\Accord.IO (NETStandard).csproj" />
      <ProjectReference Include="..\..\Sources\Accord.Math.Core\Accord.Math.Core (NETStandard).csproj" />
      <ProjectReference Include="..\..\Sources\Accord.Math\Accord.Math (NETStandard).csproj" />
    </ItemGroup>

    <ItemGroup>
      <None Update="Resources\csv\comma_in_quotes.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\empty.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\empty_crlf.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\escaped_quotes.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\json.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\newlines.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\newlines_crlf.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\new\double_quotes.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\new\error_recovery.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\new\french.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\new\pipes.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\new\tabs.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\quotes_and_newlines.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\readme.txt">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\simple.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\simple_crlf.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\csv\utf8.csv">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\excel\data1.xls">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\excel\data1.xlsx">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\excel\data2.xls">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\excel\data2.xlsx">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\excel\data3.xls">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\excel\data3.xlsx">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\excel\spreadsheet_names.xls">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\iris.scale.txt">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\liblinear\a9a.test">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\liblinear\a9a.train">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\numpy\npy_bool.npy">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\numpy\npy_byte.npy">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\numpy\npy_double.npy">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\numpy\npy_integer.npy">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\numpy\npy_single.npy">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\numpy\npy_strings.npy">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\numpy\npy_strings_var.npy">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\pendigits-orig.tes.Z">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\sample.xls">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\sample.xlsx">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
      <None Update="Resources\t10k-images-idx3-ubyte.gz">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      </None>
    </ItemGroup>

    <ItemGroup>
      <PackageReference Update="Microsoft.NET.Test.Sdk" Version="17.12.0" />
      <PackageReference Update="NUnit" Version="4.2.2" />
      <PackageReference Update="NUnit3TestAdapter" Version="4.6.0" />
      <PackageReference Update="NUnit.Console" Version="3.7.0" />
    </ItemGroup>

  </Project>
  ```

  The only real changes versus today's file are (a) `<TargetFrameworks>netcoreapp2.0;netcoreapp1.1;net47</TargetFrameworks>` → `<TargetFramework>net10.0</TargetFramework>`, and (b) the bumped-version `PackageReference Update` `ItemGroup` (same pattern as Task 5/6, plus the added `NUnit3TestAdapter` override). Everything else — `Compile Remove`, `ProjectReference`s, every `Resources\*` `None`/`CopyToOutputDirectory` entry — is carried forward unchanged from the current file.

- [ ] **Step 2: Run the tests and confirm they pass**

  Run: `dotnet test "Unit Tests/Accord.Tests.IO/Accord.Tests.IO (NETStandard).csproj"`

  Expected: **Passed!** for all tests (the Excel-reading tests are already excluded via `Compile Remove="ExcelReaderTest.cs"`, unchanged from today).

- [ ] **Step 3: Commit**

  ```bash
  git add "Unit Tests/Accord.Tests.IO/Accord.Tests.IO (NETStandard).csproj"
  git commit -m "Retarget Accord.Tests.IO to net10.0, bump NUnit test tooling"
  ```

---

### Task 8: Full Phase 1 verification

**Files:** none (verification only).

**Interfaces:**
- Consumes: everything from Tasks 1-7.

- [ ] **Step 1: Build all four Phase-1 libraries together**

  ```bash
  dotnet build "Sources/Accord.Core/Accord.Core (NETStandard).csproj" && \
  dotnet build "Sources/Accord.Math.Core/Accord.Math.Core (NETStandard).csproj" && \
  dotnet build "Sources/Accord.Math/Accord.Math (NETStandard).csproj" && \
  dotnet build "Sources/Accord.IO/Accord.IO (NETStandard).csproj"
  ```

  Expected: all four report **Build succeeded**, 0 Warning(s), 0 Error(s).

- [ ] **Step 2: Run all three Phase-1 test projects together**

  ```bash
  dotnet test "Unit Tests/Accord.Tests.Core/Accord.Tests.Core (NETStandard).csproj" && \
  dotnet test "Unit Tests/Accord.Tests.Math/Accord.Tests.Math (NETStandard).csproj" && \
  dotnet test "Unit Tests/Accord.Tests.IO/Accord.Tests.IO (NETStandard).csproj"
  ```

  Expected: all three report **Passed!** with 0 failures. This is the Phase 1 completion gate from the spec.

- [ ] **Step 3: Document the known, accepted Phase 2-5 breakage (not a regression check)**

  This step replaces an earlier version of this step that expected Phase 2+
  modules to still build. That expectation was wrong: retargeting
  `Accord.Core`/`Math.Core`/`Math`/`IO` to `net10.0`-only breaks every
  not-yet-migrated module's build via `NU1201` (`ProjectReference` TFM
  incompatibility) — confirmed empirically during Task 1, and the user
  explicitly chose to accept this rather than multi-target Phase 1's
  libraries during the transition (see the plan header's ⚠️ note and the SDD
  ledger's "accept temporary breakage" ruling).

  Run, to confirm the *expected* (not regressive) failure and capture it for the record:

  ```bash
  dotnet build "Sources/Accord.Statistics/Accord.Statistics (NETStandard).csproj"
  ```

  Expected: **fails with `NU1201`** naming `Accord`/`Accord.Math`/`Accord.Math.Core` as incompatible. This confirms the breakage is exactly the known, accepted one — not some new, different failure. If the error is anything other than `NU1201` on those three projects, that's a real regression: stop and investigate.

  No commit for this task — it's a verification-only checkpoint.
