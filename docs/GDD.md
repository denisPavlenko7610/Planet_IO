# Planet IO: GDD and development plan

Updated: November 13, 2026. Implementation status was checked against the working tree, including uncommitted changes. Code
being present does not mean a mechanic is verified; earlier test results are in the
[testing report](Testing-2026-11-13.md). Rows about size, speed, camera, food, magnet and boost were updated on
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

| Area | Now |
|------|-----|
| Mass | `Capacity` 0.08 → 2.0, start 0.08; scale = 0.08 × √(mass / 0.08) (`PlanetSizeRules`): size varies 5×, not 25× |
| Eating | contact, if mass ≥ 1.08× the victim's; base absorption 40%, loot 60%; the Predator perk and bounty raise absorption |
| Speed | player 4.8 (boost 9.6), bot 3.6; no mass penalty: a big planet is neither faster nor slower than a small one (as in Slither.io) |
| Camera | base size 3.6 (was 2.5), zoom = 3.6 × (mass / 0.08)^0.28, look-ahead of 16% of the size along the heading, +8% size while boosting |
| Boost | 0.004 mass every 0.15 s, drops food worth 80% of the cost beyond the body edge (body radius + 0.3); the owner's magnet ignores its own drop for 1 s |
| Border | movement is blocked at the edge (`WorldBounds.KeepInside`); entering the zone costs max(regular boost cost, 8% mass); red vignette when close |
| Food | pool of 2250 `NetworkObject`s (was 1800), 16 migrating fields (45 s), golden 4% (×8); client-side visual ×1.2 × zoom^0.7, capped at ×3 (`FoodViewScale`), collider unchanged |
| Enemies | pool of 16 bots, 3 personalities (Cautious/Balanced/Hunter), 4 intents (Roam/Forage/Hunt/Evade) |
| Comets | pool of 30, speed 2.5–3.5, damage 0.02 × comet mass; permanent names, a visual tail, a limited food trail (new, needs playtesting) |
| Network | NGO + UGS Sessions, rooms by code (default 8, max 16), owner-authoritative movement |
| Meta | 14 languages, skins (some for ads), continue via rewarded (50% mass), leaderboard, minimap |
| Tiers and moons | 4 tiers, base slots 0/2/4/6; server-side strikes, comet interception, ring clashes; HUD and camera account for moons |
| Gravity | pull and slingshot in `PlanetGravity`; the owner simulates the player, the server simulates bots |
| Perks | 9 perks, server-side offer of 3 cards with a timer, pick screen in the HUD; active perks are colored letter badges under the score |
| Leader | ring, minimap highlight, bounty +25%; minimum mass 0.4, refreshed every 0.5 s |
| Progression | rank (30), stardust, 3 missions a day, streak, results screen, FTUE, 12 skins (4 free); each new rank pays stardust 5 + rank |
| Respawn | 8 random candidates, the best by "food nearby + distance from bodies that can eat you" wins (`RespawnRules`) |
| Movement arrow | constant on-screen size (3.5% of screen height), at the body edge, does not grow with the planet (`DirectionArrowRules`) |
| Main menu | on screen: nickname and planet (profile chip), Play, Blitz, Online, mission and settings icons; nickname, color and skins live in the Profile window; quick play, room and code live in the Online window |

### 2.1. Scope decisions (November 16, 2026)

Selection criteria. We take what (1) protects the player's first minute, (2) is needed to ship the update safely, or (3) is
cheap and noticeably changes the feel. We defer anything that depends on art, plugins or metrics we do not have yet. We
drop what duplicates something that already works. The feel reference is Slither.io ([AGENTS.md](../AGENTS.md)).

**Taking into the update**

| What | Why | Phase |
|------|-----|-------|
| Full PlayMode run without failures, moon-reset regressions | We cannot ship with a known death/continue defect; the reset exists in code, the tests are not confirmed | 5 |
| FPS and traffic measurement on a weak Android device with 2250 food | The only confirmed performance risk; the pool grew by 25% | 5 |
| Host/client check: replication, late join, reconnect | Rooms by code are part of the update; the 13 Nov report marks this Not Run | 5 |
| Smart respawn | The first minute after death decides "play again"; it used to be a random point, often empty or next to a big body | 6 |
| On-device tuning: camera, speed, food, magnet, boost cost, growth pace | All changed on 16 Nov without hands-on checks; boost now costs the full mass | 6 |
| Active perk badges in the HUD | A perk with no indicator cannot be read, so counter-play is impossible | 6 |
| Rank reward: stardust `5 + rank` | The "death is a step forward" pillar; ranks without a prize are empty; about 600 over 30 ranks, less than half the price of 8 paid skins | 6 |
| Three new sounds: moon hit, tier-up, event warning | They used to be Kill/Hit at another pitch; sound is the main combat feedback | 6 |
| Threat ring: shape coding for color-blind players | Cheap; color used to be the only carrier of meaning (done without a setting) | 6 |
| AAB build, device smoke test, store listing and screenshots | Release | 7 |

