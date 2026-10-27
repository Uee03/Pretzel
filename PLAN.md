# Pretzel Plan

Living plan for Pretzel, a modular UPM successor to LazyJedi (`c:\Projects\Git\Unity\lazyjedi`).
Update the status boxes and the Decisions section as work happens. Last updated: 2026-10-27.

Status key: `[x]` done, `[ ]` not started, `[~]` partly done.

## Ground rules

- Packages are named `com.uee03.pretzel.*`, assemblies `Pretzel.<Module>`, root namespace `Pretzel.<Module>`.
- Asmdefs reference each other by name, not GUID. `Pretzel.Core` is the only base dependency.
- Runtime asmdefs are `autoReferenced: true` so user scripts and samples can use them. Editor asmdefs are not.
- DataIO has an EditMode regression suite; each package also ships usage examples in `Samples~`.
- Unity 6000.6.5f1 at `D:\Program Files\Unity\Hub\Editor\6000.6.5f1`. Packages target `unity: 6000.0`.
- LazyJedi stays as it is and is deprecated once Pretzel ships.
- Verify by compiling headless, then check the log for `error CS`:
  `Unity.exe -batchmode -nographics -quit -projectPath C:\Projects\Local\Unity\Pretzel -logFile <log>`
- To compile samples, copy them into `Assets/` temporarily, then delete the copy and its `.meta`.

## Package map

| Package | Contents | Ported from | Status |
|---|---|---|---|
| `pretzel.core` | `Guard`, singletons | Extras singletons | `[x]` |
| `pretzel.extensions` | String, Byte, IO, Object, GameObject, Transform, List/Array, LayerMask, Float, Texture2D, MonoBehaviour | `Common/Extensions/*` | `[x]` |
| `pretzel.data` | `IDataStore`/`DataStore`, `DataKey`, `IDataRoot`, `ISerializer` (Newtonsoft default, `JsonUtility`), `IDataTransform`, thin `DataIO` facade, `SerializableGuid`, JSON string helpers | `DataIO` (full redesign), `SerializableGuid` | `[x]` |
| `pretzel.api` | `Awaitable` HTTP client on `UnityWebRequest` | `WebRequestUtility` (rewrite) | `[x]` |
| `pretzel.tween` | Easing functions with fixes, LitMotion mapping | `EaseUtility` | `[x]` |
| `pretzel.ui` | UI Toolkit only: factories, fader port, `UIDocumentFader`, hover trigger, controller base | `UIToolkitUtility`, `UIFader`, `UIControllerBase` | `[x]` |
| `pretzel.ui.ugui` | UGUI `UIFader`, `OnMouseHoverFade`, `UIFaderEditor`, `UGUIExtensions` | Extras `UIFader` set, `UIExtensions` | `[x]` |
| `pretzel.editor` | Standalone editor utilities that work in any project (`GroupHierarchyObjects`). No dependencies | Core Editor folder | `[x]` |
| `pretzel.setup` | Project Setup window shell, Main, Addons, Addons Extra and Settings tabs, settings store, folders, first run, starter kit, catalogs, 2D/3D scene templates. Needs only Newtonsoft.Json | Core Editor folder, `CustomPackages` (expanded) | `[x]` |
| `pretzel.templates` | Script templates | `ScriptTemplates`, Examples | `[x]` |
| `pretzel.integrations` | Optional R3, LitMotion and UniTask adapters, one asmdef each under `Runtime/<Library>`, via `versionDefines`. Split into separate packages if it grows | New | `[x]` |

Dependency direction: `ui.ugui` depends on core, ui, tween and `com.unity.ugui`. `ui` depends on core and tween. `integrations` depends on core, tween, api and ui. `api` depends on core and data. `setup` depends on no Pretzel module. `editor` depends on nothing. Editors that belong to one module live in that module (`data/Editor`, `ui.ugui/Editor`).

## Steps

