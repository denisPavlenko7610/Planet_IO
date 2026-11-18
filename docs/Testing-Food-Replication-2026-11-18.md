# Food replication verification ? November 18, 2026

## Scope and environment

Unity 7000.0.0a16, Windows Editor, independent verification copy of the project.
The final render-only repeat uses local port 27887 in the verification copy to avoid an unrelated
Editor session holding port 7777; the project default port is unchanged.
These checks validate behavior and protocol operation; they do not measure Android temperature,
sustained FPS or battery consumption. The original food sprite and palette remain in use.

## Results

- EditMode suite: 595 passed, 0 failed, including Android player-script compilation.
- GameFlow PlayMode suite: 79 passed, 0 failed, 1 skipped (all skins already unlocked in the test profile).
- Real UnityTransport/NGO test: 1 passed; one host and two remote clients in the same process,
  2000 ambient records, late join, movement deltas and generation-changing respawn converge.
  No food NetworkObjects are spawned. Food payload sent across the scenario: 108,161 bytes;
  no food-channel packets during the final idle interval. Transport headers and the rest of the game
  are excluded. Online host-recovery checkpoints remain separate periodic compressed snapshots.
- Final render-only repeat: 1 passed; captured `Logs/FoodData.Game.png` and visually confirmed food.
- Gameplay tests cover camera-local food views without food colliders/NetworkObjects, magnet motion,
  own-drop immunity, pickup, comet food return, boost/loot behavior and restored host-migration state.
- EditMode validation covers grid queries against brute force, edge coordinates, stale handles,
  exclusive claims, swept pickup, allocation-free warmed queries/motion, malformed binary messages,
  snapshot atomicity, revision gaps, stale epochs and binary migration data.

## Growth re-measurement

The nearest-food pilot ran for 600 simulated seconds at 8x, without opponents, boosts, events or perks.
This is a repeatable integration measurement rather than a human balance assessment.

| Mass threshold | Time (seconds) |
| --- | ---: |
| 0.35 | 7.3 |
| 0.80 | 36.1 |
| 1.40 | 67.3 |
| 2.00 | 104.8 |
| 4.00 | 295.1 |
| 8.00 | Not reached |

Final mass was 5.8372 from 0.0810; the player survived. Ambient candidate count remained 2000;
recorded movement speed remained 4.00. Nutrition, growth falloff and movement constants were not retuned.
The diagnostic report is in `Logs/GrowthPace-2000.txt`.

## Artifacts and remaining device work

NUnit reports are stored under `Logs/FoodData.EditMode.xml`, `Logs/FoodData.PlayMode.xml` and `Logs/FoodNetwork.xml`.
A physical-device test is still needed: compare CPU/frame time, GC, draw calls, traffic and a 15-minute
thermal run on the same device and settings. Cross-device host loss and reconnection also remain
manual acceptance checks; the automated migration test restores the serialized world locally.
