# Foundation

Optional Unity 6 primitives for game code: `Result<T>`, disposable ownership, `AsyncGate`, validated ids, GZip compression, and small guards. The assembly has no project-module dependencies.

```csharp
Result<PlayerProfile> result = repository.TryLoad();
if (result.IsFailure)
    Debug.LogWarning(result.Error.Message);
```

Use `CompositeDisposable` to own subscriptions and `AsyncGate` to serialize asynchronous operations. Use `GZipHelper` for portable GZip compression of byte arrays. Clocks and countdowns live in the separate `Core/Timers` module.

## Package boundaries

- `UnityTemplates.Foundation` contains runtime primitives only.
- `Examples` is a separate `UnityTemplates.Foundation.Examples` assembly. It also contains the shared UI runner used by older module examples; production code should not reference it.
- `Core/Attributes` and `Core/Extensions` are separate sibling modules. Copy either without Foundation when that is all a project needs.

`Application/Localization` references `AsyncGate` from this module. You can copy only the root `.cs` files and `UnityTemplates.Foundation.asmdef`, or remove this module entirely. See `../../../Examples/Core/Foundation/README.md` for the demo.
