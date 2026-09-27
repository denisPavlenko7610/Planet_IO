@import UIKit;
@import CoreHaptics;


// ---------------------------------------------------------
// Cached UIKit generators
// ---------------------------------------------------------

static UISelectionFeedbackGenerator* s_selectionGenerator = nil;

static UINotificationFeedbackGenerator* s_notificationGenerator = nil;

static UIImpactFeedbackGenerator* s_lightGenerator = nil;

static UIImpactFeedbackGenerator* s_mediumGenerator = nil;

static UIImpactFeedbackGenerator* s_heavyGenerator = nil;

static UIImpactFeedbackGenerator* s_rigidGenerator = nil;

static UIImpactFeedbackGenerator* s_softGenerator = nil;

// ---------------------------------------------------------
// Core Haptics
// ---------------------------------------------------------

static CHHapticEngine* s_hapticEngine = nil;

static id<CHHapticPatternPlayer> s_hapticPlayer = nil;


// ---------------------------------------------------------
// Helpers
// ---------------------------------------------------------

static float Clamp01(float value)
{
    if (value < 0.0f)
        return 0.0f;

    if (value > 1.0f)
        return 1.0f;

    return value;
}

static void RunOnMainThread(dispatch_block_t block)
{
    if ([NSThread isMainThread])
    {
        block();
    }
    else
    {
        dispatch_async(dispatch_get_main_queue(), block);
    }
}


// ---------------------------------------------------------
// UIKit generators
// ---------------------------------------------------------

static UISelectionFeedbackGenerator*
GetSelectionGenerator()
{
    if (s_selectionGenerator == nil)
    {
        s_selectionGenerator = [[UISelectionFeedbackGenerator alloc] init];
    }

    return s_selectionGenerator;
}


static UINotificationFeedbackGenerator*
GetNotificationGenerator()
{
    if (s_notificationGenerator == nil)
    {
        s_notificationGenerator = [[UINotificationFeedbackGenerator alloc] init];
    }

    return s_notificationGenerator;
}

static UIImpactFeedbackGenerator* GetLightGenerator()
{
    if (s_lightGenerator == nil)
    {
        s_lightGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle: UIImpactFeedbackStyleLight];
    }

    return s_lightGenerator;
}

static UIImpactFeedbackGenerator* GetMediumGenerator()
{
    if (s_mediumGenerator == nil)
    {
        s_mediumGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle: UIImpactFeedbackStyleMedium];
    }

    return s_mediumGenerator;
}

static UIImpactFeedbackGenerator* GetHeavyGenerator()
{
    if (s_heavyGenerator == nil)
    {
        s_heavyGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle: UIImpactFeedbackStyleHeavy];
    }

    return s_heavyGenerator;
}

static UIImpactFeedbackGenerator* GetRigidGenerator()
{
    if (s_rigidGenerator == nil)
    {
        s_rigidGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle: UIImpactFeedbackStyleRigid];
    }

    return s_rigidGenerator;
}

static UIImpactFeedbackGenerator* GetSoftGenerator()
{
    if (s_softGenerator == nil)
    {
        s_softGenerator = [[UIImpactFeedbackGenerator alloc] initWithStyle: UIImpactFeedbackStyleSoft];
    }

    return s_softGenerator;
}

static void PlayImpact(UIImpactFeedbackGenerator* generator)
{
    if (generator == nil)
        return;

    [generator prepare];
    [generator impactOccurred];
}

// ---------------------------------------------------------
// Core Haptics support
// ---------------------------------------------------------

static BOOL SupportsCoreHaptics()
{
    id<CHHapticDeviceCapability> capabilities = [CHHapticEngine capabilitiesForHardware];

    return capabilities.supportsHaptics;
}

static CHHapticEngine* GetHapticEngine()
{
    if (!SupportsCoreHaptics())
        return nil;

    if (s_hapticEngine != nil)
        return s_hapticEngine;

    NSError* error = nil;

    s_hapticEngine = [[CHHapticEngine alloc] initAndReturnError:&error];

    if (error != nil || s_hapticEngine == nil)
    {
        NSLog(
            @"[NativeHaptics] Failed to create "
             "CHHapticEngine: %@",
            error);

        s_hapticEngine = nil;

        return nil;
    }

    s_hapticEngine.autoShutdownEnabled = YES;

    s_hapticEngine.resetHandler = ^
    {
        RunOnMainThread(^
        {
            s_hapticPlayer = nil;

            NSError* restartError = nil;

            [s_hapticEngine startAndReturnError:&restartError];

            if (restartError != nil)
            {
                NSLog(
                    @"[NativeHaptics] "
                     "Failed to restart engine: %@",
                    restartError);
            }
        });
    };

    s_hapticEngine.stoppedHandler = ^(CHHapticEngineStoppedReason reason)
    {
        (void)reason;

        RunOnMainThread(^
        {
            s_hapticPlayer = nil;
        });
    };

    return s_hapticEngine;
}

// ---------------------------------------------------------
// Custom continuous haptic
// ---------------------------------------------------------