### Phase 0: Foundations
- [x] 1. Monorepo with `Packages/`, one `package.json` per module, Unity host project resolving all packages locally.
- [x] 2. Conventions set (see Ground rules). Runtime asmdefs set to auto-referenced.
- [x] 2a. Scaffold `com.uee03.pretzel.ui.ugui` with `package.json`, runtime asmdef and Editor asmdef.

### Phase 1: Core and extensions
- [x] 3. `pretzel.core`. `Guard`, `Singleton`, `PersistentSingleton`, `RegulatorSingleton`. Sample: `Samples~/CoreUsage`.
- [x] 4. `pretzel.extensions` with the audit fixes. Sample: `Samples~/ExtensionsUsage`.
- [x] 4a. `FadeType` lives in `pretzel.ui`, shared with `ui.ugui`. Enums moved out of core, see Done so far.

### Phase 2: Data, API and tween (parallel)
- [x] 5. `pretzel.data`. `DataIO` is a full redesign, not a port. Do not carry over its API, static mutable fields, `PathType` switch or `Save`/`SaveToSlot` pairs. The old behaviour is only a reference for what must still be possible.
  - Design:
    - `IDataStore` with one implementation, `DataStore`. Constructed with a root, an `ISerializer`, a file extension and options. Instance state only, no mutable public statics.
    - `DataKey` value type (readonly struct): name, optional slot, optional sub-folder. One `Save`, `Load`, `TryLoad`, `Exists`, `Delete` and `List` for every key. No slot-specific method pairs. Default name is explicit, not `typeof(T).Name`.
    - `IDataRoot` (path provider) replaces the `PathType` enum and its switch. Built-ins: persistent data, temporary cache, `ProjectSettings` (editor only), `UserSettings` (editor only), absolute path. Roots resolve lazily, so `Application.persistentDataPath` is never read in a static initializer.
    - `ISerializer` strategy: Newtonsoft default, `JsonUtility` for ScriptableObjects and for projects without Newtonsoft. Serializer-aware `LoadInto(target)` replaces `LoadAndOverwrite`.
    - Pipeline of encoding strategies (plain, binary, AES) between serializer and disk. UTF-8 only.
    - Static `DataIO` facade over a replaceable default store, for the simple case. Thin: no logic of its own.
  - API shape:
    - `Result<T>` or `TryLoad` for missing and corrupt files. No `default` return, no `Debug.unityLogger` error as flow control. Exceptions only for programmer errors, through `Guard`.
    - `Awaitable` variants of every I/O method with `CancellationToken`. The sync variants stay for small data.
    - Key and filename validation: reject separators, `..`, invalid characters and reserved names so a key can never escape the root.
  - Reliability:
    - Flushed temp writes, replacement and a recoverable rollback fallback, with optional `.bak` recovery. Not a universal atomicity/power-loss guarantee.
    - `schemaVersion` in an envelope, with an `IMigration` chain per type.
    - Thread-safe: no static caches, paths resolved on the main thread and cached in the store.
  - Serialization extras:
    - Declare `com.unity.nuget.newtonsoft-json` in `package.json`. Add small converters for `Vector2`, `Vector3`, `Quaternion`, `Color`. Check whether the Unity package already ships converters.
  - Moves:
    - `SerializableGuid` here, with the hash/equals contract and string constructor validation fixed.
    - `ToJson`, `FromJson`, `FromJsonOverwrite` string helpers route through the default serializer.
    - `PathType` is dropped, replaced by `IDataRoot`.
  - Migration for LazyJedi users: a short mapping table in the package README (old call -> new call). No `[Obsolete]` shim of the old API unless asked.
  - Sample: `Samples~/DataUsage` showing keys, slots, a ScriptableObject, a migration and an async save.
- [x] 6. `pretzel.api`:
  - `Awaitable<Response<T>>` on `UnityWebRequest`, with `CancellationToken`, `IProgress<float>`, timeout, optional retry with backoff.
  - `GetAsync`, `PostAsync`, `PutAsync`, `DeleteAsync`, `HeadAsync`, typed JSON helpers via the `pretzel.data` serializer.
  - Downloads for text, bytes, textures, audio and files. Files stream through `DownloadHandlerFile`.
  - `Response<T>` carries `Ok`, `StatusCode`, `Data`, `Error`, `Headers`.
  - Fix the Post/Put bug that returned the request body. Add null-safe handling. Do not re-encode textures.
  - No coroutine API. Add an `[Obsolete]` shim only if wanted.