**Deferring (after the update ships)**

| What | Why |
|------|-----|
| Google Play Games and server-side movement validation | Needs a plugin and anti-cheat; without a global ranking there is no incentive to cheat; rooms have a local leaderboard |
| Cosmetics beyond skins, shop screen | Needs art; for now stardust is spent on the 8 paid skins |
| IAP: remove ads, starter pack, season pass | "Remove ads" is pointless until there are interstitials; the rest depends on retention data |
| Interstitials | Only after D1/D7 from Play Console |
| Reroll the 3 perk cards for rewarded | A monetization experiment, after the first perk data |
| Leader pointer | The minimap beacon and the ring already exist; Slither.io has no arrows |
| Leader "burnout" | Enable only if playtests show a snowball |
| Supernova on a tier III+ death | Decoration; events already produce loot rings |
| Deeper bot AI: slingshot, avoiding wells, hunting with moons | Bots already avoid moons; further work depends on playtest data |
| External analytics SDK | Play Console gives D1/D7 and crash-free; funnels stay local (`LocalAnalyticsService`) |

**Dropped from the plan**

| What | Why |
|------|-----|
| "Solar system" teams | A separate game in scope: matchmaking, team balance |
| "Solar flare" event | Three event types are enough |
| Destroying enemy moons with boost | Moons already break on ring clashes |
| Gravity strength as a fraction of target speed | 1.4 u/s at speed 4.8 is about 29% against a 35% goal; retuning does not pay off |
| Push-back at the border | Movement is already blocked, the mass loss and the vignette work; a push would be a second punishment for the same thing |
| Comet-name toggle | Names are now permanent; no separate setting is needed |

Everything else in this GDD stays unchanged. The roadmap is in section 14.

**Status on November 16 (evening).** Implemented in code, not yet checked on a device: smart respawn, perk badges, rank
reward, a thick danger ring (shape coding instead of a color-blind setting), the small movement arrow, the main menu split
into windows, three new sounds (Kenney, CC0: moon hit, tier-up, event warning), and the comet-name setting removed (names
are always shown). Not done: Android measurements and host/client checks. Tests: EditMode passed 413 of 413 before the
comet work in progress; the last full PlayMode run passed 53 of 55, and `World_WormholeTeleportsOncePerCooldown` fails
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

| Scale | What the player does | Reward |
|-------|----------------------|--------|
| 5–30 s | Moves to food, avoids big planets, risks boost | Mass growth, a sense of motion |
| 1–3 min | Reaches a tier, picks a perk, hunts or hides | New moon, new perk, camera scale change |
| 5–10 min | Becomes the leader or dies | Bounty, loot, run result |
| Between runs | Looks at the results, spends stardust, takes a mission | Rank, cosmetics, daily streak |

Target pace to verify: mass 0.35 in about a minute, 0.8 in 3–4 minutes, 1.4 in 6–8 minutes, 2.0 in 8–10 minutes of
continuous feeding. Tier-ups every 1–3 minutes should give a regular perk pick.

## 5. Comparison with Slither.io

Controls (November 16, 2026): the floating joystick stays the default. Settings have a persisted "Fixed joystick" toggle:
the base is pinned to the bottom corner of the chosen side (it honors "Joystick on the right"), the stick deflection is
limited to a radius, and the base does not follow the finger. A touch in the control zone sets the direction relative to
the pinned center; releasing or cancelling the touch zeroes the input. Switching the mode applies immediately and resets
the current touch. This is an extra mobile-comfort setting added at the user's request; the core Slither.io principle
(finger sets the direction, a second finger boosts) holds in both modes.

| Slither.io | Planet IO now | Planet IO target |
|------------|---------------|------------------|
| Tail body | Orbiting moons | Tune positional combat |
| Enemy hits your tail and dies | A moon touch hurts and knocks back | Verify a small player can beat a big target |
| Encircling (circle kill) | Moon ring and gravity | Verify the encircling tactic |
| Boost costs length | exists; ion trail is a perk | Develop trail cosmetics |
| Loot on death | exists | Keep; add a "supernova" for big planets |
| Crown for the top 1 | Leader ring, minimap beacon, bounty | Add a pointer toward the leader |
| Skins | planets | Planets + moon sets + trails + explosion effects |
| Speed independent of length | equal for all masses | Keep; EditMode test for the invariant |
| Zoom out while growing | power-law zoom 0.28, look-ahead, boost zoom | Tune in playtests |
| Food readable at any zoom | visual ×1.2 × zoom^0.7 | Tune in playtests |

