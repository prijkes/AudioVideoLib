# ID3v2 Frame Editors — Phase 2: Finalisation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans. This is a sequential coordinator-only plan.

**Goal:** After all 39 editors are registered (Phase 0 + Waves 1-3), close out the project: harden the registry-completeness gate, refresh release notes, run all validation gates, perform a final code review, and (with explicit user authorisation) squash-merge the entire `feat/id3v2-frame-editors` branch to master.

**Pre-conditions:**
- Phase 0 + Wave 1 + Wave 2 + Wave 3 all complete and merged to the working branch.
- All 39 frame editors registered.
- `dotnet test AudioVideoLib.Studio.Tests/` green.
- `RegistryCompletenessTests` reports 0 missing editors (informational output empty).

**Reference:** Spec `specs/2026-05-06-id3v2-frame-editors-design.md` §10.3.

---

## Tasks

### Task 1: Flip the `RegistrationComplete` gate

**Files:**
- Modify: `AudioVideoLib.Studio.Tests/Editors/Id3v2/RegistryCompletenessTests.cs`

- [ ] **Step 1: Edit the constant**

```csharp
public const bool RegistrationComplete = true;
```

This activates the hard `Assert.Empty(missing)` and `Assert.Empty(missingTests)` assertions in `EveryConcreteId3v2Frame_HasRegisteredEditor` and `EveryEditor_HasMatchingTestClass`.

- [ ] **Step 2: Run the meta-tests**

```bash
dotnet test AudioVideoLib.Studio.Tests/ --filter RegistryCompletenessTests
```

Expected: 2/2 passing. If either fails, identify the missing editor or test class and complete it before proceeding.

- [ ] **Step 3: Commit** — `test(studio): activate registry-completeness hard assertions`.

---

### Task 2: Refresh App.xaml ObjectDataProvider resources

The coordinator may have accumulated `ObjectDataProvider` declarations in App.xaml across waves (Id3v2EncodingValues, Id3v2KeyEventTypeValues, Id3v2TimeStampFormatValues from Phase 0; Id3v2AudioDeliveryTypeValues from Wave 2; Id3v2ContentTypeValues / Id3v2InterpolationMethodValues / Id3v2ChannelTypeValues from Wave 3). Verify all are present and used.

- [ ] **Step 1: Audit App.xaml**

```bash
grep -E "ObjectDataProvider" AudioVideoLib.Studio/App.xaml
```

Expected: 7 entries (one per enum used by editor combos).

- [ ] **Step 2: Cross-check usage**

```bash
grep -rE "StaticResource Id3v2(Encoding|KeyEventType|TimeStampFormat|AudioDeliveryType|ContentType|InterpolationMethod|ChannelType)Values" AudioVideoLib.Studio/Editors/Id3v2/
```

Each declared resource should have at least one `StaticResource` reference. Unused resources can be removed; missing references mean an editor combo isn't bound.

- [ ] **Step 3: Commit if changes** — `chore(studio): consolidate App.xaml editor resources`.

---

### Task 3: Manage Frames dialog golden snapshot

**Files:**
- Possibly modify: `AudioVideoLib.Studio.Tests/Editors/ManageFramesViewModelTests.cs`
- Possibly create: `AudioVideoLib.Studio.Tests/Editors/Goldens/manage-frames-v220.txt`, `-v230.txt`, `-v240.txt`

If the spec §9.4 menu-builder snapshot tests were deferred during Phase 0, add them now. Otherwise, regenerate goldens to match the full-39-editor menu and assert.

**Intent:** the goldens are a *baseline snapshot* for future-PR regression detection — a generated golden cannot validate current correctness (the Phase-2 generator and the test agree by construction). Current correctness is covered by the per-version `BuildModel` expected-set tests in `Id3v2AddMenuBuilderTests` (added in Phase 0 / activated after Wave 3). The goldens catch *future drift* in menu structure, ordering, or category membership.

- [ ] **Step 1: Generate goldens**

For each of v2.2.0 / v2.2.1 / v2.3 / v2.4, instantiate `Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, new Id3v2Tag(version))` and serialise the model to a stable text representation (one entry per line: `Category | Identifier | Label`). Write the result to `Goldens/manage-frames-v220.txt`, `-v221.txt`, `-v230.txt`, `-v240.txt`.

- [ ] **Step 2: Add snapshot tests**

```csharp
[Theory]
[InlineData(Id3v2Version.Id3v220, "Goldens/manage-frames-v220.txt")]
[InlineData(Id3v2Version.Id3v221, "Goldens/manage-frames-v221.txt")]
[InlineData(Id3v2Version.Id3v230, "Goldens/manage-frames-v230.txt")]
[InlineData(Id3v2Version.Id3v240, "Goldens/manage-frames-v240.txt")]
public void MenuBuilder_MatchesGoldenSnapshot(Id3v2Version version, string goldenPath)
{
    var tag = new Id3v2Tag(version);
    var model = Id3v2AddMenuBuilder.BuildModel(TagItemEditorRegistry.Shared, tag);
    var actual = SerializeMenu(model);
    var expected = File.ReadAllText(goldenPath).ReplaceLineEndings();
    Assert.Equal(expected, actual);
}
```

- [ ] **Step 3: Run, verify pass**

```bash
dotnet test AudioVideoLib.Studio.Tests/ --filter MenuBuilder_MatchesGoldenSnapshot
```

- [ ] **Step 4: Commit** — `test(studio): menu-builder golden snapshots per ID3v2 version`.

---

### Task 4: Full validation gate

- [ ] **Step 1: Full Release build**

