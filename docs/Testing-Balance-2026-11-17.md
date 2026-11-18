# Balance and damage feedback verification

November 17, 2026. Unity 7000.0.0a16, running Windows Editor, current working tree.

## Changes

- Player base/boost speed: 4.8/9.6 to 4.0/8.0; bot base speed: 3.6 to 3.0.
  Mass does not affect speed. Gravity pull is reduced proportionally to preserve its relative strength.
- Ambient food nutrition: x2 to x3; food growth falloff: 1.0 to 0.5. Explicit dropped-food budgets remain exact.
- Camera growth exponent: 0.15 to 0.10 in both code and the Game scene. Area-based body growth and cap 8 remain.
- Moon damage: 6% to 10% of victim mass. Durability: two hits; regeneration: 16 seconds.
  Ring clashes spend durability on the contacting moons on both sides, leaving visible gaps.
- Eligible bots use bounded moon raids, retreat on damage or timeout, and respect an attack cooldown.
  Per-step steering prevents them from crossing the target core while seeking a moon attack position.
- Confirmed damage sends an owner RPC for feedback, separately from capacity changes and boost costs.
  The HUD combines its red damage flash with the border warning, animates a negative score popup with
  rise/pop/fade and brief hit coalescing, and clears effects on results, revive, player change or disable.
  Camera shake is proportional to view size. Existing sound and haptics remain.

## Checks

All 534 project EditMode checks passed, including the unchanged large/small speed invariant, ambient nutrition,
explicit drop budgets, camera growth, moon loadouts, migration state validation and raid geometry/retreat rules.

Selected PlayMode checks passed:

- Eight moon scenarios: tier replication, strike damage and transfer, shared immunity, protected attacker/victim,
  comet interception and durability, death/continue reset, and real ring clashes opening a larger core attack.
- Actual base/boost velocity at masses 0.1 through 8, including movement perks.
- Visible body growth through mass 8 and camera/perk panel framing.
- Confirmed damage produces flash, shake and the correct negative score; boost costs and direct capacity resets do
  not produce damage events. Effects expire, preserve the border warning, and clear on results.
- Comet, moon and border damage continue to respect Easy/Medium/Hard tuning.
- Bot moon raids enter attack state, retreat after their time limit or a hit, and honor the cooldown.
- The explicit ten-minute growth probe completed with no death or pickup stalls.

The [damage feedback capture](screenshots/DamageFeedback-2026-11-17.png) shows the local hit flash and negative score.

## Growth probe

The existing nearest-food pilot ran 600 simulated seconds at 8x time. Ambient pool: 1800.
No boost, perks, opponents, gravity or world events; food fields and golden food remain randomized.
The measured movement speed was 4.0 throughout. Raw output was copied to `Temp/Balance-GrowthPace.txt`.

| Mass | First reached, simulated seconds |
|------|----------------------------------|
| 0.35 | 8.8                              |
| 0.80 | 27.0                             |
| 1.40 | 75.6                             |
| 2.00 | 114.7                            |
| 4.00 | 281.1                            |
| 8.00 | Not reached                      |

Final mass: 5.4230 at 600 seconds. This is one randomized run, not a controlled before/after average.
The automated pilot is substantially faster than the human first-evolution target and removes combat interruptions.
Do not interpret these milestones as human session pacing or evidence that the cap is too low.

## Limits

This pass did not run the full PlayMode suite, two independent network peers or Android device measurements.
The smaller-core attack and bot retreat checks verify that counterplay is possible in the implementation;
they do not establish the GDD's human tier-II-versus-tier-III win-rate target. Human playtesting is still needed
for first-minute speed, visual growth, damage-feedback intensity and sustained competitive balance.