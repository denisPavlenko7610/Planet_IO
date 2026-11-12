# Planet_IO

IO game inspired by Slither.io, but with a different growth and survival loop.

Players grow only by eating points, become slower as they get larger, and must trade speed, mass, and positioning to survive.

![image](https://user-images.githubusercontent.com/13468920/204050922-29bc7332-afac-4204-9f4d-99551a3ba36b.png)

## Technologies

- Unity 7 (7000.0.0a16): core engine, scenes, physics, rendering, and gameplay loop.
- Unity Netcode for GameObjects: multiplayer synchronization, player spawning, and networked gameplay state.
- Addressables: local content catalog and preloaded audio bundle.
- VContainer: lightweight dependency injection for scene scopes and service composition.
- Awaitable: async scene flow and gameplay routines without UniTask.
- Input System: keyboard, mouse, and touch input handling.
- URP: rendering pipeline for 2D visuals and post-processing setup.

## Design

- [Food system GDD](Docs/Food-GDD.md): pellet density, food fields, size tiers and bot behaviour.

## Shared modules

Planet_IO links selected runtime/editor folders from the sibling UnityTemplates checkout through Windows junctions. Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Link-UnityTemplates.ps1` before opening a fresh checkout. See [integration and migration details](Docs/Template-Sources.md).
