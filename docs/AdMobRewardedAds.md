# Rewarded skins on Android

## Initialization fix

Consent callbacks were queued through `MobileAdsEventExecutor.ExecuteInUpdate` before the executor existed. The executor
was otherwise created by `MobileAds.Initialize`, which was called only after the queued consent callback. Initialize the
executor before requesting consent to break this dependency cycle.

The skin selector now reports unavailable ads instead of silently ignoring an unsuccessful request. SDK warnings remain
visible in Android release logs.

## Identifiers

- Android app: `ca-app-pub-7173647303121367~8143078815`
- Rewarded unit: `ca-app-pub-7173647303121367/2914802868`

Development builds use Google's rewarded test unit. `PLANETIO_TEST_ADS` can be passed as an extra build define to test
the release compilation with test ads; it is not enabled in project settings or the final APK.

## Verification (2026-11-15)

- Consent callback executor regression test passed.
- All 16 localization/UI checks and Android App ID validation passed.
- Sony XQ-BC52, Android 13: Google's test video displayed, reported "Reward granted", and unlocked and selected the
  orange skin.
- Installed the final APK, version 1.0.1, Android version code 4. The menu launched and the unlocked skin persisted
  after the update and restart.
- Decompiled final Android managed input confirms normal release selection of the production rewarded unit.

## Outstanding account configuration

The device received a UMP publisher configuration error for the production App ID: "no form(s) configured for the input
app ID". Check and publish the appropriate privacy message for this application in AdMob's Privacy & messaging section.
See https://developers.google.com/admob/unity/privacy.

The signed-in AdMob browser settings could not be inspected because the Computer Use connection was unavailable.
Successful test-ad delivery does not establish that the production account configuration or production ad delivery is
ready.