- [x] 7. `pretzel.tween`: keep the structure and the `Evaluate` enum switch. Fixes:
  - `InExpo(0)` returns 0.00098, and `OutExpo(1)` is off the same way. Add explicit endpoints.
  - `InCirc` returns NaN outside 0 to 1. Clamp once in `Evaluate`.
  - `OutElastic(0)` is `1 - amplitude`. Only correct when amplitude is 1.
  - `InOutBack` mirrors `InBack` instead of using the 1.525 constant. Decide which curve to keep.
  - Add `AggressiveInlining`, `LerpUnclamped` for overshoot curves, rename `Vector2`/`Vector3`/`Color` overloads, drop the `Evaluate(time, curve)` wrapper.
  - Add an `EaseType` to LitMotion `Ease` mapping in the integration package.

### Phase 3: UI and editor
- [x] 8a. `pretzel.ui` (UI Toolkit only, depends on core and tween; add the `Pretzel.Tween` asmdef reference and package dependency when the fader lands). Can start after Phase 1:
  - Port `UIToolkitUtility`. Re-check `EnumField.Init` and `ToolbarPopupSearchField` on 6000.6.
  - `UIToolkitFader`: same `FadeType`, duration and `EaseType` inputs. Opacity through USS transitions, `display: none` on fade-out completion, callback on `TransitionEndEvent`, new fade cancels the old one. `VisualElement` extensions such as `element.FadeIn(duration, ease)`. Check the `EaseType` to `EasingMode` mapping and fall back to the nearest curve where there is no match.
  - `UIDocumentFader` component and a hover trigger using `PointerEnterEvent` and `PointerLeaveEvent`, unregistered on disable.
  - UI Toolkit controller base (port of `UIControllerBase`).
- [x] 8b. `pretzel.ui.ugui`, after step 7:
  - `UIFader` keeps its public API: `FadeIn`, `FadeOut`, `FadeInNow`, `FadeOutNow`, fade on start, `EaseType` or `AnimationCurve`, `CanvasGroup` mode and `CanvasRenderer` list mode. Fixes: auto-find a `CanvasGroup`, no per-call logging, keep `_currentAlpha` in sync, check references.
  - `OnMouseHoverFade`, null-safe.
  - `UIFaderEditor` built in code, `SetDirty` on the target, no `VisualTreeAsset` field.
  - `UGUIExtensions` (renamed from `UIExtensions`): `Button.AddListener`, `RemoveListener`, `RemoveAllListeners` (add a null guard), `Graphic.SetPlaceHolderText` (no-op unless the graphic is a `TMP_Text`, keep and document).
  - The only tween coupling is `EaseUtility.Float` in `ApplyEasingHelper`.
- [x] 9. `pretzel.editor`, Setup window (see the next section), plus:
  - Port `SerializableGuidDrawer`, folder creation and grouping (with Undo).
  - Make path-opening commands cross-platform, using `EditorUtility.RevealInFinder` or `OpenWithDefaultApp`.
  - Fix the `PlayerSettings` static cache and re-check the icon APIs.
  - Drop `DragScriptToHierarchy`, `ScriptableObjectEditor`, `RestartUnity` (pending decision below).

### Phase 4: Package tabs (depends on 5 and 9)
- [x] 10. Define the Addons Extra catalog: id, display name, source (git URL, OpenUPM or NuGetForUnity), pinned version, dependencies, registry note. Launch entries: R3, LitMotion, UniTask, ZLinq, VContainer, NuGetForUnity, Newtonsoft.Json. Verify each source and version against its README at implementation time.
- [x] 11. In `pretzel.packages`, build the Addons and Addons Extra tabs on `PackageManager.Client` (`List`, `Add`, `Remove`). Edit `manifest.json` only where a scoped registry is needed. R3 needs its NuGet dependencies through NuGetForUnity first.
- [x] 12. First-run flow that opens the Main tab and offers a 2D or 3D preset with recommended addons.

