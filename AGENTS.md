# AGENTS.md

## Language

- Write everything in English except localization: the GDD, docs, this file, commit messages, test names and assertion
  messages.
- Other languages belong only in localization data (`Assets/_Project/Localization/UIStrings.csv` and the generated tables).

## Design source of truth

- [Docs/GDD.md](Docs/GDD.md) holds the target design and implementation status. Change the GDD first, then the code.
- When the GDD is silent or a call is contested, follow best practice from .io games, with Slither.io as the main
  reference (then agar.io, Wormate.io, Snake.io). Adapt it to planets, moons and gravity, and record the decision in the
  GDD (section 5).
- If a decision departs from Slither.io practice, record the reason next to the mechanic in the GDD.

## Guardrails

- Movement speed does not depend on size; the test `Speed_BigPlanetsAreNeverFasterThanSmallOnes` guards this. Do not
  weaken it without a GDD decision.
- Put movement, size, camera and food rules in pure classes under `Runtime/Core` with EditMode tests.
- After changing size, speed, zoom or the magnet, re-measure the growth pace from GDD section 4 in a playtest.
