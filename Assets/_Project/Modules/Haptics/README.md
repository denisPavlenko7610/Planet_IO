# Haptics

Portable Android/iOS haptics with semantic presets, custom duration/intensity, a global user toggle, stop support, and observable fallback behavior.

```csharp
Haptics.IsEnabled = playerSettings.HapticsEnabled;
Haptics.Play(HapticPreset.Selection);
Haptics.Play(HapticPreset.Success);
Haptics.Vibrate(durationMilliseconds: 120, intensity: 0.7f);
```

Prefer semantic presets and map them from game events in one application-owned facade. Use custom vibration only when duration/intensity is part of the experience. `IsEnabled = false` makes play calls no-ops; `Stop()` remains available for an already-running custom vibration.

The `Handheld.Vibrate()` fallback is intentionally retained. It covers the Editor, missing hardware, and Android/iOS native failures. Unity's Android build scan also sees that call and automatically adds `android.permission.VIBRATE`; this is a normal manifest permission and requires no runtime prompt. If a custom Android build pipeline replaces Unity's generated manifest instead of merging it, verify the final manifest.

Copy the complete module so the iOS native bridge under `Plugins/iOS` is included. See `../../../Examples/Presentation/Haptics/README.md` and `HapticsExample.unity` for a VContainer production mapping example.
