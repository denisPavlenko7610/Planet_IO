# Audio

Portable cue-driven playback with pooled `AudioSource` voices, weighted clip variation, 2D/3D playback, transform following, fades, cooldowns, and bounded voice counts.

```csharp
AudioService audio = new(
    lifetimeOwner: transform,
    options: new AudioServiceOptions(maxVoiceCount: 32, prewarmVoiceCount: 8));

audio.Play(uiConfirmCue);                         // fire-and-forget 2D cue
audio.PlayAt(impactCue, hitPoint);                // stationary 3D cue
AudioVoiceHandle engine = audio.PlayFollowing(engineCue, shipTransform);
engine.Stop();                                    // uses the cue's fade-out
```

`AudioCue` owns reusable sound defaults: weighted clips, mixer routing, volume and pitch variation, loop, Unity priority, spatial settings, fades, cooldown, and maximum simultaneous instances. Per-call arguments are limited to position/follow target and volume/pitch multipliers.

The important rule: routine admission pressure returns an invalid handle. Cooldown ignores repeated starts, a full cue replaces its oldest instance, and a full global budget replaces the oldest least-important voice only when the incoming cue is at least as important. Invalid cue authoring still throws immediately. Check `IsValid` only when gameplay needs to retain and control a long-lived voice.

Call `SetPaused` to pause only voices owned by this service. `AudioListener.pause` is also respected; enable `Ignore Listener Pause` on cues such as menu UI that should continue through a listener pause. Route category volume and mute through authored `AudioMixerGroup` parameters rather than duplicating mixer settings in the service.

Dispose the service with its game/session owner. Open `../../../Examples/Presentation/Audio/AudioExample.unity` for the standalone example; it has no DI-container or other Unity Templates runtime dependency.

Use the service and its handles from Unity's main thread, like the `AudioSource` API they wrap.