### Phase 5: Integrations, samples and docs
- [x] 13. Integration packages (R3, LitMotion, UniTask) with `versionDefines`.
- [x] 14. Script templates and 2D/3D scene templates in `pretzel.templates`. The LazyJedi examples were already covered by each package's `Samples~` (Ease, Extensions, FileIO, Guid, WebRequest, Fade), so none were copied. Fader examples: `UIUsage` and `UGUIUsage`.
- [x] 15. Package READMEs and a root README with a module diagram.

## Setup window (`pretzel.setup`)

- Resizable `EditorWindow`, menu `Pretzel/Project Setup`. Default Unity editor font, built-in light and dark skins.
- Branding from `Branding/`: `pretzel.png` and `pretzel-sunflower-tile.png` as window icon and header mark, plain "Pretzel" title and version. Use PNGs, because UI Toolkit cannot render the SVG `<text>`. The Pacifico lockup is not used.
- `TabView` with four tabs. Tabs implement `IPretzelTab`, found with `TypeCache`, with title and order. The Addons tabs live in the same package (`Scripts/Addons`).
  1. **Main**: company, product, version, icon, cursor. Folder-structure preset (Game, Tool, System, Custom; scripts in `_Scripts/{Editor,Runtime}`, everything else in `_Project`) with reorderable `ListView` and Create Folders. 2D/3D scene template picker. Setup checklist.
  2. **Addons**: card per Pretzel module, including both UI packages, with installed version, dependencies and install, uninstall or update. Integration cards show a "requires" badge.
  3. **Addons Extra**: third-party catalog with search, pinned version, installed state, registry notes.
  4. **Settings**: folder preset defaults, `DataIO` settings folder, catalog sources, per-user paths, reset per section. The serializer, save folder, extension and format moved to the Data Format window (Phase 6).
- No Save, Load or Auto-save toolbar. Changes persist with a short debounce and on window close, with a Reset action.
- Folder rows register callbacks once in `makeItem` or unregister in `unbindItem`. The old window re-registered on every bind.
- The Credits window is folded into an About section.
- Styling: USS variables for the palette (honey `#FFE100` to `#FF9B00`, outline `#855000`, background `#F3EDA6` to `#EBE389`), BEM class names, USS transitions on cards, chips for installed, update and missing, `HelpBox` for requirements, flex-wrap and `ScrollView`.

### Settings storage (JSON, through `pretzel.data`)

| File | Scope | Holds |
|---|---|---|
| `ProjectSettings/Pretzel/pretzel-settings.json` | Project-wide, committed | Folder presets, catalog sources and pinned versions, `schemaVersion` |
| `UserSettings/Pretzel/pretzel-user.json` | Per user, normally git-ignored | Temp and resources folders, last tab, window state |

A missing or corrupt file falls back to defaults. One `PretzelSettings` accessor fronts both.

## Audit of LazyJedi

### Remove from core
`SystemProcessUtility` (Windows-only, `explorer.exe`), `DragScriptToHierarchy` (private reflection, endless poll), `ScriptableObjectEditor` (global `CustomEditor(typeof(ScriptableObject), true)`), `UIControllerBase` UGUI version (replaced by the UI Toolkit controller base), `Observable` (R3 covers it, pending decision), `RestartUnity` (pending decision).