static BOOL PlayContinuousHaptic(float duration, float intensity)
{
    CHHapticEngine* engine = GetHapticEngine();

    if (engine == nil)
        return NO;

    duration = MAX(0.01f, MIN(5.0f, duration));

    intensity = Clamp01(intensity);

    NSError* error = nil;

    BOOL started = [engine startAndReturnError:&error];

    if (!started || error != nil)
    {
        NSLog(
            @"[NativeHaptics] "
             "Failed to start engine: %@",
            error);

        s_hapticPlayer = nil;

        return NO;
    }

    // Stop previous custom haptic.
    if (s_hapticPlayer != nil)
    {
        NSError* stopError = nil;

        [s_hapticPlayer stopAtTime:0 error:&stopError];

        s_hapticPlayer = nil;
    }

    CHHapticEventParameter* intensityParameter = [[CHHapticEventParameter alloc]
                initWithParameterID: CHHapticEventParameterIDHapticIntensity
                value: intensity];

    CHHapticEventParameter* sharpnessParameter = [[CHHapticEventParameter alloc]
                initWithParameterID: CHHapticEventParameterIDHapticSharpness
                value: 0.5f];

    CHHapticEvent* event = [[CHHapticEvent alloc]
            initWithEventType: CHHapticEventTypeHapticContinuous
            parameters:@[
                intensityParameter,
                sharpnessParameter
            ]
            relativeTime:0 duration:duration];

    CHHapticPattern* pattern = [[CHHapticPattern alloc]
            initWithEvents:@[event]
            parameters:@[]
            error:&error];

    if (pattern == nil || error != nil)
    {
        NSLog(
            @"[NativeHaptics] "
             "Failed to create pattern: %@",
            error);

        return NO;
    }

    s_hapticPlayer = [engine createPlayerWithPattern: pattern error:&error];

    if (s_hapticPlayer == nil || error != nil)
    {
        NSLog(
            @"[NativeHaptics] "
             "Failed to create player: %@",
            error);

        return NO;
    }

    BOOL playerStarted = [s_hapticPlayer startAtTime:0 error:&error];

    if (!playerStarted || error != nil)
    {
        NSLog(
            @"[NativeHaptics] "
             "Failed to play haptic: %@",
            error);

        s_hapticPlayer = nil;

        return NO;
    }

    return YES;
}

// ---------------------------------------------------------
// Unity C API
// ---------------------------------------------------------

extern "C"
{

void NH_PlayPreset(int type)
{
    RunOnMainThread(^
    {
        switch (type)
        {
            // -----------------------------------------
            // Selection
            // -----------------------------------------

            case 0:
            {
                UISelectionFeedbackGenerator* generator = GetSelectionGenerator();

                [generator prepare];

                [generator selectionChanged];

                break;
            }

            // -----------------------------------------
            // Success
            // -----------------------------------------

            case 1:
            {
                UINotificationFeedbackGenerator* generator = GetNotificationGenerator();

                [generator prepare];

                [generator notificationOccurred: UINotificationFeedbackTypeSuccess];

                break;
            }

            // -----------------------------------------
            // Warning
            // -----------------------------------------

            case 2:
            {
                UINotificationFeedbackGenerator* generator = GetNotificationGenerator();

                [generator prepare];

                [generator notificationOccurred: UINotificationFeedbackTypeWarning];

                break;
            }

            // -----------------------------------------
            // Failure
            // -----------------------------------------

            case 3:
            {
                UINotificationFeedbackGenerator* generator = GetNotificationGenerator();

                [generator prepare];

                [generator notificationOccurred: UINotificationFeedbackTypeError];

                break;
            }

            // -----------------------------------------
            // Light
            // -----------------------------------------

            case 4:
            {
                PlayImpact(GetLightGenerator());

                break;
            }

            // -----------------------------------------
            // Medium
            // -----------------------------------------

            case 5:
            {
                PlayImpact(GetMediumGenerator());

                break;
            }

            // -----------------------------------------
            // Heavy
            // -----------------------------------------

            case 6:
            {
                PlayImpact(GetHeavyGenerator());

                break;
            }

            // -----------------------------------------
            // Rigid
            // -----------------------------------------

            case 7:
            {
                PlayImpact(GetRigidGenerator());

                break;
            }

            // -----------------------------------------
            // Soft
            // -----------------------------------------

            case 8:
            {
                PlayImpact(GetSoftGenerator());

                break;
            }
        }
    });
}

int NH_Vibrate(float duration, float intensity)
{
    __block BOOL played = NO;
    dispatch_block_t play = ^
    {
        played = PlayContinuousHaptic(
            duration,
            intensity);
    };

    if ([NSThread isMainThread])
    {
        play();
    }
    else
    {
        dispatch_sync(dispatch_get_main_queue(), play);
    }

    return played
        ? 1
        : 0;
}

void NH_Stop()
{
    RunOnMainThread(^
    {
        if (s_hapticPlayer == nil)
            return;

        NSError* error = nil;

        [s_hapticPlayer
            stopAtTime:0
            error:&error];

        s_hapticPlayer = nil;
    });
}
}