Principle: decisions about game feel (speed, camera, size, food, boost) are checked against the best practices of
Slither.io and similar genre games; any deviation is recorded here. The rule is recorded in [AGENTS.md](../AGENTS.md).

Comets are a deliberate addition to the Slither.io reference: a predictable moving hazard creates the choice "avoid it or
follow it for food". We keep the names at the user's request, with labels about 25% smaller. The trail redistributes
regular food and does not create new mass.

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
  network objects, no extra mass, and golden food, boost drops and loot are never moved. The trail is capped at 90 points
  per world; after 8 seconds an uneaten point returns to its field. Pause stops both movement and trail creation.
- Damage stays 0.02 × comet mass; a moon intercepts the hit, and the Comet Rider perk restores 50% of the damage.
  Strengthening the damage is decided separately after playtests.
- The meteor shower keeps its 6 s warning, the strip, 12 consecutive passes at about 8 u/s, and 5 loot piles after the
  event; it uses the same tails and the capped food trail.
- Checks: EditMode for safe spawning and the trail budget, PlayMode for movement, respawn, interception, labels and the
  tail at different zooms; host/client and a repeat measurement of the growth pace from section 4. The numbers are
  starting parameters for playtests, not confirmed balance.

### 6.1. Unchanged

Finger control, perpetual motion, food and fields, field migration, golden food, bots with personalities, rooms by code,
2 s spawn protection, continue via rewarded, skins. This is the working base; touch it only based on playtest results.

### 6.2. Tiers (Evolution)

| Tier | Name | Mass | Satellites | Perk | Camera |
|------|------|------|------------|------|--------|
| I | Asteroid | 0.08–0.35 | 0 | none | close |
| II | Planet | 0.35–0.8 | 2 | pick 1 of 3 | farther |
| III | Gas giant | 0.8–1.4 | 4 | pick 1 of 3 | farther |
| IV | Star | 1.4–2.0 | 6 | pick 1 of 3 | maximum |

Rules:

- A tier-up comes with a flash, haptics, new moons appearing and a perk pick screen (3 cards, 5 seconds to choose,
  otherwise random).
- Dropping below a tier threshold removes the tier, but **perks stay** until the end of the life (otherwise the penalty
  is doubled).
- A new life resets perks; continue keeps perks and returns 50% of the mass. Satellites reset on death and form again on
  revival according to the current tier.
- Progressive disclosure: on tier I the player only knows "eat and run"; moons and perks are explained when they appear.

Current differences: a tier drop has a hysteresis of 0.03 mass (`TierRules`); a perk pick is granted on the first
arrival at each new tier in a life. The server adds a 0.75 s margin to the 5 s pick window. Cards and tutorial are not
wired yet. Resetting moons on death does not match the rule above; this is a known defect, not a design change.

### 6.3. Moons (tail replacement)

Satellites orbit the planet and let it hurt a bigger opponent without a size advantage.

| Parameter | Value |
|-----------|-------|
| Orbit radius | 1.7 × body radius (collider radius × scale) |
| Angular speed | 90°/s, all moons turn the same way, each planet has its own starting phase |
| Moon radius | 0.2 × body radius; visual is a shrunken copy of the owner's skin |
| Damage to another planet on moon touch | max(0.004, 6% of its mass), effectively 6% at mass ≥ 0.067 |
| Knockback | 6 u/s, fades in ~0.4 s; the target is immune to any moons for 1.0 s |
| Moon durability | 3 hits, then it disappears; regrows in 12 s |
| Moon vs moon | "ring clash": no damage, both planets are pushed back at 3 u/s, 0.8 s pause, flash at the contact point |
| Moon vs comet | the moon intercepts the comet: the comet respawns, the moon takes a hit, the planet takes no damage |
| Lost mass | 50% of the damage goes to the moon's owner immediately (no food falls on the ground) |
| Own moon | does not hurt its owner; under spawn protection moons neither hit nor get hit |

Implemented in code:

- Tiers and moon slots (`TierRules`, 0/2/4/6), moon state (`MoonLoadout`), geometry and damage (`MoonRules`).
- Server logic for strikes, comet interception and ring clashes in `PlanetScale.Moons.cs` (strikes every 0.04 s, rings
  every 0.12 s).
- Moon display `MoonRingView`, threat ring `ThreatRingView`, knockback `PlanetKnockback`, tier-up flash.
- HUD: a Roman-numeral tier and a progress bar to the next tier under the score (`tier-panel` in `Hud.uxml`, the bar
  color depends on the tier).
