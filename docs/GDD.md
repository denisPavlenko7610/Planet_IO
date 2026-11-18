# Planet IO: GDD and development plan

Updated: November 18, 2026 (qualified leaderboard records, cloud upload queue and cleanup, section 7.2). Earlier sections:
November 13, 2026. Implementation status was checked against the working tree, including uncommitted changes.
Code
being present does not mean a mechanic is verified; earlier test results are in the
[verification history](#appendix-b-verification-history-and-release-checks). Rows about size, speed, camera, food,
magnet and boost were updated on
November 16, 2026. The reference for game-feel decisions is Slither.io; the rule is recorded in
[AGENTS.md](../AGENTS.md). The scope of the update was reviewed on November 16, 2026: what we take, defer and drop is in
section 2.1.

This document describes the target design. Section 2 records the current implementation; deviations from the target are
marked next to each mechanic. Balance numbers and schedules are hypotheses for playtests, not measured facts.

## 1. Summary

The core is "agar.io in space": a planet eats an opponent when its mass is at least 1.08 × the victim's mass. Moons and
gravity add positional play and a way to hurt a bigger planet.

We keep the controls, food, bots, rooms and skins. Three main growth mechanics:

1. **Moons**: orbiting satellites instead of a tail. They give a small planet teeth and a big one armor.
2. **Gravity**: big bodies pull small ones; small ones use this for a slingshot.
3. **Evolution**: planet tiers (Asteroid → Planet → Gas giant → Star), with a pick of 1 of 3 perks on each tier.

Around them: cosmic events, a leader beacon with a bounty, and progression between runs (rank, stardust, missions).

One-line pitch: *"Slither.io where your tail is moons and your weapon is gravity."*

## 2. What exists now (facts from the code)

| Area            | Now                                                                                                                                                                                                                                  |
|-----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Mass            | `Capacity` 0.08 → 8.0, start 0.08; scale = 0.08 × √(mass / 0.08) (`PlanetSizeRules`): size varies 10× across the full mass range                                                                                                     |
| Eating          | contact, if mass ≥ 1.08× the victim's; base absorption 40%, loot 60%; the Predator perk and bounty raise absorption                                                                                                                  |
| Speed           | player 3.6 (boost 7.2), bot 3.0; no mass penalty: a big planet is neither faster nor slower than a small one (as in Slither.io)                                                                                                      |
| Camera          | base size 3.6, zoom = 3.6 × (mass / 0.08)^0.10 with delayed growth response, look-ahead of 16% of the size along the heading, +8% size while boosting                                                                                                         |
| Boost           | 0.004 mass every 0.15 s, drops food worth 80% of the cost beyond the body edge (body radius + 0.3); the owner's magnet ignores its own drop for 1 s                                                                                  |
| Border          | movement is blocked at the edge (`WorldBounds.KeepInside`); entering the zone costs max(regular boost cost, 8% mass); red vignette when close                                                                                        |
| Food            | 2000 server table records; binary snapshots and 10 Hz deltas; client-only pooled views, ambient nutrition x3, falloff 0.5, 16 migrating fields (45 s), golden 4% (×8); client-side visual ×1.2 × zoom^0.7, capped at ×3 (`FoodViewScale`), collider unchanged |
| Enemies         | pool of 16 bots, 3 personalities (Cautious/Balanced/Hunter), 4 intents (Roam/Forage/Hunt/Evade)                                                                                                                                      |
| Comets          | pool of 30, speed 2.5–3.5, damage 50% of victim mass; permanent names, a visual tail, a limited food trail (new, needs playtesting)                                                                                                  |
| Network         | NGO + UGS Sessions, rooms by code (default 8, max 16), owner-authoritative movement                                                                                                                                                  |
| Meta            | 14 languages, skins (some for ads), continue via rewarded (50% mass), leaderboard, minimap                                                                                                                                           |
| Tiers and moons | 4 tiers, base slots 0/2/4/6; server-side strikes, comet interception, ring clashes; HUD and camera account for moons                                                                                                                 |
| Gravity         | pull and slingshot in `PlanetGravity`; the owner simulates the player, the server simulates bots                                                                                                                                     |
| Perks           | 9 perks, server-side offer of 3 cards with a timer, pick screen in the HUD; compact top-docked choices and colored icon badges under the score                                                                                       |
| Leader          | ring, minimap highlight, bounty +25%; minimum mass 0.4, refreshed every 0.5 s                                                                                                                                                        |
| Progression     | rank (30), stardust, 3 missions a day, streak, results screen, FTUE, 12 skins (4 free); each new rank pays stardust 5 + rank                                                                                                         |
| Respawn         | 8 random candidates, the best by "food nearby + distance from bodies that can eat you" wins (`RespawnRules`)                                                                                                                         |
| Movement arrow  | open chevron, constant on-screen size (1.8% of screen height), outside the visible body with a half-marker gap; does not grow with the planet (`DirectionArrowRules`)                                                                |
| Main menu       | on screen: nickname and planet (profile chip), Play, Blitz, Online, mission and settings icons; nickname and skins live in the Profile window; quick play, room and code live in the Online window                                   |

### 2.1. Scope decisions (November 16, 2026)

Selection criteria. We take what (1) protects the player's first minute, (2) is needed to ship the update safely, or (3)
is
cheap and noticeably changes the feel. We defer anything that depends on art, plugins or metrics we do not have yet. We
drop what duplicates something that already works. The feel reference is Slither.io ([AGENTS.md](../AGENTS.md)).

**Taking into the update**

| What                                                                   | Why                                                                                                                                      | Phase |
|------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|-------|
| Full PlayMode run without failures, moon-reset regressions             | We cannot ship with a known death/continue defect; the reset exists in code, the tests are not confirmed                                 | 5     |
| FPS and traffic measurement on a weak Android device with 2000 food    | Data-only food is implemented; sustained device performance still needs measurement                                                                        | 5     |
| Host/client check: replication, late join, reconnect                   | Rooms by code are part of the update; the 13 Nov report marks this Not Run                                                               | 5     |
| Smart respawn                                                          | The first minute after death decides "play again"; it used to be a random point, often empty or next to a big body                       | 6     |
| On-device tuning: camera, speed, food, magnet, boost cost, growth pace | All changed on 16 Nov without hands-on checks; boost now costs the full mass                                                             | 6     |
| Active perk badges in the HUD                                          | A perk with no indicator cannot be read, so counter-play is impossible                                                                   | 6     |
| Rank reward: stardust `5 + rank`                                       | The "death is a step forward" pillar; ranks without a prize are empty; about 600 over 30 ranks, less than half the price of 8 paid skins | 6     |
| Three new sounds: moon hit, tier-up, event warning                     | They used to be Kill/Hit at another pitch; sound is the main combat feedback                                                             | 6     |
| Threat ring: shape coding for color-blind players                      | Cheap; color used to be the only carrier of meaning (done without a setting)                                                             | 6     |
| AAB build, device smoke test, store listing and screenshots            | Release                                                                                                                                  | 7     |

**Deferring (after the update ships)**

| What                                               | Why                                                                                                |
|----------------------------------------------------|----------------------------------------------------------------------------------------------------|
| Server-side movement validation and Play Integrity | Needs a trusted backend; the game is peer-hosted, so no device can act as the server (section 7.2) |
| Cosmetics beyond skins, shop screen                | Needs art; for now stardust is spent on the 8 paid skins                                           |
| Reroll the 3 perk cards for rewarded               | A monetization experiment, after the first perk data                                               |
| Leader pointer                                     | The minimap beacon and the ring already exist; Slither.io has no arrows                            |
| Leader "burnout"                                   | Enable only if playtests show a snowball                                                           |
| Supernova on a tier III+ death                     | Decoration; events already produce loot rings                                                      |
| Deeper bot AI: slingshot, avoiding wells           | Bots already avoid moons; further work depends on playtest data                                    |
| External analytics SDK                             | Play Console gives D1/D7 and crash-free; funnels stay local (`LocalAnalyticsService`)              |

**Dropped from the plan**

| What                                           | Why                                                                                                                      |
|------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| "Solar system" teams                           | A separate game in scope: matchmaking, team balance                                                                      |
| "Solar flare" event                            | Three event types are enough                                                                                             |
| Destroying enemy moons with boost              | Moons already break on ring clashes                                                                                      |
| Gravity strength as a fraction of target speed | 1.4 u/s at speed 4.8 is about 29% against a 35% goal; retuning does not pay off                                          |
| Push-back at the border                        | Movement is already blocked, the mass loss and the vignette work; a push would be a second punishment for the same thing |
| Comet-name toggle                              | Names are now permanent; no separate setting is needed                                                                   |

Everything else in this GDD stays unchanged. The roadmap is in section 14.

**Status on November 16 (evening).** Implemented in code, not yet checked on a device: smart respawn, perk badges, rank
reward, a thick danger ring (shape coding instead of a color-blind setting), the small movement arrow, the main menu
split
into windows, three new sounds (Kenney, CC0: moon hit, tier-up, event warning), and the comet-name setting removed
(names
are always shown). Not done: Android measurements and host/client checks. Tests: EditMode passed 413 of 413 before the
comet changes. After these changes, 60 selected EditMode tests and 7 selected PlayMode tests passed (see section 6).
The last full PlayMode run passed 53 of 55, and `World_WormholeTeleportsOncePerCooldown` fails
regardless of speed.

## 3. Vision and pillars

Platform: Android, played with one thumb, sessions of 5–10 minutes, 3–5 runs in a row.

1. **Clear in 5 seconds.** Drag a finger, eat smaller ones, run from bigger ones. Everything else unlocks as you grow.
2. **Every decision costs mass.** Boost, perks, the border, comets: everything is measured in mass, and mass = size =
   score = vulnerability.
3. **The small can beat the big.** If that is impossible there is no drama and no viral clips.
4. **Death is a step forward.** After every run the player sees progress (XP, missions, unlocks) and taps "play again".
5. **Readable on 6 inches.** Silhouette, color and the threat ring say everything without text.

## 4. Game loop

| Scale        | What the player does                                   | Reward                                  |
|--------------|--------------------------------------------------------|-----------------------------------------|
| 5–30 s       | Moves to food, avoids big planets, risks boost         | Mass growth, a sense of motion          |
| 1–3 min      | Reaches a tier, picks a perk, hunts or hides           | New moon, new perk, camera scale change |
| 5–10 min     | Becomes the leader or dies                             | Bounty, loot, run result                |
| Between runs | Looks at the results, spends stardust, takes a mission | Rank, cosmetics, daily streak           |

Food/comet balance revision (November 16, 2026): reduce ambient food from 2250 to 1800 (-20%) and
multiply regular and golden food nutrition by 2 for both players and bots. Golden food remains 4% at x8
relative to regular food; boost drops and kill loot retain their exact mass budgets. This raises the total
ambient mass budget by 60% while clearing some visual clutter. As in Slither.io, feeding must show visible
progress; faster growth is the user's explicit preference. Movement, magnet and size curves stay unchanged.
Re-measure the section 4 milestones with the controlled playtest and retain the human-play caveat.

Maximum mass revision (November 16, 2026): raise the player and bot cap from 2.0 to 8.0.
With the existing area-based curve, the maximum body diameter doubles relative to the old cap and is
10 times the starting diameter. Mass 2.0 becomes an intermediate growth milestone; tier IV still starts
at 1.4 and retains six base moon slots. Keep the camera curve, movement speed and growth falloff.
This follows Slither.io's continued growth after the early progression milestones; the finite cap is a
practical match-size bound. Check camera framing and repeat the growth probe at the expanded cap.

Target pace to verify: mass 0.35 in about a minute, 0.8 in 3–4 minutes, 1.4 in 6–8 minutes, 2.0 in 8–10 minutes of
continuous feeding. Tier-ups every 1–3 minutes should give a regular perk pick.

November 16 camera/perk verification: 53 selected EditMode and 5 selected PlayMode checks passed.
A controlled nearest-food probe reached masses 0.35 / 0.8 / 1.4 in 21.6 / 106.4 / 242.8 simulated seconds,
then plateaued at 1.457; the pilot did not establish continuous feeding to 2.0. This does not validate the
human-play pace targets. Details and
limitations: [growth verification notes](#appendix-b-verification-history-and-release-checks).
The later food/comet revision passed 89 selected EditMode and 6 PlayMode checks: its cap-2 probe reached
2.0 at 282.9 s. With cap 8, 35 additional EditMode checks passed and a repeat probe reached 0.35 / 0.8
at 12.3 / 44.6 s before plateauing below the cap at 1.0601; late-game feeding remains unverified.
Both probes use randomized fields and cannot establish human-play milestone averages. A later instrumented run
explained the plateaus: the pilot stalls on a food that lies inside its body but was never eaten (no trigger enter
event), not on a global food or speed problem; see Appendix B for the probe method and limitations. The server-side body
sweep
described in section 5 removed these stalls: four probe runs showed none and reached masses of 3.75 to 4.20 at 600 s.

## 5. Comparison with Slither.io

Movement indicator revision (November 17, 2026): use a small open chevron with two strokes,
soft white at 75% opacity and a subtle dark edge. Its longest dimension stays at 1.8% of screen
height through growth, camera zoom and boost. Place its center beyond the larger of the body collider
and visible sprite radius (including 12% outline clearance), plus half the marker and a half-marker gap.
Unlike Slither.io, the planet has no head to communicate its heading, so retain this minimal direction cue.
The marker rotates with movement; it does not pulse or enlarge during boost. This is a visual-only change.

Food replication implementation (November 17-18, 2026): replace per-pellet network objects with a server-owned
FoodWorld table and spatial grid. Keep 2000 ambient slots and separate temporary drops. Every record has
a generation-qualified handle; claims, respawns, expiry, comet relocation and magnet motion mutate the table.
Clients receive a versioned binary snapshot followed by bounded reliable delta batches at 10 Hz; only
changed records are sent. A scene replication service requests a new baseline after reconnect/migration,
rejects stale epochs/revisions and repairs gaps. Full-world replication is deliberate at this scale;
camera culling remains local. Render only visible food through a shared sprite/material and pooled local
views, including on the graphical host. Dedicated servers create no food renderers or food physics bodies.
All food queries (body pickup, inside-body sweep, magnet, bots and smart respawn) use the grid.
Magnet motion stays within the arena bounds; coordinates keep full float precision on the wire to avoid
quantization changing collection geometry. Client records use 26 bytes plus bounded packet headers.
Magnet positions remain authoritative and replicate only while changing; clients interpolate corrections.
Migration format version 2 stores binary food records alongside JSON metadata; incompatible older snapshots
are rejected. Host migration stores records, generations, field occupancy, remaining expiry/immunity and dropper/loot
references independently of NGO object IDs; restored worlds start a fresh replication epoch.
Movement, growth, food sizes, nutrition, field distribution, magnet reach/speed and the 2000 ambient count
remain unchanged. Re-run growth measurements because query scheduling and magnet integration change.
Implementation and verification status is tracked in [Food Replication Plan](Food-Replication-Plan.md).

Balance and damage feedback revision (November 17, 2026): player movement is 4.0 u/s, boost 8.0,
and the base bot speed is 3.0. Speed remains independent of mass; perk and personality modifiers remain.
Gravity pull is reduced proportionally from 1.4 to 1.167 u/s so slowing movement does not strengthen traps.
Ambient food nutrition increases another 50% (the base multiplier becomes x3); boost drops, kill loot and
comet food retain their exact budgets. Ambient growth falloff becomes 0.5 instead of 1.0, keeping food
useful late in a run. These are tuning hypotheses: measure the section 4 milestones again; the first
evolution target is 45-60 seconds of human active feeding, with noticeable growth within 10-15 seconds.
The area-based body curve and mass cap 8 stay; camera growth exponent becomes 0.10 instead of 0.15,
preserving moon framing and delayed zoom. Doubling mass gives about 32% more settled screen diameter.
The existing 10x physical diameter range should be assessed before expanding the cap to 16.

Moon counterplay: a core hit removes 10% of the victim's current mass (minimum 0.004), with the existing
one-second shared immunity. Moons have two durability and take 16 seconds to regrow. A ring clash spends
one durability on each contacting moon, publishes broken moons immediately, and still knocks both bodies
back without core damage. Only contacting moons take damage. This fixes the previous implementation where
clashes never wore down the defensive ring. It intentionally adapts Slither.io's positional vulnerability:
larger bodies retain a direct-contact advantage, but cannot keep an inexhaustible ring by colliding with rivals.
Medium balanced/hunter bots and Hard bots may make bounded moon raids on larger targets up to 3x their mass
when their moon reach extends beyond body contact. They retreat after taking damage, exhausting their moons,
or committing for 1.5 seconds, then wait 4 seconds before another raid. Easy and cautious Medium bots keep
their defensive behavior. Hazards and spawn protection take priority. Test a smaller planet's actual ability
to land hits through broken rings; do not treat increased damage as proof of competitive balance.
This brings moon attack AI into this update's scope. Leader burnout remains deferred.

Damage feedback is driven by confirmed server damage, separately from boost expenditure and capacity resets.
Local hits produce a brief red edge flash over the existing border warning, a restrained camera shake scaled
to view size, and a red floating negative score number at the planet. Numbers use the same mass-to-score
conversion as the HUD, rise, pop and fade on unscaled time, and coalesce rapid hits to avoid clutter.
The existing UI animation loop supplies easing; no tween package is currently installed. Damage also keeps
the existing hit sound and haptics. Spawn/continue resets and boost expenditure must not trigger hit feedback.
Clear transient feedback when changing player, reviving, disabling the HUD or opening results.
Verification includes real comet/moon/border hits, healing and boost exclusions, late-game camera framing,
growth pace, rule tests and PlayMode combat. Human/mobile feel and the tier-II vs tier-III 1-in-5 goal still
require hands-on playtesting.

Verification: 534 project EditMode checks passed. Selected PlayMode checks passed for damage feedback,
moon clashes/strikes/protection/continue, bot raid retreat, difficulty damage, camera growth and actual movement.
The 600-second feeding probe ended at mass 5.423; milestones and limitations are recorded in
[balance verification report](Testing-Balance-2026-11-17.md). This verifies implementation, not human competitive
balance.

Appearance and localization revision (November 17, 2026): remove manual planet color selection from the
Profile window, profile persistence and cloud sync. Player skins render with a neutral white base tint,
including after respawn and host migration; temporary boost and protection effects remain. Old saved
color choices are ignored without resetting nickname, skins or progression. Skin selection remains the
cosmetic control, adapting Slither.io customization to textured planets whose artwork is damaged by tinting.
Localized buttons keep horizontal space inside their artwork and wrap within a readable bounded font size.
Settings labels wrap and rows grow; controls must leave space for longer translations. Profile, online
and mission windows scroll when their content exceeds the available height. The results panel wraps its
columns and scrolls on narrow screens.

Verification: 1,134 localized button cases fit at a readable 20 px font in Editor layout checks across
14 locales and 1280x1080, 1280x720 and 720x1080 viewports. Screenshots confirmed the Russian settings
and French narrow results layouts. Localization, menu structure, profile persistence/recovery, cloud
save and remaining owner-RPC checks passed. The PlayMode spawn-protection scenario passed with a
neutral white body tint. Android hardware and independent network peers were not checked for this revision.

Bot difficulty (November 16, 2026): Settings offers Easy, Medium and Hard, persisted with Medium as the default.
In SinglePlayer (including solo Blitz), changes take effect on the next AI decision. In multiplayer, the host's
selection at room creation becomes a shared room property; joining clients cannot override it. Online settings
show that shared value with the selector locked for the current match. The host may choose a new value before
creating the next room.
This explicit difficulty choice adapts Slither.io's opponents to a primarily solo audience.
Easy bots react every 0.75 s, have shorter awareness, do not lead targets and use brief defensive boosts.
Medium bots react every 0.4 s, lead targets slightly and boost to escape or close a safe pursuit.
Hard bots react every 0.2 s, look farther, intercept targets and use longer, more frequent tactical boosts.
All difficulties retain the same base movement speed and existing personality variety; mass never increases speed.
Bots evaluate other bots as prey/threats as well as visible, unprotected players, prioritize danger over feeding,
avoid borders and prefer nutritious nearby food. They cannot target hidden planets or spawn-protected players.
Boost replaces the previous free hunting/evasion speed multipliers: 2x speed, 0.004 mass per 0.15 s,
80% returned as food behind the bot, with the same own-drop magnet grace and starting-mass floor as players.
Boosting is bounded by duration/cooldown and reserve mass; hunting boosts stop if the prey is no longer edible.
The visual boost tint is replicated. Respawn clears boost and tactical state. Re-measure section 4 growth pace
after this speed/food economy change; automated checks alone do not validate human-play milestone targets.

Difficulty damage revision (November 16, 2026): Easy/Medium/Hard multiply solo-player incoming damage from
comets, moons, borders and map hazards by 0.75/1/1.25. Boost cost, eating eligibility, mass-to-speed rules,
spawn protection and healing remain unchanged. Bots take 1.25/1/0.85 incoming damage and deal 0.8/1/1.2 moon
damage at the room's difficulty in every mode. These are bot attributes; multiplayer players retain the normal
damage rules and PvP moon damage. Multipliers compose when a bot attacks a solo player.
Death loot remains 60% of the victim's remaining mass in 4-30 food pieces at the death location; continue
discards that life's drop as before.

Host migration target (November 16, 2026): use Multiplayer Services Relay migration with an application-owned,
versioned and compressed world snapshot every second. Graceful departure freezes gameplay and uploads a final
snapshot before leaving. A surviving session member becomes host, restores the arena and reconnects players
by their stable session player IDs, never by old NGO client IDs. Preserve mass, positions, appearance, moons,
perks/offers, defeat/continue state, loot ownership/lifetimes, bots, food fields, comets, world-event clocks,
room difficulty and each surviving client's run accounting. Recovery pauses gameplay and blocks input; it
must not create a new life, award duplicate rewards or silently reset an invalid snapshot. Abrupt loss can roll
back at most to the latest successfully uploaded snapshot, so zero rollback is only promised for a successful
graceful final upload. After 45 seconds without recovery, show a failure and return to the menu. This adapts
the persistent arena expected from .io games to peer-hosted Relay rooms. Independent-peer tests are required
before marking the feature verified;
see [migration acceptance checks](#appendix-b-verification-history-and-release-checks).

Graceful handoff first broadcasts a pause and waits up to two seconds for connected clients to acknowledge
their final owned pose. Snapshot uploads are serialized by the application; the final upload waits for any
older upload before writing, so an in-flight periodic request cannot overwrite the handoff state.

Verification after the difficulty damage/room revision: 464 EditMode checks and 8 selected PlayMode scenarios
passed. The scenarios cover real boost velocity and cost, difficulty persistence, comet/moon/border damage,
death loot from both a player and an eaten bot, moon immunity, continue loot cleanup and black-hole behavior.
The preceding full PlayMode run passed 61, failed the already reproduced wormhole cooldown assertion and
skipped 2 (the explicit feeding probe and a skin purchase on an already-unlocked profile). The explicit feeding
probe was run separately: masses 0.35/0.8/1.4/2 at 12.0/49.3/130.8/224.9 simulated seconds; mass 2.9087 at
600 s, with a late plateau and no reach to 4 or 8. This is not a hands-on pace or active-bot balance validation.
Room difficulty metadata and PvP damage isolation passed rule tests; independent network peers and Android
hardware were not tested. See [migration acceptance checks](#appendix-b-verification-history-and-release-checks) for the
required continuity work.

Food inside the body (November 16, 2026): food is eaten on the trigger enter event, so a food that ends up inside a
planet's body without a new enter event (a respawn or a magnet pull landing inside the body) used to stay uneaten while
the body kept overlapping it. The server now also sweeps every combat-active planet, player or bot, every 0.1 s and eats
any unclaimed, non-immune food overlapping the body circle through the same claim and growth path as a trigger enter,
so nothing is counted twice. Slither.io eats on overlap with no such gap. Growth per food, magnet and speed are
unchanged; the sweep only removes the missed pickups. Re-measure the section 4 growth pace after this change.

Growth readability (November 16, 2026): keep area-based body growth and equal movement speed at every mass.
Reduce the camera zoom exponent from 0.28 to 0.15: doubling mass should increase settled screen diameter by
about 27%, and growing from 0.08 to 2.0 by about 3.09 times, unless moon framing requires a wider view.
Hold growth-driven zoom for 0.35 s, then ease outward over 0.9 s; continuous feeding must not restart the hold.
Shrinkage and a minimum view required for moons bypass the hold. Moon framing accounts for aspect ratio and
look-ahead so the ring remains visible in portrait and landscape. This is a deliberate adaptation of Slither.io's
zoom-out: a round planet needs a stronger screen-size cue than a snake's growing tail. Re-measure section 4's
feeding pace in Play Mode and report actual results separately from targets.

Feel and HUD revision (November 18, 2026): reduce player cruise speed from 4.0 to 3.6 u/s and
boost from 8.0 to 7.2 u/s; retain equal speed at every mass and the existing gravity/speed ratio.
Ease steering over 0.18 s with a 360 degree/s limit. This slower response is a planet-specific comfort
adaptation of Slither.io steering requested after playtesting. Ease body growth continuously toward its
area-based target (95% in 0.45 s), including consecutive food pickups, with no overshoot. The visible
body and collision radius grow together; mass, score and eating eligibility update immediately. Damage,
initial spawn and migration restoration snap to their correct size to avoid a stale large hitbox.
Keep the camera curve and food economy. Re-measure section 4 milestones after the speed/size change.

The minimap must reject non-finite initial layout dimensions before caching marker coordinates; otherwise
markers can remain stuck in the corner after the first layout. It must preserve the 440:296 world aspect ratio,
keep edge markers inside its bounds, and draw
the local player last with a white outline even when they lead. Gold still marks the leader; event
markers use a square shape. Retain a 264 logical-pixel joystick base, 108-pixel knob, 78-pixel travel
and 20% dead zone: this keeps the knob inside the ring. Floating remains the default; fixed and mirrored
controls stay supported. Increase icon button hit areas to 104 pixels, stepper buttons to 96 pixels,
and toggle hit areas to 88 pixels while retaining the 60-pixel switch graphic. Text links receive an
80-pixel minimum touch height. Verify menus, settings and HUD in portrait and landscape in the Editor;
physical comfort and gesture-navigation clearance still require device testing. UI aspect-ratio tests account
for up to one physical pixel of dimension rounding when UI Toolkit snaps rectangle edges to the pixel grid.

Planet nicknames (November 17, 2026): reduce player and bot nameplate font size from 5 to 4.25 (15%)
at the user's request to keep labels less prominent beside planets. Retain the existing size-dependent scaling.

Perk presentation (November 16, 2026): place the three choices in a compact top dock inside the safe area,
below the top HUD controls, keeping the center and bottom thumb zones clear. Each choice uses a distinct icon,
a localized name and a short localized effect/cost summary. Reuse those icons for active perks. Keep the
countdown and server-authoritative selection. Solar Sail retains +12% movement speed and -10% food;
Ion Trail additionally gives +15% boost speed, retaining its damaging trail and +25% boost mass cost.
Speed bonuses depend on chosen perks, never mass; these are deliberate progression choices beyond Slither.io.

Controls (November 16, 2026): the floating joystick stays the default. Settings have a persisted "Fixed joystick"
toggle:
the base is pinned to the bottom corner of the chosen side (it honors "Joystick on the right"), the stick deflection is
limited to a radius, and the base does not follow the finger. A touch in the control zone sets the direction relative to
the pinned center; releasing or cancelling the touch zeroes the input. Switching the mode applies immediately and resets
the current touch. This is an extra mobile-comfort setting added at the user's request; the core Slither.io principle
(finger sets the direction, a second finger boosts) holds in both modes.

| Slither.io                    | Planet IO now                                            | Planet IO target                                 |
|-------------------------------|----------------------------------------------------------|--------------------------------------------------|
| Tail body                     | Orbiting moons                                           | Tune positional combat                           |
| Enemy hits your tail and dies | A moon touch hurts and knocks back                       | Verify a small player can beat a big target      |
| Encircling (circle kill)      | Moon ring and gravity                                    | Verify the encircling tactic                     |
| Boost costs length            | exists; ion trail is a perk                              | Develop trail cosmetics                          |
| Loot on death                 | exists                                                   | Keep; add a "supernova" for big planets          |
| Crown for the top 1           | Leader ring, minimap beacon, bounty                      | Add a pointer toward the leader                  |
| Skins                         | planets                                                  | Planets + moon sets + trails + explosion effects |
| Speed independent of length   | equal for all masses                                     | Keep; EditMode test for the invariant            |
| Zoom out while growing        | power-law zoom 0.10, delayed growth response, boost zoom | Tune in playtests                                |
| Food readable at any zoom     | visual ×1.2 × zoom^0.7                                   | Tune in playtests                                |

Principle: decisions about game feel (speed, camera, size, food, boost) are checked against the best practices of
Slither.io and similar genre games; any deviation is recorded here. The rule is recorded in [AGENTS.md](../AGENTS.md).

Comets are a deliberate addition to the Slither.io reference: a predictable moving hazard creates the choice "avoid it
or
follow it for food". We keep the names at the user's request, with labels about 25% smaller. The trail redistributes
regular food and does not create new mass.

Services decision (November 17, 2026): all ads are player-requested rewarded video. Unlike Slither.io's
between-run advertising, Planet IO has no automatic ad placements or paid ad-removal option, at the user's
request. Restarting a run never shows an ad. Rewarded gates retain their existing limits: continue once per life,
with other available rewards requested individually. Review prompts appear only after a rank-up. Global lists
are best-effort because Planet IO has no trusted score server (section 7.2).

## 6. Mechanics

### Comets: agreed design (November 16, 2026)

- Pool of 30, regular speed 2.5–3.5 u/s, a straight course with no homing and no sharp turns.
- A new pass starts outside the view of living players: at least 55 units from each, with a margin for the camera. If
  there is no safe place, the comet waits for the next attempt off the map. The server picks the position and course.
- The name is always visible regardless of old saved settings; the label is 25% smaller than before. The visibility
  toggle was removed from the settings.
- A blue glowing tapering tail keeps about 1.4 seconds of movement, shows the heading, has no collider and deals no
  damage. It is cleared on respawn or teleport and never joins different places on the map.
- Every 1.5 seconds a comet may leave one regular food item beyond its body edge. This relocates existing food: no new
  network objects, no extra mass, and golden food, boost drops and loot are never moved. The trail is capped at 90
  points
  per world; after 8 seconds an uneaten point returns to its field. Pause stops both movement and trail creation.
- An unprotected hit removes 50% of the victim's current mass and current mass score, independent of comet size.
  Existing minimum-mass/death rules apply; peak score and earned XP are retained. A moon still intercepts the hit; Comet
  Rider restores half of the calculated damage instead. This deliberately harsher moving hazard, absent in Slither.io,
  makes avoidance meaningful as requested by the user.
  Strengthening the damage is decided separately after playtests.
- The meteor shower keeps its 6 s warning, the strip, 12 consecutive passes at about 8 u/s, and 5 loot piles after the
  event; it uses the same tails and the capped food trail.
- Checks: EditMode for safe spawning and the trail budget, PlayMode for movement, respawn, interception, labels and the
  tail at different zooms; host/client and a repeat measurement of the growth pace from section 4. The numbers are
  starting parameters for playtests, not confirmed balance.

Implementation status: permanent labels reduced by 25%, speed 2.5–3.5, safe respawning, blue visual tails and a
capped trail of relocated regular food are implemented. All 60 selected EditMode tests and 7 PlayMode tests passed,
covering rules, settings UI, movement, respawning, food conservation, damage, moon interception and the meteor shower.
A separate network client, Android performance and the growth-pace measurement from section 4 remain unverified.

### 6.1. Unchanged

Finger control, perpetual motion, food and fields, field migration, golden food, bots with personalities, rooms by code,
2 s spawn protection, continue via rewarded, skins. This is the working base; touch it only based on playtest results.

### 6.2. Tiers (Evolution)

| Tier | Name      | Mass      | Satellites | Perk        | Camera  |
|------|-----------|-----------|------------|-------------|---------|
| I    | Asteroid  | 0.08–0.35 | 0          | none        | close   |
| II   | Planet    | 0.35–0.8  | 2          | pick 1 of 3 | farther |
| III  | Gas giant | 0.8–1.4   | 4          | pick 1 of 3 | farther |
| IV   | Star      | 1.4–8.0   | 6          | pick 1 of 3 | maximum |

Rules:

- A tier-up comes with a flash, haptics, new moons appearing and a perk pick screen (3 cards, 5 seconds to choose,
  otherwise random).
- Dropping below a tier threshold removes the tier, but **perks stay** until the end of the life (otherwise the penalty
  is doubled).
- A new life resets perks; continue keeps perks and returns 50% of the mass. Satellites reset on death and form again on
  revival according to the current tier.
- Progressive disclosure: on tier I the player only knows "eat and run"; moons and perks are explained when they appear.

Current differences: a tier drop has a hysteresis of 0.03 mass (`TierRules`); a perk pick is granted on the first
arrival at each new tier in a life. The server adds a 0.75 s margin to the 5 s pick window. Cards and tutorial are
wired. Death clears satellites; continue rebuilds them for the revived tier. Both behaviors have PlayMode regression
coverage.

### 6.3. Moons (tail replacement)

Satellites orbit the planet and let it hurt a bigger opponent without a size advantage.

| Parameter                              | Value                                                                                                             |
|----------------------------------------|-------------------------------------------------------------------------------------------------------------------|
| Orbit radius                           | 1.7 × body radius (collider radius × scale)                                                                       |
| Angular speed                          | 90°/s, all moons turn the same way, each planet has its own starting phase                                        |
| Moon radius                            | 0.2 × body radius; visual is a shrunken copy of the owner's skin                                                  |
| Damage to another planet on moon touch | max(0.004, 6% of its mass), effectively 6% at mass ≥ 0.067                                                        |
| Knockback                              | 6 u/s, fades in ~0.4 s; the target is immune to any moons for 1.0 s                                               |
| Moon durability                        | 2 hits, then it disappears; regrows in 16 s                                                                       |
| Moon vs moon                           | "ring clash": contacting moons lose one durability; no core damage; both bodies pushed back at 3 u/s, 0.8 s pause |
| Moon vs comet                          | the moon intercepts the comet: the comet respawns, the moon takes a hit, the planet takes no damage               |
| Lost mass                              | 50% of the damage goes to the moon's owner immediately (no food falls on the ground)                              |
| Own moon                               | does not hurt its owner; under spawn protection moons neither hit nor get hit                                     |

Implemented in code:

- Tiers and moon slots (`TierRules`, 0/2/4/6), moon state (`MoonLoadout`), geometry and damage (`MoonRules`).
- Server logic for strikes, comet interception and ring clashes in `PlanetScale.Moons.cs` (strikes every 0.04 s, rings
  every 0.12 s).
- Moon display `MoonRingView`, threat ring `ThreatRingView`, knockback `PlanetKnockback`, tier-up flash.
- HUD: a Roman-numeral tier and a progress bar to the next tier under the score (`tier-panel` in `Hud.uxml`, the bar
  color depends on the tier).
- Camera: while the player has moons, the scale is at least the outer ring edge × 1.45 (`PlayerCamera._moonViewMargin`);
  on a tier-up the zoom widens smoothly.
- Sound and haptics: tier-up and own-moon strike use dedicated cues (Kenney, CC0) with Success and LightImpact haptics.
- Bots treat enemy moons as a hazard. Eligible Medium/Hard bots also attempt short moon raids against larger
  targets, then retreat after taking damage or exhausting their attack window. They can hunt with moons
  in mind.

Remaining: the first-tier-II hint is a FTUE step, so the translations into 14 languages need checking; bot AI for moons
is deferred.

Eating by the core keeps the 1.08 threshold; 1.12 is a possible future tuning. Moons should create windows to attack and
to dodge. Destroying enemy moons with boost is not planned: moons already break on ring clashes.

Implementation: moons are not `NetworkObject`s. Position is computed from server time, the planet position, the moon
index and the slot count; a `NetworkVariable<byte>` mask holds the living moons. A destroyed moon leaves a gap in the
orbit. The server finds contacts with circle queries; hit and ring-clash buffers hold 16 colliders per query.

### 6.4. Gravity

Every planet creates a pull zone for bodies much smaller than itself (mass ≤ 0.6 × its mass).

| Parameter                     | Value                                                                                         |
|-------------------------------|-----------------------------------------------------------------------------------------------|
| Zone radius                   | 3 × planet radius                                                                             |
| Velocity offset at the center | up to +35% of the target's speed, falling off quadratically toward the edge                   |
| Slingshot                     | moving tangentially (60–120° to the radius) for ≥ 0.5 s gives +20% speed for 2 s, then decays |
| Food                          | magnet: body radius + 0.2 × perk × growth (`FoodMagnetRules`); the gravity zone is not used   |
| Indication                    | a ring around planets bigger than you: orange, pulsing                                        |

Implemented: shared `GravityRules`, pull and slingshot in `PlanetGravity`, indication in `ThreatRingView`. The owner
computes it for the player, the server for bots. No separate network messages for pull strength were added.

Decisions: the strength is an absolute offset of up to 1.05 u/s; at speed 3.6 that is about 29%, close to the 35% goal,
so
a fraction of speed is not introduced. Food magnet: body radius + `_foodAttractionRadius` (0.2) × perk multiplier ×
(body radius / 0.28)^0.7 (`FoodMagnetRules`); the gravity zone radius is not used. Bots feel the pull; tactics for
avoiding wells and using the slingshot are deferred.

### 6.5. Perks (starting pool: 9)

| Perk           | Effect                                               | Cost                       |
|----------------|------------------------------------------------------|----------------------------|
| Ion trail      | boost speed +15%; damaging trail for 2 s             | boost costs 25% more       |
| Magnetosphere  | magnet radius ×1.5                                   | speed −8%                  |
| Heavy moons    | +1 moon slot, moon damage +50%                       | speed −5%                  |
| Ablative crust | survives one lethal hit, 30% mass remains            | consumed when triggered    |
| Solar sail     | speed +12%                                           | food gives −10%            |
| Gravity well   | gravity zone ×1.4                                    | moons spin 20% slower      |
| Nebula veil    | disappear from enemy minimaps for 10 s after a boost | 40 s cooldown              |
| Comet rider    | comets heal 50% of the damage instead of hurting     | visible glow, enemies know |
| Predator       | a kill gives +20% absorbed mass                      | food gives −10%            |

The pool is a starter set; expand it after playtests. Each perk must have a downside and one strong counter.

Status: all nine perks work. The server stores an offer of three cards, validates the pick index and picks a random perk
itself after 5 s (+0.75 s for network delay). The pick screen is `perk-panel` in `Hud.uxml`, shown by `PerkPresenter`
through `IPerkView` (`SessionHudView`); texts live in `UIStrings.csv` in 14 languages (names and descriptions should be
proofread by native speakers). Bots get no perks.

Offer rules: one pick for each new tier II–IV per life. Losing and regaining a tier gives no offer; jumping several
tiers
queues one pick per tier. Death resets perks and the queue; continue via ads keeps perks.

Balance and implementation differences:

- "Gravity well" lowers movement speed by 6% instead of slowing the moons.
- The crust leaves at least the player's starting mass (currently 0.08, equal to the minimum); when it triggers on a
  drop
  to the minimum mass, the restore is applied on the next frame, because `Capacity` cannot be changed inside its own
  `OnValueChanged`: assigning again after that does not fire the callback. The deferred restore must never lower mass
  already gained from food in the meantime; healing must not trigger another lethal minimum-mass transition.
- "Nebula veil" invisibility shows only as disappearing from the minimap and bots losing the target; there is no
  separate
  effect on the planet.
- "Ion trail" deals 70% of moon damage, and the victim gets the same one-second immunity as from moons.
- The "Comet rider" glow is not wired (low priority).
- Active perks are shown as badges under the score (color per perk, the letter comes from the localized name); rerolling
  the cards for ads is deferred.

### 6.6. Combat: rule summary

| Contact                     | Result                                                                                                            |
|-----------------------------|-------------------------------------------------------------------------------------------------------------------|
| Core ≥ 1.08 × target core   | eaten; the owner gets a base 40% of the victim's mass, 60% becomes loot; absorption is boosted by perk and bounty |
| Core × enemy moon           | damage, knockback, the moon loses durability                                                                      |
| Moon × moon                 | bounce                                                                                                            |
| Planet × comet              | mass damage; a moon intercepts                                                                                    |
| Planet × border             | movement blocked at the edge, mass lost on entering the zone, red vignette; no push-back needed                   |
| Planet × black hole (event) | −25% mass, then food is spat out                                                                                  |
| Tier III+ death             | deferred: "supernova", a 6 u shockwave pushes away, loot flies out in a ring over 1 s                             |

The calculations are split among the pure `CombatRules`, `GrowthRules`, `MoonRules`, `GravityRules`, `TierRules` and
`PerkRules`; they have EditMode tests. Contacts and network state changes stay in gameplay classes. The black hole is
implemented as a cosmic event (6.8); the supernova on death is deferred.

### 6.7. Leader and bounty

- The top-1 by mass gets a crown and a beacon: visible on everyone's minimap; a pointer arrow is deferred.
- Killing the leader gives +25% absorbed mass.
- A lever against snowballing: "burnout": above mass 1.2, a passive loss of 0.2 × (mass − 1.2) per minute unless food
  was picked up in the last 3 s.

Current implementation: the leader is chosen among players and bots with mass from 0.4; on equal mass the smaller
`NetworkObjectId` wins; recomputed every 0.5 s. There is a ring, a minimap highlight and an absorption multiplier of
1.25. There is no pointer to the leader (deferred); "burnout" is enabled only if playtests show a snowball for the
leader. The existing arrow shows the player's own movement direction.

### 6.8. Cosmic events

Phase 4 status: events, the map, the daily modifier and Blitz are implemented in code. The event schedule is not sent
over
the network: every participant computes it from the day number (UTC) and server time (`CosmicEventRules`), only the
server applies effects, everyone draws the visuals (`WorldDirector`). One event every 90–150 s, a banner 6 s before
the
start, and a minimap marker for the whole event. Types come in triples (each once in random order), so there are never
two identical events in a row.

| Event         | What happens                                  | Reward/risk                               |
|---------------|-----------------------------------------------|-------------------------------------------|
| Meteor shower | 12 comets cross a strip of the map (25 s)     | 5 loot piles along the strip afterward    |
| Supernova     | a star pulsates for 30 s, then explodes       | 4 big loot piles in a ring                |
| Black hole    | crawls for 40 s and pulls everything in       | −25% mass once per event, loot at the end |
| Solar flare   | dropped from the plan: three types are enough | —                                         |

The daily modifier (`DailyModifierRules`) is chosen by day: weak gravity ×0.5, strong ×1.5, gold rush (event loot ×2)
or
none; a banner shows at the 8th second of play. Gravity scales as a whole (planets, stars, the black hole), not only for
events; we keep that. Known limitation: the day is taken on each device separately, so if midnight passes during a
session the schedule can diverge between participants.

### 6.9. Map

- Now: 440 × 296, 1 home field and 16 migrating ones.
- Phase 4: 4 neutral stars (static gravity wells for the slingshot, strength 0.8, zone 14), a pair of wormholes (the
  teleport exits 5 u from the other entrance, 10 s cooldown per planet), 3 nebulae (a planet inside disappears from the
  minimap and from bots' view). Positions come from `MapFeatureRules` by day number, outside 40 u from the center, stars
  no closer than 90 u.
- Respawn: the owner scores 8 random candidates (`Player.Respawn`): score = food within radius 12 (up to 40 items) /
  40 +
  2 × min(1, distance to the edge of the nearest body that can eat the player / 22). Food and planets come from
  replicated objects, so a server density map is not needed. It also applies to continue.

## 7. Progression and meta

Phase 3 status: rank, stardust, missions, streak, the results screen, buying skins with stardust, a step-by-step FTUE
and
local analytics are implemented. The rank reward is done: stardust `5 + rank` for each new rank (about 600 across all 30
ranks, less than half the price of the eight paid skins); it counts toward the run's stardust and toward "Double
stardust". Deferred: moon sets, trails and explosion effects (need art and a shop screen) and server-side leaderboard
storage. Google Play Games sign-in, cloud save, leaderboards and achievements are implemented after release (section
7.2).

| System                        | Description                                                                                               |
|-------------------------------|-----------------------------------------------------------------------------------------------------------|
| Rank                          | XP per run: lifetime, peak mass, kills, tier. 30 ranks, reward: stardust 5 + rank                         |
| Stardust                      | soft currency per run, spent on cosmetics; rewarded gives ×2                                              |
| Cosmetics                     | planet skins (exist, 12 of them, 4 free); moon sets, trail, explosion, nickname frame come after release  |
| Missions                      | 3 a day ("eat 3 planets", "reach tier III", "survive 4 min"), reroll via rewarded                         |
| Streak                        | daily login: growing reward, reset on a missed day                                                        |
| Achievements and leaderboards | Google Play Games: peak mass, kills per run, total kills; 10 achievements (section 7.2)                   |
| Cloud save                    | Google Play Games saved game: progression, profile, skins; highest XP wins on conflict (section 7.2)      |
| Results screen                | killer and their tier, peak mass, time, XP with an animated rank bar, "Play again" and "Continue" buttons |

### 7.1. How it works in code

| Part           | Where                                             | Rules                                                                                                                                                    |
|----------------|---------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| XP and rank    | `XpRules`                                         | XP per run: 1 per 2 s of life (up to 900 s), 1 per 2 points of peak mass, 25 per kill, 20 per tier above the first; 30 ranks, step 100 + 40 × (rank − 1) |
| Stardust       | `DustRules`                                       | a quarter of the XP earned (minimum 1); ads double the run's stardust; a skin costs 250                                                                  |
| Streak         | `ProgressionState.RefreshDay`                     | logging in on a consecutive day gives 10 stardust, plus 5 for each further day, max 50; a missed day resets to 1                                         |
| Missions       | `MissionRules`                                    | 3 a day from 7 templates, chosen deterministically by day number; one reroll a day, only for an untouched mission                                        |
| Run accounting | `ProgressionState.ApplyRun`                       | each run has a unique `RunId` (UTC ticks), so "continue" and a repeated death pay only the difference                                                    |
| Storage        | `ProgressionService`                              | `player-progress` slot in `SaveStore`; the "day" is counted by UTC on the device clock                                                                   |
| Run statistics | `RunStatsTracker`                                 | lifetime, peak mass and tier, kills, perks, killer                                                                                                       |
| Results        | `DefeatPresenter` + `IRunResultView`              | time, kills, tier, killer, an animated rank bar, reward, a "Double stardust" ad button                                                                   |
| Menu           | `ProgressMenuPresenter` + `MainMenuView.Progress` | rank and stardust in the header, the missions screen, a "!" badge on the button when a reward is ready                                                   |
| FTUE           | `FtuePresenter`                                   | condition-driven steps: movement, food, red ring, green ring, tier II, boost; each step times out, about a minute at most                                |
| Analytics      | `LocalAnalyticsService`                           | events from Appendix A go to the debug build log; there is no external backend                                                                           |

Saving goes through `RD.Save`, day and streak timers through `RD.Timers`, haptic feedback through `RD.Haptics`, settings
through `RD.Settings`. Do not write new equivalents in the project; extend shared modules in the linked `UnityTemplates`
checkout (see the README).

### 7.2. Platform services (after release)

Platform services sit behind contracts in `Runtime/Core/Contracts/Services`; the rules are pure classes in
`Runtime/Core/Services` with EditMode tests. Google Play Games is unavailable in the Editor; device sign-in
requires IDs from Play Console. Android release validation automatically lists missing
Play Games IDs as a warning during the AAB build. Two icon buttons (leaderboards,
achievements) sit in the top corners of the main menu and show only while signed in; sign-in lives in Settings.

Monetization decision (November 17, 2026): only player-requested rewarded ads are supported. There are no
interstitial ads, purchases, ad-removal entitlements, or restore-purchase controls. This departs from ad-supported
.io restart flows to keep every ad optional; continuing, doubling stardust, and ad-gated cosmetics still require
the rewarded ad to grant its reward.

| Service                | Where                                             | Rules                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
|------------------------|---------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| In-App Review          | `PlayReviewService`, `ReviewPromptRules`          | Right after a rank-up on the results screen; at least 5 finished runs and 2 days since the first run; at most 3 prompts in a lifetime, at least 90 days apart. The Play quota can still hide the dialog: that is not an error                                                                                                                                                                                                                                                                                                                                 |
| Sign-in and cloud save | `PlayGamesService`, `CloudSaveRules`, `CloudSync` | Silent sign-in at start; a "Sign in" button in the menu if it failed or was cancelled. After sign-in the snapshot `planetio-progress` is merged with the local data: the side with more XP wins the progression, skins are unioned, best score and total kills take the maximum. Upload after each run (at most once per 60 s), when the app is paused and after a merge that changed anything. Dust earned on the losing side is lost: there is no spend ledger to merge, and a 5–10% loss on the rare two-device case is cheaper than a duplicated currency |
| Leaderboards           | `PlayGamesService`, `ScoreValidationRules`        | Three boards: peak mass in a run, kills in a run, total kills. A run is reported only if it passes the plausibility check (duration at least 10 s, mass within the physical maximum, kills no faster than one per 2 s plus 3) and difficulty is not Easy, because Easy lowers incoming damage                                                                                                                                                                                                                                                                 |
| Achievements           | `AchievementRules`                                | 10 achievements: tier II, III, IV; 5 and 15 kills in a run; survive 5 minutes; rank 5, 15, 30; a 7-day streak. Unlocked ones are cached locally and in the cloud save, so they are reported once                                                                                                                                                                                                                                                                                                                                                              |

Anti-cheat decision. Global lists accept client-reported numbers: Play Games validates nothing. The movement is
owner-authoritative and the host is a player's phone (host migration), so there is no trusted process that could
validate speed, bounds or teleports, and Play Integrity needs a server to verify its token. The plausibility check
above only stops accidental and crude cheating. Real protection needs a backend (Cloud Code or a small function that
verifies a Play Integrity token and signs scores) and stays deferred; a spoiled leaderboard can be reset in Play
Console.

## 8. Modes

| Mode                                   | Status                                                         |
|----------------------------------------|----------------------------------------------------------------|
| Classic arena (endless)                | exists                                                         |
| Single player against bots             | exists                                                         |
| Private room by code                   | exists                                                         |
| 5-minute Blitz with a final scoreboard | exists (single player only, enabled with a toggle in the menu) |
| Daily modifier                         | exists                                                         |
| "Solar system" teams                   | not planned                                                    |

## 9. Monetization

Principle: all ads are rewarded and shown only at the player's request. There are no purchases or automatic ad
placements.

| Placement               | Type     | Status        |
|-------------------------|----------|---------------|
| Continue after death    | rewarded | exists        |
| Skin for watching       | rewarded | exists        |
| Stardust ×2             | rewarded | exists        |
| Mission reroll          | rewarded | exists        |
| Reroll the 3 perk cards | rewarded | after release |

Only player-requested rewarded ads are supported. Ad consent (UMP) is already implemented through `IAdPrivacyService`.

## 10. Android UX

- Edge-to-edge: the game background fills the whole screen, including the area under system bars and cutouts; buttons
  and important HUD stay inside `Screen.safeArea`. System bars are hidden, appear briefly on swipe and have a
  transparent
  background. The release Android build uses R8 (Minify Release); the build script sets and verifies these settings. On
  a
  device, check both landscape rotations, gesture and three-button navigation, and returning from ads and the consent
  form. This is a platform adaptation of the interface; the .io mechanics do not change.
- Controls: a floating joystick by default, or a fixed one in the bottom corner of the chosen side (a toggle in
  settings, see section 5), and a boost button for the second finger. In-interface hints describe touches, not keys.
- **Threat ring**: the planet outline is red if it is bigger than you, green if smaller, neutral if equal. The meaning
  is
  also coded by shape, without a setting: a dangerous planet has a thick pulsing ring, the others a thin one.
- HUD: a mass bar with tier ticks, perk badges, a kill feed, a small movement arrow at the planet edge, and a minimap
  with the leader beacon, an event banner.
- Main menu: at most four actions on screen (profile, Play, Online, the Blitz toggle) and two corner icons (missions,
  settings). Everything else opens in separate windows, as in Slither.io, where the main screen has only a nickname,
  Play and a skin picker.
- 60-second FTUE: eat food → see the big planet's red ring and run → eat a small one → get tier II and the first
  perk.
  The `onboarding.tutorial-completed` key already exists.
- Haptics: moon hit, kill, tier-up, death.
- Every new screen is built on UI Toolkit (UXML/USS in `Assets/_Project/UI`), localized through `UIStrings.csv`. For 14
  languages write as little text as possible: icons and numbers instead of phrases, no more than 20 new strings per
  phase.

## 11. Art and audio

Audio mix and variety (November 17, 2026): keep music at a maximum source gain of 0.45 and
apply a 0.2 global SFX gain before cue volumes (currently at most 0.7). Measured source
levels put the loudest cue about 3 dB below the gameplay theme at equal slider settings.
Raise the two new tracks to approximately -20 dB mean level to match the existing gameplay theme.
Keep both sliders independent. Add two free CC0 ambient
tracks, "Exploration Theme" by Cleyton Kauffman and "Steller Dreams" by Synth-thetic, alongside
the existing themes. Randomize the track on entering the menu or a game; remember each pool's
last choice across launches and exclude it from the next selection. Keep looping, streaming,
Addressables delivery and the existing fades. Record sources in `Assets/_Project/Audio/AUDIO_CREDITS.md`.

Settings audit (November 17, 2026): the existing music and effects sliders independently mute
their category at zero and persist through `RD.Settings`. Apply effects changes to active boost
loops immediately, including loops started while muted. The linked Unity Templates `RD.Haptics`
module is already used for damage, moon strikes, kills, tier-up, slingshots, perk picks, mission
rewards and death; the persisted haptics toggle controls all these calls.

- Moons: reuse small-planet sprites from `Sprites/Planets`, one material, a `SpriteRenderer` pool.
- Effects: tier-up (flash + ring), moon hit (sparks in the strike direction), supernova (shockwave), boost trail.
- Audio goes through the existing `SfxCatalog` (Eat, Hit, Kill, Death, Boost, plus MoonHit, TierUp and EventWarning from
  Kenney, CC0); the bounty cue is deferred. Music is already delivered through Addressables.
- Store screenshots: a series of English UIs, different skins and nicknames, the middle of a run near bots and comets,
  three size sets (following the project memory rules).

## 12. Technical plan

Editor workflow (November 17, 2026): keep only `Build release AAB` and `Set signing password` in the
`Planet IO > Android` menu. Release settings, icons, validation and version codes are handled automatically
by the build command; remove the other project menu shortcuts while retaining their callable editor methods.
After validation succeeds, the AAB command clears `Builds/Android`, including old bundles, symbols and
backup folders, before writing the new build. Other build directories are preserved.

1. **Rules in pure classes with tests.** `CombatRules`, `TierRules`, `GravityRules`, `PerkRules`, `XpRules` modeled on
   `GrowthRules`; tests in `Tests/EditMode`.
2. **Shared combat code.** Done in phase 0: the eating threshold and absorption share moved into `CombatRules`;
   `_eatSizeRatio` and `_initialCapacity` were lifted into `PlanetScale` (same field names, so prefabs do not change);
   `Player` and `Enemy` call `CanDevour`. A single `OnTriggerEnter2D` was not made: tests call it through reflection on
   `Player`, and the protection rules differ for player and bot.
3. **Split `Player.cs`.** Done as `partial`: the main file, `Boost`, `Border`, `Combat`, `Lifecycle`, `Appearance`,
   `Perks`, `Abilities`, `Respawn`. Keep serialized fields and RPCs. Extract separate components only when needed;
   adding a
   mechanic does not require a new architecture.
4. **Bots as the same planet object with an AI controller.** Moons and gravity are shared through `PlanetScale`; perks
   still belong to `Player`. Deeper AI (keeping moons toward the enemy, avoiding the pull zone, using the slingshot) is
   deferred; perks for bots are not planned.
5. **Network fields:** `NetworkVariable<byte>` for the tier and the moon mask, `NetworkVariable<ushort>` for perks, and
   the leader and stealth flags are implemented. The server stores the offer, and `ChoosePerkRpc(index)` validates the
   index. Widen the perk mask only if the current range runs out.
6. **Events:** the server-side `CosmicEventDirector`; the event state is a `NetworkVariable`; the banner is in the HUD.
7. **Food and network.** Replace individual food network objects with server-owned records, a spatial index,
   binary snapshots and batched change messages; render food only on graphical clients (including the host).
   Keep the full 2000 ambient records on clients initially; local visibility culling does not need network
   subscriptions. Add region subscriptions only if measured traffic requires them. The detailed
   implementation is in [Food Replication Plan](Food-Replication-Plan.md). Compare the data-only implementation with the former object baseline on the same device. The phase 5 target remains 55 FPS.
   Performance pass (November 16, 2026, from a code read, not yet measured on a device): the HUD reads
   `PlanetDirectory` instead of scanning every spawned `NetworkObject`; the minimap refreshes at 10 Hz and writes only
   changed styles; dropped food expires from one heap-based queue in `FoodSpawner.Update` instead of one async loop
   per item; the eat-effect RPC goes only to clients whose player is within `EatEffectRules` range; moon, threat and
   nameplate views skip planets outside the camera rectangle (`ViewBounds`); the host migration snapshot is taken every
   2 s (was 1 s) with fast gzip, so the rollback window after an abrupt host loss is up to 2 s; weak devices
   (`DeviceTierRules`) start on the Low quality level (no HDR, render scale 0.85); `NetworkRigidbody2D` is removed
   from the food prefab. Deliberately not done, reasons recorded in section 15: distance-based food visibility and
   ambient food as data (both were deferred then; the data-only replacement is now implemented), tick alignment (tunneling
   at boost speed), a higher
   `NetworkTransform` threshold (bodies always move, so it saves nothing).
   Performance follow-up (November 17, 2026): keep the 60 FPS gameplay target, but cap mobile menu/loading
   scenes at 30 FPS to reduce idle power use. Use Sprite Atlas V2 with built-in delivery and a prebuild
   updater: Gameplay contains planets/comets and food; Controls contains button sprites, including future
   additions. Keep full-screen backgrounds, the repeating grid, UI Toolkit vector icons and the edge vignette
   outside gameplay atlases. Use rectangular packing for Controls to preserve UI sampling. Set the Universal
   Renderer intermediate texture mode to Auto, allowing URP to allocate it only when required by post-processing
   or scaling. Preserve HDR/bloom on the standard profile pending device profiling. Cull the separate moon
   root when its planet leaves the view, so stale moon transforms cannot remain visible or render unnecessarily.
   These changes do not alter growth, food quantity, movement, zoom or magnet rules. Device heat reduction
   and sustained frame times remain unmeasured; do not infer a device speedup from Editor results.
8. **Anti-cheat before leaderboards.** Server-side validation of speed, bounds and teleports of the owner-authoritative
   transform. Deferred together with global leaderboards (phase 8).
9. **Analytics:** `run_start`, `run_end{cause, tier, mass_peak, duration, killer_tier}`, `perk_offered/picked`,
   `ad_offered/accepted`, `tutorial_step`, `mission_complete`. There is no external SDK: D1/D7 and crash-free come from
   Google Play Console, funnels from the `LocalAnalyticsService` debug log.
10. **Tests:** EditMode for rules, PlayMode for moon hits, tier-up and perk picks with synchronization checks.

## 13. Plan: remove, add, rework

### Remove

| What                                                                                 | Why                                                                         |
|--------------------------------------------------------------------------------------|-----------------------------------------------------------------------------|
| Duplicated combat logic in `Player` and `Enemy`                                      | Every new mechanic is written twice and balance drifts apart                |
| The `ShowCometNames` setting and its toggle                                          | Comet names are always shown, in a smaller font                             |
| Random respawn anywhere on the map (`Constants.RandomWorldPosition` in `TeleportTo`) | The player appears in the desert; replace with smart respawn (see "Rework") |
| Dead README links to `Docs/Food-GDD.md` and `Docs/Template-Sources.md`               | The documents were removed in a6a0037; links fixed in phase 0               |
| "Score = mass × 100" as the only metric                                              | Replaced by XP and rank; the number stays only as peak mass in the results  |

Status on November 16: duplicated combat logic, dead links and the single "score" metric are gone; random respawn was
replaced in phase 6; comet names stay and are always shown, only the visibility setting is removed.

### Add

| What                                                               | Phase                             |
|--------------------------------------------------------------------|-----------------------------------|
| Tiers, moons, combat rules `CombatRules`                           | 1                                 |
| Threat ring (size readability)                                     | 1                                 |
| Camera and HUD for tiers                                           | 1                                 |
| Gravity and slingshot                                              | 2                                 |
| Perks and the pick screen                                          | 2                                 |
| Leader beacon, bounty                                              | 2                                 |
| Results screen, rank, stardust                                     | 3                                 |
| Cosmetics: moons, trail, explosion                                 | after release                     |
| Daily missions and streak                                          | 3                                 |
| 60 s FTUE                                                          | 3                                 |
| Google Play Games: sign-in, cloud save, leaderboards, achievements | after release, done in code (7.2) |
| In-App Review                                                      | after release, done in code (7.2) |
| Cosmic events                                                      | 4                                 |
| Blitz mode and the daily modifier                                  | 4                                 |
| Neutral stars, wormholes, nebulae                                  | 4                                 |
| Analytics                                                          | 0 (event schema), 3 (funnels)     |

### Rework

| What                           | How                                                                                                             |
|--------------------------------|-----------------------------------------------------------------------------------------------------------------|
| `Player.cs` (643 lines)        | Done (`partial`), see 12.3                                                                                      |
| `Enemy`                        | Done: a bot is a planet plus an AI controller with a shared combat component, see 12.4                          |
| Border                         | Done: red vignette, blocking at the edge, 8% loss; the push-back is cancelled                                   |
| Comets                         | Telegraphed in advance, intercepted by moons, part of events                                                    |
| Boost                          | Same cost, add a cosmetic trail; the Ion trail perk makes it a weapon                                           |
| Respawn                        | Done: near food and away from bodies bigger than you (`RespawnRules`)                                           |
| Food over the network          | Phase 5: baseline 2000 items, then server records, spatial queries and binary delta replication                 |
| Death screen                   | Exists: killer and their tier, rank progress, "Play again" more prominent than "Continue"; perk reroll deferred |
| Movement (owner-authoritative) | Server validation after release, together with global leaderboards                                              |
| Camera                         | Done: smooth power-law zoom by mass (as in Slither.io) with look-ahead; tiers only set a minimum view for moons |

## 14. Roadmap

Estimates are for one developer, in weeks of active work. Phases 0–4 are implemented in code; the remaining phases
lead
to
the update release and are ordered by the rules in section 2.1.

| Phase                                                | Duration | Content                                                                                                                                                                                                     | Exit criteria                                                                                                                                                            |
|------------------------------------------------------|----------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 0–4. Foundation, moons, gravity, perks, meta, events | done     | Sections 6–8; rank, stardust, missions, streak, FTUE, events, Blitz, map features                                                                                                                           | Code implemented; on-device verification not finished (the 13 Nov report has scenarios marked Not Run)                                                                   |
| 5. Stabilization                                     | 1        | Full PlayMode without failures, moon-reset regressions, host/client on two devices, FPS and traffic measurement on a weak Android with 2000 food                                                            | All tests green; ≥ 55 FPS on a weak Android; validate data-only food replication (12.7)                                                                                  |
| 6. First minute and feel                             | 1–2      | Done in code: smart respawn, perk badges, rank reward, threat ring shape, small movement arrow, menu split into windows, three sounds. Left: on-device tuning of camera, speed, food, magnet and boost cost | A series of playtests (≥ 5 people): growth pace from section 4 within ±25%; ≥ 80% finish the FTUE; on tier II testers kill a tier III planet in at least 1 of 5 attempts |
| 7. Update release                                    | 1        | AAB build, device smoke test, closed testing, store listing and screenshots                                                                                                                                 | crash-free ≥ 99.5% in closed testing; D1 ≥ 35% from Play Console after release                                                                                           |
| 8. After release                                     | by data  | Done in code: Play Games (sign-in, cloud save, leaderboards, achievements), In-App Review. Left: anti-cheat backend, cosmetics and a shop, rerolls for rewarded, deeper bot AI                              | By retention and rewarded monetization metrics                                                                                                                           |

If time is short, cut in this order: first the sounds, then the rank reward, then the perk badges. Do not cut
stabilization, smart respawn and on-device tuning: without them the update ships with unverified feel and a risk to the
first minute.

## 15. Metrics and risks

Target hypotheses (not guarantees; recheck against test data):

| Metric                | Target                |
|-----------------------|-----------------------|
| D1 / D7               | 35–40% / 8–12%        |
| Session length        | 7–10 min, 4+ runs     |
| Rewarded acceptance   | ≥ 25% of shown offers |
| Crash-free            | ≥ 99.5%               |
| FPS on a weak Android | ≥ 55                  |

| Risk                                    | Mitigation                                                                                                                                                                                                                                                                                                                                                                                         |
|-----------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Too many mechanics for a 5-second pitch | Disclosure by tier, FTUE, threat ring                                                                                                                                                                                                                                                                                                                                                              |
| Network load                            | Moons are computed analytically, perks sit in a bit mask, events are one `NetworkVariable`; food uses 2000 table records and binary delta replication; online rooms retain periodic recovery checkpoints                                                                                                                                                                                              |
| Food replication migration    | Data-only food replication is implemented (Food-Replication-Plan.md): magnet, bot, respawn, comet trail and migration use the table; food colliders and NGO prefabs are removed. Editor tests cover two clients, late join and host migration; on-device multiplayer measurement remains open. Aligning the 30 Hz network tick with 50 Hz physics risks tunneling at boost speed (9.6 u/s); a larger `NetworkTransform` threshold saves nothing because bodies never stand still |
| Leader snowball                         | Bounty, burnout above 1.2 if needed                                                                                                                                                                                                                                                                                                                                                                |
| Perk imbalance                          | A pick-rate table in analytics, a downside on every perk, all numbers in a `ScriptableObject`                                                                                                                                                                                                                                                                                                      |
| Localization cost                       | Icons and numbers instead of phrases, ≤ 20 strings per phase                                                                                                                                                                                                                                                                                                                                       |
| Cheating                                | Global lists accept client-reported numbers after a plausibility check only; a trusted backend is the real fix (section 7.2). Lists can be reset in Play Console                                                                                                                                                                                                                                   |
| Cloud save overwrites progress          | The side with more XP wins, skins are unioned; the merge is a pure function with tests; the local file is never deleted by a sync                                                                                                                                                                                                                                                                  |
| Low player count                        | Bots fill an empty room, the mode does not depend on the number of humans                                                                                                                                                                                                                                                                                                                          |

## 16. Assumptions and open questions

The plan for phases 5–8 is built on the assumptions below; confirm or replace them:

1. The audience is casual: short runs, 9 perks, gravity without manual tuning. We do not deepen the system.
2. Bots come first (single player and Blitz); rooms by code are an extra mode, so anti-cheat and matchmaking are
   deferred.
3. The time to the update release is 3–4 weeks of active work (phases 5–7).

## Appendix A. Analytics schema

Event names use snake_case, parameters are flat, values are only numbers and short strings. Do not include player IDs or
nicknames in events.

| Event              | Parameters                                                                                                  | When                         |
|--------------------|-------------------------------------------------------------------------------------------------------------|------------------------------|
| `session_start`    | `mode` (solo, quick, room), `bots` (count)                                                                  | entering the arena           |
| `run_start`        | `run_index`, `skin_id`                                                                                      | the planet appears           |
| `run_end`          | `cause` (planet, bot, comet, border, boost), `duration_s`, `mass_peak`, `tier_peak`, `kills`, `killer_tier` | death                        |
| `tier_up`          | `tier`, `time_in_run_s`                                                                                     | reaching a new tier          |
| `perk_offered`     | `tier`, `perk_ids` (comma-separated)                                                                        | cards shown                  |
| `perk_picked`      | `tier`, `perk_id`, `auto` (0/1), `rerolled` (0/1)                                                           | perk chosen                  |
| `moon_hit`         | `dealt` (0/1), `victim_tier`                                                                                | moon hit, sample 1 in 5      |
| `event_start`      | `event_id`                                                                                                  | a cosmic event starts        |
| `ad_offered`       | `placement` (continue, skin, dust_x2, mission_reroll, perk_reroll)                                          | offer shown                  |
| `ad_result`        | `placement`, `accepted` (0/1), `completed` (0/1)                                                            | viewing result               |
| `tutorial_step`    | `step`, `skipped` (0/1)                                                                                     | FTUE step                    |
| `mission_complete` | `mission_id`                                                                                                | mission completed            |
| `shop_purchase`    | `item_id`, `currency`                                                                                       | purchase with stardust       |
| `review_prompt`    | `rank`                                                                                                      | the review flow was launched |
| `signin`           | `ok` (0/1), `auto` (0/1)                                                                                    | Play Games sign-in finished  |
| `cloud_sync`       | `result` (uploaded, downloaded, merged, same, failed)                                                       | a cloud save sync finished   |

Funnels for checking hypotheses: FTUE (steps in sequence), first run → second run, death → continue / "play again",
tier II → tier III, ad offer → acceptance.

## Appendix B. Verification history and release checks

Historical results describe their recorded source revision, not the current build. Raw logs and captures remain
local artifacts under `Logs/` and `Temp/`. Detailed one-off investigation reports have been consolidated here.

### Baseline verification (November 13, 2026)

Windows, Unity 7000.0.0a16, Test Framework 1.9.0: the first full EditMode run passed 264 of 266;
existing PlayMode scenarios passed 14 of 14; new moon scenarios passed 5 of 7. The moon failures were
`Moons_DefeatClearsSatellites` and `Moons_ContinueDoesNotRetainBrokenMoonFromPreviousLife`: death left
satellite state alive and continue retained a broken moon. Later implementation and verification updates
are recorded in sections 2 and 5; the baseline is not evidence of a current failure.
The baseline also found the demo AdMob App ID and a test/settings target-API mismatch (37 versus 36).
It did not establish an external API requirement. Remote replication, late join, reconnect, Android
performance, traffic and production rewarded delivery were not checked in that run.

### Controlled growth verification (November 16, 2026)

The explicit `GrowthPace_ContinuousFeedingRecordsMilestones` probe runs for 600 simulated seconds at
time scale 8 using actual movement, pickup, magnet and food respawn. A nearest-food pilot starts near food;
opponent collisions, events, gravity, boosts and perks are disabled. Fields are randomized. This is not
a human or Android playtest, and its milestones are not representative match averages.

Plateau diagnostics found eligible, unclaimed trigger food already overlapping the body without a fresh
trigger-enter event. The server-side `PlanetScale.SweepFoodInsideBody` fix uses the normal claim/growth
path every 0.1 s; 4 EditMode and 3 PlayMode sweep checks passed. Four subsequent probe runs had no stalls:
mass 0.35 at 12-21 s, 0.8 at 35-55 s, 1.4 at 84-142 s, 2.0 at 146-234 s, and final mass 3.75-4.20.
These are historical tuning results; section 5 records later revisions and probes. Human growth targets
in section 4 still require a hands-on run. Probe output: `Temp/GrowthPace.txt`; visual capture: `Temp/PerkGrowth.png`.

### Feel and HUD verification (November 18, 2026)

After the section 5 feel revision and the minimap initialization fix, all 609 EditMode tests passed.
The new regression exercises an unmeasured (NaN) layout and verifies that subsequent marker movement
is finite and correct; screenshot review exposed the cached-NaN corner freeze. The full PlayMode run passed 81
checks, skipped the explicit growth probe and the skin-purchase check (this local profile already
owns every skin), and found only an overly strict pixel-rounding assertion in the new HUD check.
After allowing for panel pixel rounding, both new feel/UI checks passed, including live menu,
settings and HUD layouts at logical sizes 1280x2276, 1920x1080 and 2400x1080, with both control hands.
Actual velocity remains independent of mass, the camera/perk framing check passed, and body growth
was verified to advance gradually and settle while mass updates immediately. Menu, settings and
HUD captures are under `Temp/FeelAudit/`.

The separately selected 600-second controlled feeding probe passed: masses 0.35 / 0.8 / 1.4 /
2.0 / 4.0 were reached at 14.6 / 40.4 / 84.2 / 147.6 / 384.8 simulated seconds. End: t=600.0s mass=4.7938; defeated=False
The pilot uses randomized food, no boosts, no perks, and no opponents or events; these results are
not human-play averages and do not validate section 4's human pace targets. Raw output:
`Logs/GrowthPace.txt`. Phone touch comfort, safe-area behavior and hands-on steering remain device checks.

### Host migration acceptance

Implemented components include `MatchMigrationStore`, `MigrationWorldService`, gameplay migration partials,
`SessionMigrationDataHandler` and the recovery path in `NetworkSessionService`. Initial verification passed
5 `MatchMigrationTests` and `Migration_RestartsNgoAndRestoresWorldWithoutStartingANewRun` in one process.
Use `HostMigrationPeerProbe` (menu: `Planet IO/Tests/Build host migration probe`) for independent peers.

Still required on separate processes/devices with real Relay: graceful host exit, forced termination,
transport loss, late join during recovery, two consecutive host departures, and one surviving player.
Compare ownership, mass, loot nutrition, perks, moons, progression and event timers before and after recovery.
Require exactly one authoritative arena, no new lives or duplicate loot/rewards, bounded and measured
abrupt-loss rollback, and correct ordinary client exit and intentional shutdown. See section 5 for the
snapshot and recovery design. A single-process restore does not establish network continuity.

### Android startup and rewarded ads (November 15, 2026)

On Sony XQ-BC52, Android 13/API 33, ARM64, a pre-scene Burst crash in
`WeakAssetReferenceLoadingSystem.OnCreate` was traced to an old AOT artifact traversing eight-byte
pointers in four-byte steps. The exact internal compiler-cache defect was not isolated. The build
preprocessor invalidates `Library/BurstCache/Android-Arm` and `Temp/BurstOutput` for this Editor version;
Burst stays enabled. A rebuilt IL2CPP/ARM64 APK passed three cold starts, menu/match, minimum-mass
boost, respawn and return-to-menu checks without a new native crash. Evidence remains under `Logs/`.

Ad consent initialization creates `MobileAdsEventExecutor` before queuing callbacks, avoiding a cycle
where the consent callback waits for the executor that ad initialization would create. Regression,
localization/UI and App ID checks passed; a Google test video unlocked a skin on the same device,
and the unlock persisted after an APK update and restart (version 1.0.1, code 4).
Android App ID: `ca-app-pub-7173647303121367~8143078815`;
rewarded unit: `ca-app-pub-7173647303121367/2914802868`.
Development builds use Google's test unit; `PLANETIO_TEST_ADS` also enables test ads for a release test build.
Production UMP returned "no form(s) configured for the input app ID". Publishing the appropriate AdMob
privacy message and verifying production consent/ad delivery remain release checks. Successful test ads
do not establish production readiness. Reference: [Google UMP setup](https://developers.google.com/admob/unity/privacy).

### Correctness review verification (November 18, 2026)

An independent Windows Editor snapshot passed 608/608 EditMode tests (including Android player-script
compilation) and 83/83 GameFlow PlayMode tests, with no failures or skips. Reports: `Logs/Review.Latest.EditMode.xml`
and `Logs/Review.Completed.PlayMode.xml`. The verification copy uses a separate profile and local port 27931;
the main project keeps its existing profile and port. Main-workspace sources were edited concurrently, so
these results apply to the tested snapshot and do not validate subsequent parallel edits.

Regressions cover qualified leaderboard totals and cloud restore, uploads requested during an active save,
pause/failure handling, and food eaten before deferred Ablative Crust healing. Tier-offer fixture setup and
cold-menu waits were stabilized. UI aspect checks use physical-pixel rounding bounds rather than a fixed
ratio tolerance; the temporary minimap flex-shrink experiment was reverted. Device and real Play Games
acceptance checks remain separate.