```bash
dotnet build -c Release
```

Expected: 0 warnings, 0 errors.

- [ ] **Step 2: Full test suite**

```bash
dotnet test -c Release
```

Expected: all green across `AudioVideoLib.Tests` and `AudioVideoLib.Studio.Tests`.

- [ ] **Step 3: DocFX (if part of CI)**

```bash
# Confirm the docfx config path first; this project uses docs/docfx.json (verify).
docfx docs/docfx.json --warningsAsErrors
```

Expected: clean. Any `_doc_snippets` build returns 0; no broken cross-refs. If `--warningsAsErrors` is unsupported by the installed docfx version (added in v2.59), pin a compatible version in CI or drop the flag.

- [ ] **Step 4: Manual smoke**

Launch Studio. For a representative file with v2.4, v2.3, and v2.2 tags:
- Open Add Frame menu — confirm all 14 categories appear with their members.
- Try adding a representative frame from each category (TIT2 / WCOM / GRID / COMM / SYLT / ETCO / IPLS / RVA2 / POPM / APIC / OWNE / AENC / CDM (v2.2.1 only) / RBUF / XRVA).
- For each, complete the editor and save; confirm the frame is added.
- Cancel an Add — confirm no frame added.
- Edit each existing frame — confirm `Load` populates correctly and `Save` writes back.
- "Manage frames…" toolbar button — confirm search filtering works.

If any step fails, fix in a per-issue commit before merge.

---

### Task 5: Update release notes

**Files:**
- Modify: `docs/release-notes.md`

- [ ] **Step 1: Promote the "(next release)" entry**

Open `docs/release-notes.md`, find the "(next release)" section, rename to today's date and version. Add:

```markdown
## 0.9.0 — YYYY-MM-DD

- **AudioVideoLib.Studio:** complete ID3v2 frame editor surface — every of the 39 frame classes
  (text, URL, identification, comments & lyrics, timing & sync, people, audio adjustment,
  counters & ratings, attachments, commerce & rights, encryption & compression, containers,
  system, experimental) is now Add/Edit-capable through dedicated dialogs filtered by tag
  version, plus a "Manage frames" power-user search dialog reachable from the toolbar.
- New `Editors/` framework (tag-format-agnostic foundation + ID3v2 layer + 39 editor pairs)
  designed so future tag formats (APE, Lyrics3v2, Vorbis, ID3v1) can plug in by adding
  sibling `Editors/Ape/`, `Editors/Vorbis/`, etc. directories.
```

- [ ] **Step 2: Bump `Directory.Build.props`** version to `0.9.0` (or whatever the user's chosen version is).

- [ ] **Step 3: Commit** — `release: 0.9.0 — full ID3v2 frame editor surface`.

---

### Task 6: Final code review

Dispatch the `superpowers:code-reviewer` subagent with the entire branch's diff against master.

- [ ] **Step 1: Generate the review prompt**

```
Review the entire feat/id3v2-frame-editors branch (Phase 0 + Wave 1 + Wave 2 + Wave 3 + Phase 2).
Focal areas: registry shape, two-class pattern adherence, version-mask correctness, test coverage,
spec compliance per specs/2026-05-06-id3v2-frame-editors-design.md.
Output: BLOCKER / IMPORTANT / NICE-TO-HAVE findings, prioritised.
```

- [ ] **Step 2: Address findings**

For each BLOCKER: fix in a small commit, retest. For IMPORTANT: triage with user (defer or fix). For NICE-TO-HAVE: file as follow-up tickets.

- [ ] **Step 3: Re-run validation gates** after fixes.

---

### Task 7: Squash-merge to master

**⚠ Requires explicit user authorisation before pushing.** Coordinator does NOT push without confirmation.

- [ ] **Step 1: Summarise the branch state to the user**

Report to the user:
- Branch name: `feat/id3v2-frame-editors`
- Commit count
- Test count
- Build state (Release, all green)
- DocFX state
- Any open BLOCKER / IMPORTANT findings from Task 6

Wait for the user to authorise the merge.

- [ ] **Step 2: Squash-merge**

```bash
git checkout master
git merge --squash feat/id3v2-frame-editors
git commit -m "$(cat <<'EOF'
feat(studio): full ID3v2 frame editor surface

Adds Add/Edit dialogs for every of the 39 ID3v2 frame classes via a
tag-format-agnostic editor framework with reflection-based registration.
Cascading Add Frame menu filtered by tag version, plus a Manage Frames
power-user search dialog.

The framework's foundation (Editors/*.cs) is tag-format-agnostic; future
APE / Lyrics3v2 / Vorbis / ID3v1 editor surfaces plug in as siblings.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 3: Push (with user authorisation)**

```bash
git push origin master
```

- [ ] **Step 4: Create release on GitHub**

```bash
# Use --notes-file to extract the v0.9.0 entry from release-notes.md, or paste the entry
# inline via --notes if you prefer a hand-edited summary.
gh release create v0.9.0 \
    --title "v0.9.0 — Full ID3v2 frame editor surface" \
    --notes-file docs/release-notes.md
```

(Skip if user prefers to create the release through the web UI.)

- [ ] **Step 5: Clean up the working branch**

```bash
git branch -d feat/id3v2-frame-editors
git push origin --delete feat/id3v2-frame-editors   # only if user authorises
```

---

## Phase 2 done definition

- All 7 tasks complete; user has authorised the merge.
- `master` contains the squash-merged commit.
- Release v0.9.0 published.
- All 39 ID3v2 frames have Add/Edit dialog support in Studio.
- Editor framework foundation is ready for future tag-format additions.