- Camera: while the player has moons, the scale is at least the outer ring edge × 1.45
  (`PlayerCamera._moonViewMargin`); on a tier-up the zoom widens smoothly.
- Sound and haptics: tier-up and own-moon strike use dedicated cues (Kenney, CC0) with Success and LightImpact haptics.
- Bots treat enemy moons as a hazard: they avoid an approaching moon the same way as a comet. They cannot hunt with moons
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

| Parameter | Value |
|-----------|-------|
| Zone radius | 3 × planet radius |
| Velocity offset at the center | up to +35% of the target's speed, falling off quadratically toward the edge |
| Slingshot | moving tangentially (60–120° to the radius) for ≥ 0.5 s gives +20% speed for 2 s, then decays |
| Food | magnet: body radius + 0.2 × perk × growth (`FoodMagnetRules`); the gravity zone is not used |
| Indication | a ring around planets bigger than you: orange, pulsing |

Implemented: shared `GravityRules`, pull and slingshot in `PlanetGravity`, indication in `ThreatRingView`. The owner
computes it for the player, the server for bots. No separate network messages for pull strength were added.

Decisions: the strength is an absolute offset of up to 1.4 u/s; at speed 4.8 that is about 29%, close to the 35% goal, so
a fraction of speed is not introduced. Food magnet: body radius + `_foodAttractionRadius` (0.2) × perk multiplier ×
(body radius / 0.28)^0.7 (`FoodMagnetRules`); the gravity zone radius is not used. Bots feel the pull; tactics for
avoiding wells and using the slingshot are deferred.

### 6.5. Perks (starting pool: 9)

| Perk | Effect | Cost |
|------|--------|------|
| Ion trail | boost leaves a trail that hurts others for 2 s | boost costs 25% more |
| Magnetosphere | magnet radius ×1.5 | speed −8% |
| Heavy moons | +1 moon slot, moon damage +50% | speed −5% |
| Ablative crust | survives one lethal hit, 30% mass remains | consumed when triggered |
| Solar sail | speed +12% | food gives −10% |
| Gravity well | gravity zone ×1.4 | moons spin 20% slower |
| Nebula veil | disappear from enemy minimaps for 10 s after a boost | 40 s cooldown |
| Comet rider | comets heal 50% of the damage instead of hurting | visible glow, enemies know |
| Predator | a kill gives +20% absorbed mass | food gives −10% |

The pool is a starter set; expand it after playtests. Each perk must have a downside and one strong counter.

Status: all nine perks work. The server stores an offer of three cards, validates the pick index and picks a random perk
itself after 5 s (+0.75 s for network delay). The pick screen is `perk-panel` in `Hud.uxml`, shown by `PerkPresenter`
through `IPerkView` (`SessionHudView`); texts live in `UIStrings.csv` in 14 languages (names and descriptions should be
proofread by native speakers). Bots get no perks.

Offer rules: one pick for each new tier II–IV per life. Losing and regaining a tier gives no offer; jumping several tiers
queues one pick per tier. Death resets perks and the queue; continue via ads keeps perks.

Balance and implementation differences:

- "Gravity well" lowers movement speed by 6% instead of slowing the moons.
- The crust leaves at least the player's starting mass (currently 0.08, equal to the minimum); when it triggers on a drop
  to the minimum mass, the restore is applied on the next frame, because `Capacity` cannot be changed inside its own
  `OnValueChanged`: assigning again after that does not fire the callback.
- "Nebula veil" invisibility shows only as disappearing from the minimap and bots losing the target; there is no separate
  effect on the planet.
- "Ion trail" deals 70% of moon damage, and the victim gets the same one-second immunity as from moons.
- The "Comet rider" glow is not wired (low priority).
- Active perks are shown as badges under the score (color per perk, the letter comes from the localized name); rerolling
  the cards for ads is deferred.

### 6.6. Combat: rule summary

| Contact | Result |
|---------|--------|
| Core ≥ 1.08 × target core | eaten; the owner gets a base 40% of the victim's mass, 60% becomes loot; absorption is boosted by perk and bounty |
| Core × enemy moon | damage, knockback, the moon loses durability |
| Moon × moon | bounce |
| Planet × comet | mass damage; a moon intercepts |
| Planet × border | movement blocked at the edge, mass lost on entering the zone, red vignette; no push-back needed |
| Planet × black hole (event) | −25% mass, then food is spat out |
| Tier III+ death | deferred: "supernova", a 6 u shockwave pushes away, loot flies out in a ring over 1 s |

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

