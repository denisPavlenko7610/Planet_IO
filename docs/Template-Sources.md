# UnityTemplates integration

Planet_IO uses the sibling checkout at `../UnityTemplates` through Windows directory junctions, following Night
Courier's source-link workflow. Unity 7000.0.0a16 is required. These are source folders, not UPM packages.

## Restore the project

1. Clone `git@github.com:denisPavlenko7610/UnityTemplates.git` beside Planet_IO if it is missing.
2. For a clean machine or CI, check out the commit recorded in `Tools/UnityTemplates.revision` in that checkout.
3. With Planet_IO's Editor closed, run
   `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Link-UnityTemplates.ps1` from the project root.
4. Open Planet_IO using Unity 7000.0.0a16 and let Package Manager restore `Packages/packages-lock.json`.

The script can be run repeatedly. It accepts `-TemplateProject` for another checkout and refuses to replace an existing
directory or unexpected link. Junction targets are ignored by Git; their folder metadata is versioned. Runtime and
required Editor folders are linked, including Haptics native plugins. Demos and library tests are excluded. CI must use
Windows and recreate the links before opening Unity.

## Ownership

Edits through `Assets/TemplateSources` change the shared UnityTemplates checkout immediately and can affect Night
Courier. Keep Planet_IO adapters and rules in `Assets/_Project`. Update the recorded library revision only after
validating the game against that revision. A revision file does not freeze uncommitted edits in the sibling checkout.

| Shared source     | Planet_IO responsibility                                      |
|-------------------|---------------------------------------------------------------|
| Foundation, Pause | Logging, lifetime primitives, solo pause integration          |
| SceneFlow         | Scene ids, scene catalog, NGO scene synchronization           |
| Settings          | Setting keys, audio/haptics application, last room preference |
| Localization      | Unity Localization provider, tables, font fallback            |
| Audio             | SFX catalog, game events, music selection and crossfades      |
| Haptics           | Game feedback and enabled setting                             |
| Addressables      | Preload policy and disposable music asset leases              |
| Save              | Profile schema, validation, legacy import, coalesced writes   |
| Startup           | Profile-before-content startup and profile flush at shutdown  |
| Attributes        | Inspector assignment and buttons                              |

The game keeps VContainer, NGO/UGS, AdMob, pools, network spawning, presenters and authored uGUI. The template Spawner
has no pooling/network support. Audio explicitly does not own music policy. Adding generic gameplay modules or a second
UI navigation system would not replace these responsibilities directly.

## Persistence migration

User settings retain their `PlanetIO.Settings.` prefix. The previous `PlanetIO.Room.Code` value is imported once into
the typed room setting when the new key is missing.

The profile is saved in slot `player-profile` under `Application.persistentDataPath/Saves` using RD.Save. On first load,
missing saves import nickname, preferred color, best score, selected skin and unlocked skins from the existing
PlayerPrefs keys. Those legacy keys are retained. Existing saves take precedence; invalid or unsupported saves fail
startup instead of silently resetting progress. Writes use detached snapshots and serialize/coalesce subsequent changes.
RD.Save supplies file replacement and backup recovery.

The old copied modules, custom Inspector helpers, and GameLogger have been replaced. Serialized script GUID references
in game assets point to the shared scripts while game asset GUIDs remain stable. The obsolete nested UnityTemplates
submodule is no longer a project dependency.
## Validation on 2026-11-11

Unity 7000.0.0a16: 174 EditMode tests and 12 PlayMode tests passed, including Android player-script compilation, profile import/recovery, solo game flow and room approval rules. The Addressables Android content build passed. All game ScriptableObjects load, the scene catalog is valid, and the five AudioCue assets resolve. The linked sources contain no duplicate asset GUIDs. Live UGS matchmaking between two real devices was not exercised.

Localization 2 retains the package table workflow and the project-owned provider. Obsolete LocalizationGroupSchema assets were removed while keeping bundle/update schemas and localization entries. Unity 7 plugin metadata and static lifecycle handling were updated for the existing no-domain-reload Play Mode configuration.