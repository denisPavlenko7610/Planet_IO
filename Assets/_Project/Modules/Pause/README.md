# Pause

A pause-ownership service. The game stays paused while at least one caller owns a pause handle.

## Quick Start

```csharp
using var pauses = new PauseService();
using var timeScale = new UnityTimeScalePauseController(pauses);

using IDisposable inventoryPause = pauses.RequestPause("inventory");
```

Each caller must keep and dispose its own handle. Reasons are for diagnostics only.

## Notes

`UnityTimeScalePauseController` is optional. It sets `Time.timeScale` to zero and restores the previous value. Input, audio, and other pause behaviors are project-specific.

See [`Examples/Application/Pause/README.md`](../../../Examples/Application/Pause/README.md) and open `PauseExample.unity` for a complete setup.