### Modernize
| Area | Changes |
|---|---|
| `WebRequestUtility` / `Response` | Rewrite on `Awaitable`, see step 6. |
| `DataIO` | Full redesign, not a port: instance `DataStore`, `DataKey`, `IDataRoot`, `ISerializer`, recoverable writes and async. Thin static facade over the replaceable default store. See step 5. |
| `ProjectWindow` / `CreditsWindow` / `Project` / `StringGlobals` | Rewrite, see the Setup window section. |
| `EaseUtility` | See step 7. |
| `SerializableGuid` | `GetHashCode` ignores case but `Equals` does not. String constructor accepts malformed input. |
| `UIFader` set, `UGUIExtensions` | Kept in `pretzel.ui.ugui`, see step 8b. |
| `Singleton` family, `GameObject/String/List/...` extensions | Done in Phase 1. |
| `EntityBase` | Hooks never called, but templates derive from it. Migrate the templates first. |
| `GroupHierarchyObjects` | Add Undo and an empty-selection guard. |
| `SerializableGuidDrawer` | Empty `OnGUI`, `GuidString` fallback is a property, not a serialized field. |
| `CustomPackages` | Concurrent requests overwrite one field, Git URLs unpinned. Replaced by the Addons tabs in `pretzel.setup`. |
| `BrowseCustomPaths` | Hard-coded `explorer.exe`. |

### Leave as is
`UIToolkitUtility` (re-check on 6000.6), `ByteExtensions`, `CreateThings` and folder creation.

## Done so far (details)

- Host project opened in Unity, `ProjectSettings` and `Library` created. Unity expanded `Packages/manifest.json` with its default packages. Newtonsoft 3.2.2 already resolves.
- Phase 1 renames and removals relative to LazyJedi:
  - `RemoveSpecialChars_ExclSpaces` is now `ToAlphanumeric`. `RemoveAllSpecialChars` is now `RemoveSpecialChars`.
  - `IsValidEmail_Regex` and `IsValidEmail_MailAddress` are now `IsValidEmail`. `IsValidUrl_Regex` and `IsValidUri` are now `IsValidUrl`.
  - `GameObject.Activate(float)` and `Deactivate(float)` are now `ActivateRoutine` and `DeactivateRoutine`.
  - `StreamWriter` and `StreamReader` extensions are now `WriteWithStream` and `ReadWithStream`.
  - Dropped: `IsNullOrWhiteSpace(trim)`, the `MailAddress` email checks, `ArrayExtension` (merged into `ListExtensions`).
- Singletons are used as `class Foo : Singleton<Foo>`. Duplicates are destroyed, instances reset in `OnDestroy`, nothing is created in edit mode or while quitting.
- Enum ownership: each enum lives in the package that owns the concept, one enum per file, namespace `Pretzel.<Module>`, and core holds none. `EaseType` in `tween`, `EncodingType` in `extensions`, `FadeType` in `ui` (`ui.ugui` depends on `ui` for it). `PathType` is replaced by `IDataRoot` in `data` (step 5), `HttpMethodType` and `TextureType` go in `api`.

## Open decisions

1. Remove `Observable` and `RestartUnity`? Recommended: yes.
2. Newtonsoft as the default serializer? Recommended: yes, as a declared dependency of `pretzel.data`.
3. Does the integrations package appear in the Addons tab or only in Addons Extra next to its third-party packages? Recommended: Addons tab with a "requires" badge.
4. Shared settings file in `ProjectSettings` (committed), or everything in `UserSettings`? Recommended: both, as in the table above.
5. `InOutBack` curve: decided, use the standard 1.525 constant (matches easings.net and LitMotion).

## Phase 2 notes

- Tween: `Float`/`Vector2`/`Vector3`/`Color` overloads are now `EaseUtility.Lerp` (unclamped). `Evaluate` clamps time once. `OutElastic` uses Penner's phase shift and clamps amplitude to 1 or more, so it is 0 at 0 for any amplitude. LitMotion mapping is `EaseType.ToLitMotionEase()`, behind the `PRETZEL_LITMOTION` define (not compiled until LitMotion is installed).
- Data: `com.unity.nuget.newtonsoft-json` ships no Unity converters, so `Vector2`, `Vector3`, `Quaternion`, `Color` and `SerializableGuid` converters are in `pretzel.data`. Versioned types use `[SchemaVersion(n)]` and `IMigration`, and are written as `{"schemaVersion": n, "data": {...}}` (needs a JSON serializer). `NewtonsoftSerializer` uses `ObjectCreationHandling.Replace`, because the default appends loaded items to lists that a field initializer already filled. `SerializableGuid` string to guid conversion is now explicit.
- Api: `ApiClient` instance (no statics). `TextureType` was dropped because textures are no longer re-encoded. `HttpMethodType` lives in `pretzel.api`. AssetBundle download was dropped.
- Verified with a throwaway editor script (86 checks: easing endpoints, store round trip, backup recovery, migration, key validation). It was deleted afterwards.