Phase 4 status: events, the map, the daily modifier and Blitz are implemented in code. The event schedule is not sent over
the network: every participant computes it from the day number (UTC) and server time (`CosmicEventRules`), only the
server applies effects, everyone draws the visuals (`WorldDirector`). One event every 90–150 s, a banner 6 s before the
start, and a minimap marker for the whole event. Types come in triples (each once in random order), so there are never
two identical events in a row.

| Event | What happens | Reward/risk |
|-------|--------------|-------------|
| Meteor shower | 12 comets cross a strip of the map (25 s) | 5 loot piles along the strip afterward |
| Supernova | a star pulsates for 30 s, then explodes | 4 big loot piles in a ring |
| Black hole | crawls for 40 s and pulls everything in | −25% mass once per event, loot at the end |
| Solar flare | dropped from the plan: three types are enough | — |

The daily modifier (`DailyModifierRules`) is chosen by day: weak gravity ×0.5, strong ×1.5, gold rush (event loot ×2) or
none; a banner shows at the 8th second of play. Gravity scales as a whole (planets, stars, the black hole), not only for
events; we keep that. Known limitation: the day is taken on each device separately, so if midnight passes during a
session the schedule can diverge between participants.

### 6.9. Map

- Now: 440 × 296, 1 home field and 16 migrating ones.
- Phase 4: 4 neutral stars (static gravity wells for the slingshot, strength 0.8, zone 14), a pair of wormholes (the
  teleport exits 5 u from the other entrance, 10 s cooldown per planet), 3 nebulae (a planet inside disappears from the
  minimap and from bots' view). Positions come from `MapFeatureRules` by day number, outside 40 u from the center, stars
  no closer than 90 u.
- Respawn: the owner scores 8 random candidates (`Player.Respawn`): score = food within radius 12 (up to 40 items) / 40 +
  2 × min(1, distance to the edge of the nearest body that can eat the player / 22). Food and planets come from
  replicated objects, so a server density map is not needed. It also applies to continue.

## 7. Progression and meta

Phase 3 status: rank, stardust, missions, streak, the results screen, buying skins with stardust, a step-by-step FTUE and
local analytics are implemented. The rank reward is done: stardust `5 + rank` for each new rank (about 600 across all 30
ranks, less than half the price of the eight paid skins); it counts toward the run's stardust and toward "Double
stardust". Deferred until after release: moon sets, trails and explosion effects (need art and a shop screen),
Google Play Games achievements and leaderboards (need a plugin and anti-cheat), and server-side leaderboard storage.

| System | Description |
|--------|-------------|
| Rank | XP per run: lifetime, peak mass, kills, tier. 30 ranks, reward: stardust 5 + rank |
| Stardust | soft currency per run, spent on cosmetics; rewarded gives ×2 |
| Cosmetics | planet skins (exist, 12 of them, 4 free); moon sets, trail, explosion, nickname frame come after release |
| Missions | 3 a day ("eat 3 planets", "reach tier III", "survive 4 min"), reroll via rewarded |
| Streak | daily login: growing reward, reset on a missed day |
| Achievements and leaderboards | Google Play Games: peak mass, kills per run, total eaten; after release together with anti-cheat |
| Results screen | killer and their tier, peak mass, time, XP with an animated rank bar, "Play again" and "Continue" buttons |

### 7.1. How it works in code

| Part | Where | Rules |
|------|-------|-------|
| XP and rank | `XpRules` | XP per run: 1 per 2 s of life (up to 900 s), 1 per 2 points of peak mass, 25 per kill, 20 per tier above the first; 30 ranks, step 100 + 40 × (rank − 1) |
| Stardust | `DustRules` | a quarter of the XP earned (minimum 1); ads double the run's stardust; a skin costs 250 |
| Streak | `ProgressionState.RefreshDay` | logging in on a consecutive day gives 10 stardust, plus 5 for each further day, max 50; a missed day resets to 1 |
| Missions | `MissionRules` | 3 a day from 7 templates, chosen deterministically by day number; one reroll a day, only for an untouched mission |
| Run accounting | `ProgressionState.ApplyRun` | each run has a unique `RunId` (UTC ticks), so "continue" and a repeated death pay only the difference |
| Storage | `ProgressionService` | `player-progress` slot in `SaveStore`; the "day" is counted by UTC on the device clock |
| Run statistics | `RunStatsTracker` | lifetime, peak mass and tier, kills, perks, killer |
| Results | `DefeatPresenter` + `IRunResultView` | time, kills, tier, killer, an animated rank bar, reward, a "Double stardust" ad button |
| Menu | `ProgressMenuPresenter` + `MainMenuView.Progress` | rank and stardust in the header, the missions screen, a "!" badge on the button when a reward is ready |
| FTUE | `FtuePresenter` | condition-driven steps: movement, food, red ring, green ring, tier II, boost; each step times out, about a minute at most |
| Analytics | `LocalAnalyticsService` | events from Appendix A go to a ring buffer of 200 entries and to the debug build log; there is no external backend |

Saving goes through `RD.Save`, day and streak timers through `RD.Timers`, haptic feedback through `RD.Haptics`, settings
through `RD.Settings`. Do not write new equivalents in the project; extend shared modules in the linked `UnityTemplates`
checkout (see the README).

## 8. Modes

| Mode | Status |
|------|--------|
| Classic arena (endless) | exists |
| Single player against bots | exists |
| Private room by code | exists |
| 5-minute Blitz with a final scoreboard | exists (single player only, enabled with a toggle in the menu) |
| Daily modifier | exists |
| "Solar system" teams | not planned |

## 9. Monetization

Principle: ads are voluntary and always on the player's request, until retention metrics show that more is acceptable.

| Placement | Type | Status |
|-----------|------|--------|
| Continue after death | rewarded | exists |
| Skin for watching | rewarded | exists |
| Stardust ×2 | rewarded | exists |
| Mission reroll | rewarded | exists |
| Reroll the 3 perk cards | rewarded | after release |
| Interstitial | only on the death screen, at most once every 3 deaths, not in the first 3 runs | after D1/D7 checks |
| Remove ads | IAP | after release, together with interstitials |
| Starter cosmetics pack | IAP | after release |
| Season pass | IAP | after release |

Ad consent (UMP) is already implemented through `IAdPrivacyService`.

## 10. Android UX

- Edge-to-edge: the game background fills the whole screen, including the area under system bars and cutouts; buttons
  and important HUD stay inside `Screen.safeArea`. System bars are hidden, appear briefly on swipe and have a transparent
  background. The release Android build uses R8 (Minify Release); the build script sets and verifies these settings. On a
  device, check both landscape rotations, gesture and three-button navigation, and returning from ads and the consent
  form. This is a platform adaptation of the interface; the .io mechanics do not change.
- Controls: a floating joystick by default, or a fixed one in the bottom corner of the chosen side (a toggle in
  settings, see section 5), and a boost button for the second finger. In-interface hints describe touches, not keys.
- **Threat ring**: the planet outline is red if it is bigger than you, green if smaller, neutral if equal. The meaning is
  also coded by shape, without a setting: a dangerous planet has a thick pulsing ring, the others a thin one.
- HUD: a mass bar with tier ticks, perk badges, a kill feed, a small movement arrow at the planet edge, and a minimap
  with the leader beacon, an event banner.
- Main menu: at most four actions on screen (profile, Play, Online, the Blitz toggle) and two corner icons (missions,
  settings). Everything else opens in separate windows, as in Slither.io, where the main screen has only a nickname,
  Play and a skin picker.
- 60-second FTUE: eat food → see the big planet's red ring and run → eat a small one → get tier II and the first perk.
  The `onboarding.tutorial-completed` key already exists.
- Haptics: moon hit, kill, tier-up, death.
- Every new screen is built on UI Toolkit (UXML/USS in `Assets/_Project/UI`), localized through `UIStrings.csv`. For 14
  languages write as little text as possible: icons and numbers instead of phrases, no more than 20 new strings per
  phase.

## 11. Art and audio

- Moons: reuse small-planet sprites from `Sprites/Planets`, one material, a `SpriteRenderer` pool.
- Effects: tier-up (flash + ring), moon hit (sparks in the strike direction), supernova (shockwave), boost trail.
- Audio goes through the existing `SfxCatalog` (Eat, Hit, Kill, Death, Boost, plus MoonHit, TierUp and EventWarning from
  Kenney, CC0); the bounty cue is deferred. Music is already delivered through Addressables.
- Store screenshots: a series of English UIs, different skins and nicknames, the middle of a run near bots and comets,
  three size sets (following the project memory rules).

## 12. Technical plan

1. **Rules in pure classes with tests.** `CombatRules`, `TierRules`, `GravityRules`, `PerkRules`, `XpRules` modeled on
   `GrowthRules`; tests in `Tests/EditMode`.
2. **Shared combat code.** Done in phase 0: the eating threshold and absorption share moved into `CombatRules`;
   `_eatSizeRatio` and `_initialCapacity` were lifted into `PlanetScale` (same field names, so prefabs do not change);
   `Player` and `Enemy` call `CanDevour`. A single `OnTriggerEnter2D` was not made: tests call it through reflection on
   `Player`, and the protection rules differ for player and bot.
3. **Split `Player.cs`.** Done as `partial`: the main file, `Boost`, `Border`, `Combat`, `Lifecycle`, `Appearance`,
   `Perks`, `Abilities`, `Respawn`. Keep serialized fields and RPCs. Extract separate components only when needed; adding a
   mechanic does not require a new architecture.
4. **Bots as the same planet object with an AI controller.** Moons and gravity are shared through `PlanetScale`; perks
   still belong to `Player`. Deeper AI (keeping moons toward the enemy, avoiding the pull zone, using the slingshot) is
   deferred; perks for bots are not planned.
5. **Network fields:** `NetworkVariable<byte>` for the tier and the moon mask, `NetworkVariable<ushort>` for perks, and
   the leader and stealth flags are implemented. The server stores the offer, and `ChoosePerkRpc(index)` validates the
   index. Widen the perk mask only if the current range runs out.
6. **Events:** the server-side `CosmicEventDirector`; the event state is a `NetworkVariable`; the banner is in the HUD.
7. **Food and network.** Profile first (the project has `ProfilerCaptures`). Step 1: a distance-based visibility filter
   (`NetworkObject.CheckObjectVisibility`, check support in the NGO version used) and a smaller pool on weak devices.
   Step 2, if that is not enough: pellets as data (structs + delta events) without `NetworkObject`. The measurement is in
   phase 5, with a 55 FPS threshold on a weak Android; the pool is currently 2250.
8. **Anti-cheat before leaderboards.** Server-side validation of speed, bounds and teleports of the owner-authoritative
   transform. Deferred together with global leaderboards (phase 8).
9. **Analytics:** `run_start`, `run_end{cause, tier, mass_peak, duration, killer_tier}`, `perk_offered/picked`,
   `ad_offered/accepted`, `tutorial_step`, `mission_complete`. There is no external SDK: D1/D7 and crash-free come from
   Google Play Console, funnels from `LocalAnalyticsService`.
10. **Tests:** EditMode for rules, PlayMode for moon hits, tier-up and perk picks with synchronization checks.

## 13. Plan: remove, add, rework

### Remove

| What | Why |
|------|-----|
| Duplicated combat logic in `Player` and `Enemy` | Every new mechanic is written twice and balance drifts apart |
| The `ShowCometNames` setting and its toggle | Comet names are always shown, in a smaller font |
| Random respawn anywhere on the map (`Constants.RandomWorldPosition` in `TeleportTo`) | The player appears in the desert; replace with smart respawn (see "Rework") |
| Dead README links to `Docs/Food-GDD.md` and `Docs/Template-Sources.md` | The documents were removed in a6a0037; links fixed in phase 0 |
| "Score = mass × 100" as the only metric | Replaced by XP and rank; the number stays only as peak mass in the results |

Status on November 16: duplicated combat logic, dead links and the single "score" metric are gone; random respawn was
replaced in phase 6; comet names stay and are always shown, only the visibility setting is removed.

### Add

| What | Phase |
|------|-------|
| Tiers, moons, combat rules `CombatRules` | 1 |
| Threat ring (size readability) | 1 |
| Camera and HUD for tiers | 1 |
| Gravity and slingshot | 2 |
| Perks and the pick screen | 2 |
| Leader beacon, bounty | 2 |
| Results screen, rank, stardust | 3 |
| Cosmetics: moons, trail, explosion | after release |
| Daily missions and streak | 3 |
| 60 s FTUE | 3 |
| Google Play Games: leaderboards and achievements | after release |
| Cosmic events | 4 |
| Blitz mode and the daily modifier | 4 |
| Neutral stars, wormholes, nebulae | 4 |
| Analytics | 0 (event schema), 3 (funnels) |

### Rework

| What | How |
|------|-----|
| `Player.cs` (643 lines) | Done (`partial`), see 12.3 |
| `Enemy` | Done: a bot is a planet plus an AI controller with a shared combat component, see 12.4 |
| Border | Done: red vignette, blocking at the edge, 8% loss; the push-back is cancelled |
| Comets | Telegraphed in advance, intercepted by moons, part of events |
| Boost | Same cost, add a cosmetic trail; the Ion trail perk makes it a weapon |
| Respawn | Done: near food and away from bodies bigger than you (`RespawnRules`) |
| Food over the network | Phase 5: measure; distance-based visibility only if the budget is not met, then pellets as data |
| Death screen | Exists: killer and their tier, rank progress, "Play again" more prominent than "Continue"; perk reroll deferred |
| Movement (owner-authoritative) | Server validation after release, together with global leaderboards |
| Camera | Done: smooth power-law zoom by mass (as in Slither.io) with look-ahead; tiers only set a minimum view for moons |

## 14. Roadmap

Estimates are for one developer, in weeks of active work. Phases 0–4 are implemented in code; the remaining phases lead to
the update release and are ordered by the rules in section 2.1.

| Phase | Duration | Content | Exit criteria |
|-------|----------|---------|---------------|
| 0–4. Foundation, moons, gravity, perks, meta, events | done | Sections 6–8; rank, stardust, missions, streak, FTUE, events, Blitz, map features | Code implemented; on-device verification not finished (the 13 Nov report has scenarios marked Not Run) |
| 5. Stabilization | 1 | Full PlayMode without failures, moon-reset regressions, host/client on two devices, FPS and traffic measurement on a weak Android with 2250 food | All tests green; ≥ 55 FPS on a weak Android; if over budget, distance-based food visibility (12.7) |
| 6. First minute and feel | 1–2 | Done in code: smart respawn, perk badges, rank reward, threat ring shape, small movement arrow, menu split into windows, three sounds. Left: on-device tuning of camera, speed, food, magnet and boost cost | A series of playtests (≥ 5 people): growth pace from section 4 within ±25%; ≥ 80% finish the FTUE; on tier II testers kill a tier III planet in at least 1 of 5 attempts |
| 7. Update release | 1 | AAB build, device smoke test, closed testing, store listing and screenshots | crash-free ≥ 99.5% in closed testing; D1 ≥ 35% from Play Console after release |
| 8. After release | by data | Google Play Games and anti-cheat, cosmetics and a shop, IAP, interstitials, rerolls for rewarded, deeper bot AI | By retention and monetization metrics |

If time is short, cut in this order: first the sounds, then the rank reward, then the perk badges. Do not cut
stabilization, smart respawn and on-device tuning: without them the update ships with unverified feel and a risk to the
first minute.

## 15. Metrics and risks

Target hypotheses (not guarantees; recheck against test data):

| Metric | Target |
|--------|--------|
| D1 / D7 | 35–40% / 8–12% |
| Session length | 7–10 min, 4+ runs |
| Rewarded acceptance | ≥ 25% of shown offers |
| Crash-free | ≥ 99.5% |
| FPS on a weak Android | ≥ 55 |

| Risk | Mitigation |
|------|------------|
| Too many mechanics for a 5-second pitch | Disclosure by tier, FTUE, threat ring |
| Network load | Moons are computed analytically, perks sit in a bit mask, events are one `NetworkVariable`; food (2250 objects) is measured in phase 5 |
| Leader snowball | Bounty, burnout above 1.2 if needed |
| Perk imbalance | A pick-rate table in analytics, a downside on every perk, all numbers in a `ScriptableObject` |
| Localization cost | Icons and numbers instead of phrases, ≤ 20 strings per phase |
| Cheating | No global leaderboards before phase 8; server validation is mandatory before they are enabled |
| Low player count | Bots fill an empty room, the mode does not depend on the number of humans |

## 16. Assumptions and open questions

The plan for phases 5–8 is built on the assumptions below; confirm or replace them:

1. The audience is casual: short runs, 9 perks, gravity without manual tuning. We do not deepen the system.
2. Bots come first (single player and Blitz); rooms by code are an extra mode, so anti-cheat and matchmaking are deferred.
3. The time to the update release is 3–4 weeks of active work (phases 5–7).

## Appendix A. Analytics schema

Event names use snake_case, parameters are flat, values are only numbers and short strings. Do not include player IDs or
nicknames in events.

| Event | Parameters | When |
|-------|------------|------|
| `session_start` | `mode` (solo, quick, room), `bots` (count) | entering the arena |
| `run_start` | `run_index`, `skin_id` | the planet appears |
| `run_end` | `cause` (planet, bot, comet, border, boost), `duration_s`, `mass_peak`, `tier_peak`, `kills`, `killer_tier` | death |
| `tier_up` | `tier`, `time_in_run_s` | reaching a new tier |
| `perk_offered` | `tier`, `perk_ids` (comma-separated) | cards shown |
| `perk_picked` | `tier`, `perk_id`, `auto` (0/1), `rerolled` (0/1) | perk chosen |
| `moon_hit` | `dealt` (0/1), `victim_tier` | moon hit, sample 1 in 5 |
| `event_start` | `event_id` | a cosmic event starts |
| `ad_offered` | `placement` (continue, skin, dust_x2, mission_reroll, perk_reroll) | offer shown |
| `ad_result` | `placement`, `accepted` (0/1), `completed` (0/1) | viewing result |
| `tutorial_step` | `step`, `skipped` (0/1) | FTUE step |
| `mission_complete` | `mission_id` | mission completed |
| `shop_purchase` | `item_id`, `currency` | purchase with stardust or IAP |

Funnels for checking hypotheses: FTUE (steps in sequence), first run → second run, death → continue / "play again",
tier II → tier III, ad offer → acceptance.