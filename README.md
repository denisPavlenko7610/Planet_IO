# Planet_IO

IO game inspired by Slither.io, but with a different growth and survival loop.

Players grow by collecting food and absorbing smaller planets. Movement speed stays independent of size; boost, moons and gravity add tactical choices.

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

- [GDD and development plan](docs/GDD.md): vision, moons, gravity, evolution tiers, progression and roadmap.

## Shared modules

Planet_IO links selected runtime/editor folders from the sibling UnityTemplates checkout through Windows junctions. Run `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Link-UnityTemplates.ps1` before opening a fresh checkout.