## Phase 3 notes

- Decisions applied: `Observable`, `RestartUnity`, `DragScriptToHierarchy` and `ScriptableObjectEditor` were not ported (open decision 1). Settings live in both files (decision 4). The menu is `Pretzel/Project Setup` (this plan wins over the `Tools/Pretzel/` rule in the skill).
- `pretzel.ui`: `UIToolkitUtility` is ported. `EnumField` and `EnumFlagsField` are built with their constructors, because `new EnumField()` plus setting `value` leaves the text empty on 6000.6. `CreateToolbarPopup` is now `CreateToolbarPopupSearchField`. `UIToolkitFader` uses USS transitions, a `TransitionEndEvent` callback and a fallback timer, because transitions never end on an element that is not rendered. `EaseType.ToEasingMode()` maps Quad to `EaseIn/Out/InOut` and Quart, Quint and Expo to the Cubic modes. UI Toolkit has no closer match. Components: `UIDocumentFader`, `UIHoverFade`, `UIControllerBase`.
- `pretzel.ui.ugui`: `UIFader` fields are private and serialized (`_useCanvasGroup`, `_fadeInEaseType`, and so on). The old public fields are gone. `OnMouseHoverFade` also implements the pointer handlers, so it works on UI graphics. `UIFaderEditor` binds to the serialized properties, so dirty and undo are handled and it has no explicit `SetDirty`.
- `pretzel.editor`: tabs implement `IPretzelTab` (public, found with `TypeCache`). `pretzel.packages` adds its tabs by implementing it, with `Order` between 0 and 100. Settings are saved through a 500 ms debounce and on window close. The scene template picker lists `SceneTemplateAsset`s under `Packages/com.uee03.pretzel.templates/`, and shows a hint until step 14 adds them. The serializer, save folder, settings folder and extension settings are stored but nothing reads them at runtime yet.
- Verified headless with throwaway editor scripts: settings round trip, folder creation, the Guid drawer, the window builds with both tabs and the fader completes, supersedes and cancels. Not verified: the look of the window in the Editor (spacing, TabView content sizing, USS colors), and fades with a real GPU frame loop. Open `Pretzel/Project Setup` once and check it.

## Phase 6: Data formats and the Data Format window

- [x] 16. `pretzel.data` formats: `DataFormat` (PlainText `.json`, Binary `.bin`/`.dat`, Encrypted `.pretzel`), `BinaryTransform` (versioned BinaryWriter/BinaryReader), `EncryptionTransform` (AES-256-CBC + HMAC-SHA256), `Base64Transform`, `EncryptionKeys`, `EncryptionKeyFile`, `DataStoreFactory`, and `DataIOConfig`.
- [x] 17. `pretzel.editor` Data Format window (`Pretzel/Data Format`): Format, Encryption, Keys, Shipping and Decrypt tabs. Guide: `pretzel.data/Documentation~/EncryptionKeyDelivery.md`.

### Phase 6 notes

