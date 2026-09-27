# Settings

Typed user settings with defaults, normalization, codecs, caching, change notifications, and replaceable storage.

Depends on `Core/Foundation`; copy Foundation alongside this module.

```csharp
static readonly SettingKey<float> MusicVolume = new(
    "audio.music-volume",
    0.8f,
    SettingCodecs.Single,
    Mathf.Clamp01);

var settings = new SettingsService(new PlayerPrefsSettingsStore("my-game."));
settings.Set(MusicVolume, 0.5f);
settings.Flush();
```

Declare each `SettingKey<T>` once and reuse it everywhere. Use `PlayerPrefsSettingsStore` for small persistent preferences and `MemorySettingsStore` for tests, previews, or temporary profiles.

`BeginBatch` batches final notifications; it is not a storage transaction. Call `Flush` at deliberate persistence points such as Apply, app pause, or shutdown. Save-game progression belongs in the Save module.

See `../../../Examples/Application/Settings/README.md` for VContainer registration and the interactive scene.
