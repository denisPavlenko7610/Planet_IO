# Growth readability and perk UI verification

Date: November 16, 2026. Editor: Unity 7000.0.0a16, Windows.

## Changes checked

Camera exponent 0.15; growth zoom holds for 0.35 s and eases outward with a 0.9 s smooth time.
The body size curve and base movement speeds are unchanged. Moon framing accounts for viewport aspect and look-ahead.
Solar Sail retains +12% movement speed and -10% food. Ion Trail adds +15% boost speed with its existing +25% boost cost.
Three perk choices appear in a compact top dock inside the safe area. Nine vector icons replace letter badges;
localized effect/cost descriptions were shortened in all 14 languages and regenerated into the String Tables.

## Results

- 53 selected EditMode tests passed: SizeAndFeelRulesTests and GravityAndPerkRulesTests.
- 5 selected PlayMode tests passed: actual movement at four masses, camera and rendered perk layout,
  perk selection, timeout selection, and the explicit growth-pace probe.
- Actual player velocity was 4.8 normally and 9.6 while boosting at masses 0.1, 0.35, 0.8 and 2.0.
  Ion Trail raised boost velocity to 11.04 without changing normal velocity. Solar Sail raised normal velocity to 5.376.
- Screen diameter grows approximately 3.07 times between mass 0.081 and 2.0 after the camera settles.
  The numeric rule gives 3.085 times between 0.08 and 2.0, versus approximately 2.03 with exponent 0.28.
- The rendered top dock leaves the center and both thumb controls visible. Its bottom stays above 46% of viewport height.
  Vector icons were checked as actual imported UI Toolkit VectorImage assets.
- Compilation succeeded after repairing stale junctions to shared modules moved from Framework/Presentation to Core
  in the sibling UnityTemplates checkout. The linking script was also checked for a repeat run.

## Growth-pace probe

One run, 600 simulated seconds at time scale 8, using the actual movement, food pickup, magnet and respawn systems.
A nearest-food pilot starts close to food. Opponent collisions, world events, gravity, boosts and perks are disabled.
This is a controlled automated probe, not a human or Android playtest and not a representative match average.

| Elapsed simulation time | Mass |
|---|---|
| 60 s | 0.5324 |
| 180 s | 0.9854 |
| 240 s | 1.3745 |
| 360 s | 1.4570 |
| 480 s | 1.4570 |
| 600 s | 1.4570 |

Milestones: 0.35 at 21.6 s; 0.8 at 106.4 s; 1.4 at 242.8 s; 2.0 was not reached.
The late plateau means this pilot did not establish uninterrupted feeding for the full run. Its cause was not isolated;
these numbers must not be used to claim that GDD section 4's human-play targets pass or fail.
No food balance was changed based on this single probe. A hands-on mobile run is still needed for growth pace,
perceived motion, localized text legibility and comfort during combat.

The explicit probe is GrowthPace_ContinuousFeedingRecordsMilestones. Its raw output is written to Temp/GrowthPace.txt.
The visual regression check writes Temp/PerkGrowth.png.
