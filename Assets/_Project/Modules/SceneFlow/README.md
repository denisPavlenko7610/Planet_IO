# Scene Flow

Small scene-loading service for Unity. `SceneCatalog` maps stable IDs to scene assets; `SceneFlow` handles single and additive loading, unloading, active scenes, progress, and overlapping-operation protection.

```csharp
var sceneFlow = new SceneFlow(catalog);

await sceneFlow.ChangeSceneAsync("game", cancellationToken);
await sceneFlow.LoadAdditiveAsync("hud", cancellationToken: cancellationToken);
await sceneFlow.UnloadAsync("hud", cancellationToken: cancellationToken);
```

Use `ISceneTransition` when single-scene changes need a cover/reveal effect. `ISceneOperationScope` is optional and can block input or pause gameplay for the duration of any real load operation.

Only one operation may run at a time. Unity scene loads cannot be safely cancelled after they start, so cancellation is checked before starting and while running custom transitions.

Create a catalog through **Assets → Create → Unity Templates → Scene Flow → Scene Catalog**, give every scene a stable ID, and include its assets in Build Settings.

See `../../../Examples/Application/SceneFlow/README.md` for the persistent VContainer setup and an interactive single/additive-loading scene.