- Plain JSON stays the default; AES is the only encryption option. `IDataFormatStrategy` selects plain, binary or AES encoding, preserving schemas, migrations, `LoadInto` and both serializers. The settings asset and enum factory select the same strategies. Setup generates distinct JSON settings, binary inventory and AES profile examples.
- Binary uses a `PZBN` header and versioned BinaryWriter/BinaryReader values, never BinaryFormatter or Newtonsoft BSON APIs.
- The IV is random per file and stored in the file, so the window stores no IV. A fixed IV is not offered on purpose.
- Key storage: `<PREFIX>_AES_KEY` (Base64, persisted at user level on Windows only) and `UserSettings/Pretzel/pretzel-keys.json` (per project, plaintext, Editor-only). Keep one backed-up key for a normal game's lifetime. Rotation/history is optional advanced functionality.
- Moved out of `PretzelProjectSettings` into `DataIOConfig`: `serializer`, `saveFolder`, `fileExtension`. Old files that still contain them load fine, the members are ignored. `SerializerChoice` now lives in `Pretzel.Data`.
- AES uses separate derived encryption/MAC keys, random IVs and fixed-time tag comparison. Optional passphrase derivation uses PBKDF2-HMAC-SHA256 (600,000 default iterations); measure on target devices rather than assuming Editor timing.
- Setup now provides isolated save/load diagnostics, folder access, cautious build-check status and pre-construction migration callbacks. Load results categorize schema, authentication, format and I/O failures.
- Clean-host CI, generated-example compilation and release/compatibility gates are documented in [COMPATIBILITY.md](COMPATIBILITY.md). Player, minimum-Editor and IL2CPP checks must not be inferred from local EditMode success.

## Phase 5 notes

