# Android startup crash, 2026-11-15

Device: Sony XQ-BC52, Android 13 / API 33, ARM64. Editor: Unity 7000.0.0a16.

The original crash was a SIGSEGV while `WeakAssetReferenceLoadingSystem.OnCreate`
called `EntityDataAccess.EndStructuralChanges`, before application scene startup.
The crashing Burst library's Build ID was `93429454172843bd`; the identical binary
was found in the project's Android Burst AOT cache and preserved under `Logs/`.

Disassembly at the fault identifies the error, rather than merely the library:

```text
37b774: lsl  x9, x9, #2
37b780: ldr  x11, [x11, x10]
37b784: add  x10, x10, #4
37b78c: str  wzr, [x11, #0x90]   // SIGSEGV
```

This AArch64 library iterated the `MatchingQueryData` pointer list in four-byte
steps while loading eight-byte pointers. The next iteration combined halves of
adjacent pointers. In the device register dump, `x11 = 0x19abc3c00000006f` produced
the reported fault address `0x00abc3c0000000ff` after the offset and Android's
ignored top byte. The rebuilt implementation uses `lsl #3` and `add #8` instead.

The cache also contained an ELF32 ARM library and both bad and correct ELF64
AArch64 libraries. This is evidence of an incorrect pointer-size specialization
in an old AOT artifact; the exact internal compiler-cache defect has not been
isolated in Unity's implementation.

The project fix keeps Burst enabled and preserves the project's ARM64 target in
the release build helper. For this specific Editor version, a build preprocessor
invalidates `Library/BurstCache/Android-Arm` and `Temp/BurstOutput` before Android
builds. `CleanBuildCache` alone left the separate Burst cache intact; removing
the generated plugin also forces incremental builds to invoke Burst again.

Evidence retained in the ignored `Logs/` directory:

- `android-startup-crash-2026-11-15.log`: original device tombstones.
- `original-crashing-burst.so`: original binary, SHA256
  `E292F901937C712C93A33B3FA2F714890CA3EF45A6FCF8B1B9E05B207690872D`.
- `original-burst-crash-disassembly.txt`: instructions around the fault.
- `android-startup-with-burst-live.log`: successful startup with Burst enabled.
- `android-startup-with-burst.png`, `android-bots-with-burst.png`: device captures.

The first clean ARM64 build with Burst enabled reached the main menu and a bot
match on the same device. Android version code was raised from 1 to 2.

## Final build and mass rules

`Builds/Android/PlanetIO.apk` was rebuilt after the gameplay changes, with Burst
enabled, IL2CPP, ARM64, version 1.0 / Android version code 2. The build completed
with zero errors. Its Burst compilation log reports `Compiled` for every method.
The packaged library has Build ID `c1bcd3a482810358`; its `.text` section matches
the verified compiler output byte for byte (SHA256
`9A0FA2F33A7ABE38A065ECC70D0678912932AA08D2DF1402D227A358CE29A67F`).

The player now starts at the minimum displayed mass of 8. Boost requires enough
surplus mass to pay its next cost while staying above that minimum, and turns
off immediately after its last affordable payment. Initialization at the minimum
does not trigger defeat; actual damage below an already-reached minimum still
does. Respawn and continue support this minimum starting mass.

Focused verification passed: both boost PlayMode tests, the minimum-mass
respawn/damage test, all 10 numeric edge cases and both prefab capacity-bound
checks. The broader PlayMode run also exposed two outstanding satellite reset
failures: `Moons_DefeatClearsSatellites` and
`Moons_ContinueDoesNotRetainBrokenMoonFromPreviousLife`. These call the existing
defeat path, which this fix does not modify.

Final device verification: three cold starts reached the menu without a crash;
a bot match started at mass 8; holding boost at mass 8 did not activate its
effect or consume mass; respawn and return to the menu worked. No application
exception or new native crash appeared in the captured final player logs.
The APK's SHA256 is
`50CF08BED3DEC528BFDDB8EA35D164043D4B8D528B212AF48EBF69EE93D189C0`.
The original App Bundle build preference was restored after APK verification.
