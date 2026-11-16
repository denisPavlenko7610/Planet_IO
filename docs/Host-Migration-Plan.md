# Keep a multiplayer match running after the host leaves

Status: required behavior; steps 1-5 are implemented in code, step 6 (independent-peer verification) is not done.
Reviewed November 16, 2026.

Implemented: versioned snapshot codec and `MatchMigrationStore` (Core), `MigrationWorldService` and the
`*.Migration.cs` capture/restore partials, `SessionMigrationDataHandler` for Multiplayer Services, and the recovery
path in `NetworkSessionService`. Verified so far: 5 EditMode tests (`MatchMigrationTests`) and one single-process
PlayMode test (`Migration_RestartsNgoAndRestoresWorldWithoutStartingANewRun`). Not verified: separate processes or
devices, real Relay, forced process termination, late join during recovery, two consecutive host departures.
`HostMigrationPeerProbe` (built from the `Planet IO/Tests/Build host migration probe` menu item) exists for step 6.

## Behavior before this work

The project uses Netcode for GameObjects 3.2.1 with Multiplayer Services 2.4.1 and Relay.
`UgsSessionConnector` configures `WithRelayNetwork` and a custom `NgoSessionNetworkHandler`.
It does not configure host migration or provide migration data.

When the host disconnects, `NetworkSessionService.OnClientDisconnected` handles the client's local
disconnect and calls `RecoverFromDisconnectAsync`. That method shuts down the session and returns
to the menu. Leaving the host's game scene therefore ends the active network game for its clients.

The installed Multiplayer Services source confirms that network migration returns immediately when
`HostMigrationHandler` is null. Its parameterless `WithHostMigration` implementation is conditional
on Netcode for Entities migration support. This NGO project needs a custom `IMigrationDataHandler`
and an explicit world restore path; enabling session owner reassignment cannot supply that state.

## Implementation plan

1. Define a versioned match snapshot and stable player identity based on the authenticated session player,
   separate from NGO client and object IDs. Include mass, position, movement, defeat/continue status,
   perks and pending choices, moons including durability and timers, bots, active food/drop budgets,
   comets, event state and clocks, Blitz timer, map seed and the shared room difficulty. Restore loot
   once and preserve run identifiers so migration cannot duplicate rewards.
2. Implement capture/restore APIs in the owning gameplay and application services. Validate schema,
   bounds and snapshot completeness; test serialization round trips and restoration independently of Relay.
   Pure snapshot rules belong in Core. Do not use reflection to write private network state in production.
3. Provide a custom migration data handler for the installed Multiplayer Services package. Upload bounded
   snapshots periodically and on graceful handoff, retain the latest usable revision, elect the next host,
   allocate a new Relay connection and restore the authoritative arena before accepting gameplay input.
4. Change the session state machine to distinguish migration from an unrecoverable disconnect. Keep a
   reconnecting client in a recovery view while election/reconnection is in progress. Wait for NGO shutdown
   completion before restarting it; the current network handler's StopAsync returns immediately.
   Rebind authenticated players to their restored planets instead of spawning duplicate new lives.
5. Preserve the room's difficulty and expose recovery status consistently to all peers. Keep the existing
   return-to-menu path only after a bounded recovery timeout or when no surviving host candidate exists.
6. Verify with separate processes/devices: graceful host exit, forced process termination, transport loss,
   late join during recovery, two consecutive host departures and one surviving player. Compare snapshots,
   loot nutrition, perks, moons, progression and event timers before and after migration. Verify ordinary
   client exit and intentional local shutdown still work.

## Acceptance criteria

Surviving players reconnect to the same match without starting new lives or losing progression. There is
exactly one authoritative world, no duplicate loot/rewards, and every surviving player's ownership is correct.
A graceful handoff should preserve the latest state; abrupt-loss rollback is bounded by the snapshot interval
and is measured in a two-peer test. A visible recovery period is preferable to continuing a disconnected copy
of the arena. The implementation is not complete until these cases pass with independent peers.