- Host `Packages/manifest.json` was aligned with MortalTowers (ide.rider 3.0.40, ide.visualstudio 2.0.28). `com.unity.analytics` (deprecated, pulled in the deprecated `com.unity.purchasing`), `collab-proxy`, `multiplayer.center`, `xr.legacyinputhelpers` and `ai.navigation` were removed. That cleared the "packages contain warnings" message. `pretzel.data` now requires Newtonsoft 3.2.2, which is what Unity 6000.6 resolves.
- Integrations were compiled in a scratch copy of the project with LitMotion v2.0.2, UniTask 2.5.11 and R3 1.3.1 (R3's NuGet DLLs placed in `Assets/Plugins`). A smoke test checked that every `EaseType` maps to the `Ease` of the same name and that the R3 event observable unsubscribes on dispose. The UniTask and `ApiClient` observables were compiled, not run.
- `Pretzel.Integrations.R3` and `.UniTask` put their `using` directives inside the namespace, because a namespace named `R3` or `UniTask` under `Pretzel.Integrations` would otherwise hide the third-party namespace.
- Script templates live in `Templates~` and are created from `Assets > Create > Pretzel`, because Unity only reads `ScriptTemplates` from the project's `Assets` folder. Scene templates `Pretzel2D` and `Pretzel3D` were generated with `SceneTemplateService` and fill the Main tab picker. The script templates compile after placeholder substitution. The menu items and scene templates were not opened in the Editor.

## Phase 7: package restructure (2026-10-19)

- [x] 18. `pretzel.setup` is one package with no Pretzel dependency. It absorbed the Setup shell from `editor`, all of `packages` (deleted) and the 2D/3D scene templates from `templates`. Assembly `Pretzel.Setup.Editor`, namespaces `Pretzel.Setup.*` (`Pretzel.Setup.Addons.*` for the old `Pretzel.Packages.*`). It dropped its use of `Pretzel.Data` (settings are read and written with Newtonsoft directly, same `{ schemaVersion, data }` file format), `Pretzel.UI` (one button) and `Pretzel.Api` (`UnityWebRequest` for remote catalogs). Only `com.unity.nuget.newtonsoft-json` remains.
- [x] 19. The Data Format window, its panels and `SerializableGuidDrawer` moved to `pretzel.data/Editor` (assembly `Pretzel.Data.Editor`, namespace `Pretzel.Data.Editor`). Rule: an editor that belongs to one module lives in that module. `ui.ugui` already followed it.
- [x] 20. `pretzel.editor` keeps only standalone editors that work in any project (`GroupHierarchyObjects`). No dependencies.
- [x] 21. Styles: each package is self-contained. The Data Format window has one stylesheet with its own tokens and the few shared components, and its own copy of the sunflower tile (about 150 KB) as header logo and window icon. The Setup window keeps `Variables.uss`, its own stylesheet and branding.
- Setup reaches the Data Format window with `EditorApplication.ExecuteMenuItem("Pretzel/Data Format")`, so there is no compile-time link. It shows a hint when `pretzel.data` is not installed.
- Open: `ModuleCatalog` still mirrors each module's `package.json` dependencies by hand. A generated `modules.json`, or reading the installed package's dependencies, would give one source of truth. Resolved in Phase 8.
- Verified: headless compile of a scratch copy with 0 errors and 0 warnings. Behavior checks in the scratch project: see the check list in the repo memory.

## Phase 4 notes

- Catalog (checked against the READMEs on 2026-10-18): NuGetForUnity v4.5.0, UniTask 2.5.11, LitMotion v2.0.2, R3 1.3.1, ZLinq 1.5.6, VContainer 1.19.0 (all git URLs with a pinned tag), Newtonsoft.Json 3.2.1 (Unity registry). Pins can be changed per project on the Addons Extra tab and are saved in `pretzel-settings.json`. Catalog files (JSON, `CatalogFile`) listed under Settings > Catalog sources add entries or replace built-in ones by id. The `OpenUpm` source edits `Packages/manifest.json` (adds the OpenUPM registry, the scope and the dependency). No built-in entry uses it yet.
- R3 and ZLinq also need their NuGet package. Pretzel cannot install it, so the card shows "NuGet R3 needed", installs NuGetForUnity first, then opens the NuGet window. Install stays disabled until `packages.config` lists the NuGet package, because the Unity package does not compile without it. LitMotion is installed straight from git.
- Installs are one `Client.AddAndRemove` call, because a domain reload after the first package would drop a queue. Remove is blocked while another installed package depends on it, including Newtonsoft.Json for Pretzel Data.
- The Addons tab installs Pretzel modules from a git repository set in Settings (`moduleSourceUrl`, `moduleSourceVersion`), because the Pretzel repository URL is not known yet. Install is disabled until it is set. Modules already in the project show as Local.
- Extension points added to `pretzel.editor`: `IPretzelMainSection` (cards on the Main tab). The starter kit card is the first one. It stays until it is used or skipped, and the Setup window opens once by itself (`firstRunShown` in the user settings).
- Verified headless: manifest edit, catalog merge, pins, remote catalog loading, a real install and remove of a registry package through the Package Manager, 4 tabs and 20 cards. Screenshots checked for all four tabs. Not verified: installing from a git URL, the NuGet window handoff, and a remote http catalog.

## Phase 8: 0.2.0 release (2026-10-27)

- [x] 22. Hosted CI removed; validation is local through `New-UnityValidationHost.ps1` (see COMPATIBILITY.md).
- [x] 23. Module catalog generated from the manifests: `.github/scripts/Update-ModuleCatalog.ps1` writes `setup/Editor/Catalog/modules.json`; `-Check` and `ModuleCatalogTests` guard drift. Package descriptions in `package.json` are the catalog text.
- [x] 24. New packages: `events` (EventBus, SO channels, listeners), `di` (container, lifetimes, scopes, entry points, `LifetimeScope`), `pooling` (`ComponentPool<T>`, `PooledObject`, `PoolRegistry`), `scenes` (`SceneLoader`, `UIDocumentSceneTransition`), `audio` (`AudioManager`, `AudioCue`), `preferences` (`PreferencesService`), `devconsole` (`DeveloperConsole`, `CommandRegistry`, `LogBuffer`).
- [x] 25. Additions: core timers and `DisposableAction`; data `SaveSlots<T>` and `AutoSaveTrigger`; ui screen stack, toasts, safe area, element extensions, world helpers (world-space panels behind `UNITY_6000_2_OR_NEWER`) and the `Pretzel.UI.Editor` assembly; templates UI Toolkit theme.
- [x] 26. EditMode tests for every package with logic (271 tests). MonoBehaviours used by tests live in `Tests/Fixtures` runtime asmdefs, because components from Editor assemblies cannot be added.
- [x] 27. All packages at 0.2.0, first tag `v0.2.0`. RSA, BSON and gzip removal is not a breaking change, because nothing shipped before.
