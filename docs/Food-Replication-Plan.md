# Food replication plan

Status: implemented on November 17-18, 2026; Editor validation passed: 595 EditMode tests, 79 gameplay tests and the host/two-client transport test. Per-food NGO objects have been removed.
The Game scene creates 2000 ambient table records. Migration format 2 uses fixed binary food records
and JSON metadata, compressed under the existing 120 KiB recovery budget. Temporary drops and loot occupy additional slots,
matching the current expandable pool. 2000 is not a hard limit on all food.

## Architecture

Store food in one authoritative array and spatial grid on the server. Clients keep a read-only replica
and render visible items locally. The graphical host uses the same renderer; a dedicated server creates
no food views. Binary encoding is the network representation, not a file read every frame.

The expected benefit comes from removing individual object lifecycle, transforms, physics proxies and
network bookkeeping. Binary messages alone would leave much of that CPU work. Do not replace 2000
NetworkObjects with 2000 independently updated NetworkVariables. Measure the actual savings on a device.

Replicate the whole table to clients at connection/rebaseline and cull rendering locally. At 2000 mostly static items,
this avoids region-subscription races. Use the grid for simulation queries and view selection. Add network
region subscriptions only if measured traffic justifies them.

## Authoritative state

- Use slot IDs with generation counters. Reusing a slot increments its generation so stale consumption,
  expiry and network messages cannot affect replacement food. Change the match epoch after migration.
- Record position, physical size, nutrition, value multiplier, kind, active state, density-field cell,
  dropper identity, immunity deadline and expiry deadline. Keep full precision in the simulation.
- Preserve ambient food, boost drops, loot and comet-relocated ambient food as distinct kinds/budgets.
  The server chooses respawns, claims records atomically and awards mass once. Clients cannot supply rewards.
- Replace food Physics2D queries with grid queries and equivalent geometry checks, including body pickup,
  inside-body sweep, magnet reach/speed, immunity, bot feeding and smart respawn. Cover cell boundaries,
  dense piles and fast crossings; use swept checks where needed rather than reducing simulation frequency.
- Update grid membership when magnets, comets or respawns move food. Keep centralized heap-based expiry.

## Replication

Use one NGO CustomMessagingManager service with FastBufferWriter/FastBufferReader. Food has no individual
RPC, NetworkTransform or NetworkObject. Verify message sizing against the installed transport configuration.

1. A snapshot header identifies protocol version, epoch, snapshot ID, baseline revision R, server time,
   record count and chunk count. Freeze a consistent table view at R.
2. Send bounded binary chunks with snapshot ID and chunk index. Validate count/checksum before exposing
   the replica. Explicit chunks avoid one huge reliable fragmented message blocking gameplay traffic.
3. Queue changes newer than R during snapshot delivery and replay them afterward. Bound this queue and
   restart the snapshot on overflow. Late join must neither miss changes nor apply them twice.
4. Delta batches carry epoch, revision range, time and typed operations: upsert, consume/respawn, remove
   and attraction start/stop. Include slot and generation. Treat ambient consume plus replacement as one
   logical operation so a delayed removal cannot hide the new food. Reject stale messages and detect gaps.
5. Send structural changes reliably and in order, initially batching at 10 Hz as a tuning starting point.
   Server simulation and pickup retain their current schedule. Coalesce repeated state writes while
   preserving required feedback events. Empty batches send nothing.
6. Magnet motion needs explicit handling: send target/start/stop and a baseline, animate locally, and send
   corrections for actively moving food when necessary. Final pickup remains server-authoritative. If
   motion corrections use unreliable delivery, include generation and sample sequence; they must never
   recreate removed food. Test contested magnets before enabling visual prediction.
7. Add bounded periodic state-hash checks and a repair/resnapshot path. Track actual bytes, changes,
   corrections and repairs. Do not assume reliable delivery solves snapshot races or reconnects.

The implementation keeps full float coordinates; quantization is deferred until measured bandwidth
justifies its error budget. If added, quantize serialized coordinates only, after testing error against world bounds and the smallest pickup
radius. A signed 16-bit grid at 1 cm covers roughly +/-327 world units and fits the current world
(x: -220..220, y: -136..160); retain range validation if the world expands. Keep nutrition as float initially to avoid changing growth.

Client visual records currently use 26 bytes (handle, position, size, value, flavor and active flag).
Packets hold at most 32 records and are at most 863 bytes of application payload. A 2000-record initial
snapshot is 54,013 bytes including its application headers. The real NGO integration test measured
108,161 payload bytes for two initial client snapshots plus the tested movement and respawn deltas.
The food channel sends no idle packets. Existing online host-recovery checkpoints remain complete
compressed snapshots at the existing 2-second interval; they are separate from this delta channel.
Migration records use 50 bytes before compression and retain full nutrition, expiry/immunity, generation
and tombstones. Snapshot schema version 2 rejects incompatible pre-table recovery payloads.

## Client rendering

Query the camera rectangle with a margin covering visual food scaling and magnet animation. Use a bounded
pool of local SpriteRenderers for visible food only, sharing the Gameplay atlas/material. Static views
update only when state, visibility or the quantized global visual scale changes. No per-food Update,
Collider2D or Rigidbody2D is needed. The server keeps all records regardless of visibility.

If visible-object overhead remains significant, benchmark chunk meshes with vertex colors and dirty-chunk
updates; keep attracted items in a small dynamic batch. GPU instancing is another option requiring a
compatible shader, platform fallback and correct transparency sorting. Cull individual records first:
Unity's instanced mesh culling operates on groups. An atlas does not guarantee a single draw call.

## Integration map

| Current implementation                                       | Replacement                                                       |
|--------------------------------------------------------------|-------------------------------------------------------------------|
| FoodSpawner, Spawner and food pool                           | Array allocation, generations, respawn and lifetime service       |
| Player.Combat.AttractFood                                    | Grid search, authoritative motion and replicated attraction state |
| PlanetScale.FoodSweep and food collision branches            | Grid pickup with existing growth rules                            |
| EnemyMovement food search                                    | Grid queries preserving targeting and awareness                   |
| Player.Respawn food-density queries                          | Spatial density queries                                           |
| FoodFieldMap, comet trail, boost drops and LootDrop          | Stable food handles instead of component references               |
| Food.PlayEatenEffect                                         | Confirmed consumption events and local relevance filtering        |
| MigrationWorldService, Food.Migration, FoodSpawner.Migration | Table snapshot and grid/expiry reconstruction                     |
| Planet loot IDs and dropper immunity                         | Food handles independent of NGO IDs; migrated dropper mapping     |
| FoodViewScale and golden tint                                | Renderer state only; physical size/nutrition unchanged            |

## Delivery sequence (implemented; device measurement remains open)

1. Measure the existing 2000-item baseline. Implement the table, handles, spatial index, atomic claims and
   expiry under Runtime/Core. EditMode tests compare spatial queries against brute force, including edge
   cells, dense piles, stale generations and fast crossings.
2. Route every food consumer through storage/query contracts. Preserve growth, magnet, golden food,
   comet trails, boost drops, loot and bot/respawn behavior. Avoid two authoritative mass-award paths.
3. Add binary codec, snapshot/delta replica, late join, repair and local views. Compare old/new development
   configurations with a host and second client before disabling legacy replication.
4. Integrate migration: retain generations, field occupancy, revisions and remaining timers, restore food
   handles and dropper/loot references, advance epoch and rebaseline clients. Keep the documented host-loss
   rollback window. Remove food network components, colliders and prefab registrations after integration
   checks pass; update GDD implementation status.
5. Profile the device again. Choose pooled views versus chunk meshes/instancing from measurements, then
   tune message rate/precision. Re-measure GDD section 4 growth milestones after the 1800-to-2000 increase.

## Acceptance checks

- Exactly 2000 ambient records; temporary drops use extra slots and expire correctly.
- No per-food NetworkObject, NetworkTransform, Rigidbody2D or Collider2D after cutover. No steady-state
  managed allocation in food simulation, serialization or view updates after warm-up.
- Two clients converge after late join, reconnect, duplicate/reordered/stale messages and gap repair.
  Contested pickup awards one winner. Migration preserves active magnets, expiry, loot and comet trails.
- Preserve nutrition, falloff, golden rewards, boost drops, bot feeding, immunity and view scaling.
- Record CPU p50/p95, frame p50/p95/p99, physics time, GC bytes/frame, draw calls, memory and food bytes/s
  during idle, feeding, boosting, comet and late-join scenarios. Label Editor results separately.
- Compare 15-minute runs on the same device/settings/brightness/ambient conditions. Target sustained
  > =55 FPS per GDD and record thermal throttling/temperature when accessible. Packet-size estimates and
  desktop FPS do not establish a thermal improvement.

## References

- [Unity custom messages](https://mp-docs.dl.it.unity3d.com/netcode/current/advanced-topics/message-system/custom-messages/)
- [Unity RenderMeshInstanced](https://docs.unity3d.com/ScriptReference/Graphics.RenderMeshInstanced.html)
## Verification record

See [Food replication verification ? November 18, 2026](Testing-Food-Replication-2026-11-18.md) for
the host/two-client transport run, gameplay and migration checks, growth re-measurement and remaining
physical-device acceptance work.
