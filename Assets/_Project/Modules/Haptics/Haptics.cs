using System;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace UnityTemplates.Haptics
{
	public enum HapticPreset
	{
		Selection = 0,
		Success = 1,
		Warning = 2,
		Failure = 3,
		LightImpact = 4,
		MediumImpact = 5,
		HeavyImpact = 6,
		RigidImpact = 7,
		SoftImpact = 8
	}

	public static class Haptics
	{
		private const int MinDurationMilliseconds = 1;
		private const int MaxDurationMilliseconds = 5000;
		private const int MaxAmplitude = 255;

		public static bool IsEnabled { get; set; } = true;
		public static event Action FallbackUsed;

		#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NH_PlayPreset(int type);

        [DllImport("__Internal")]
        private static extern int NH_Vibrate(float durationSeconds, float intensity);

        [DllImport("__Internal")]
        private static extern void NH_Stop();
		#endif

		public static void Play(HapticPreset preset)
		{
			if (!IsEnabled)
			{
				return;
			}

			if ((uint)preset > (uint)HapticPreset.SoftImpact)
			{
				throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
			}

			#if UNITY_ANDROID && !UNITY_EDITOR
            PlayAndroid(preset);
			#elif UNITY_IOS && !UNITY_EDITOR
            NH_PlayPreset((int)preset);
			#else
			UseFallback();
			#endif
		}

		public static void Vibrate(int durationMilliseconds, float intensity = 1f)
		{
			if (!IsEnabled)
			{
				return;
			}

			durationMilliseconds = Mathf.Clamp(durationMilliseconds, MinDurationMilliseconds, MaxDurationMilliseconds);
			intensity = Mathf.Clamp01(intensity);
			if (intensity <= 0f)
			{
				return;
			}

			#if UNITY_ANDROID && !UNITY_EDITOR
            VibrateAndroid(durationMilliseconds, intensity);
			#elif UNITY_IOS && !UNITY_EDITOR
            if (NH_Vibrate(durationMilliseconds / 1000f, intensity) == 0)
            {
                UseFallback();
            }
			#else
			UseFallback();
			#endif
		}

		public static void Stop()
		{
			#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                GetAndroidVibrator()?.Call("cancel");
            }
            catch (Exception)
            {
                ClearAndroidVibrator();
            }
			#elif UNITY_IOS && !UNITY_EDITOR
            NH_Stop();
			#endif
		}

		private static void UseFallback()
		{
			Handheld.Vibrate();
			FallbackUsed?.Invoke();
		}

		#if UNITY_ANDROID && !UNITY_EDITOR
        private const int AndroidSdkVibratorManager = 31;

        private static readonly long[] SuccessTimings = { 0, 20, 45, 45 };
        private static readonly int[] SuccessAmplitudes = { 0, 90, 0, 230 };
        private static readonly long[] WarningTimings = { 0, 45, 45, 30 };
        private static readonly int[] WarningAmplitudes = { 0, 255, 0, 160 };
        private static readonly long[] FailureTimings = { 0, 30, 35, 45, 35, 45, 35, 20 };
        private static readonly int[] FailureAmplitudes = { 0, 160, 0, 255, 0, 255, 0, 90 };
        private static AndroidJavaObject _vibrator;

        private static void PlayAndroid(HapticPreset preset)
        {
            switch (preset)
            {
                case HapticPreset.Selection: VibrateAndroid(15, 0.3f); break;
                case HapticPreset.Success: PlayAndroidWaveform(SuccessTimings, SuccessAmplitudes); break;
                case HapticPreset.Warning: PlayAndroidWaveform(WarningTimings, WarningAmplitudes); break;
                case HapticPreset.Failure: PlayAndroidWaveform(FailureTimings, FailureAmplitudes); break;
                case HapticPreset.LightImpact: VibrateAndroid(20, 0.35f); break;
                case HapticPreset.MediumImpact: VibrateAndroid(35, 0.65f); break;
                case HapticPreset.HeavyImpact: VibrateAndroid(50, 1f); break;
                case HapticPreset.RigidImpact: VibrateAndroid(16, 1f); break;
                case HapticPreset.SoftImpact: VibrateAndroid(45, 0.35f); break;
                default: throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }
        }

        private static void VibrateAndroid(int durationMilliseconds, float intensity)
        {
            PlayVibration(
                vibrationEffect =>
                    vibrationEffect.CallStatic<AndroidJavaObject>(
                        "createOneShot",
                        (long)durationMilliseconds,
                        Mathf.Clamp(Mathf.RoundToInt(intensity * MaxAmplitude), 1, MaxAmplitude)));
        }

        private static void PlayAndroidWaveform(long[] timings, int[] amplitudes)
        {
            PlayVibration(
                vibrationEffect =>
                    vibrationEffect.CallStatic<AndroidJavaObject>(
                        "createWaveform",
                        timings,
                        amplitudes,
                        -1));
        }

        private static void PlayVibration(Func<AndroidJavaClass, AndroidJavaObject> createEffect)
        {
            var fallbackRequired = false;

            try
            {
                var vibrator = GetAndroidVibrator();
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                {
                    fallbackRequired = true;
                }
                else
                {
                    using var vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = createEffect(vibrationEffect);
                    vibrator.Call("vibrate", effect);
                }
            }
            catch (Exception)
            {
                ClearAndroidVibrator();
                fallbackRequired = true;
            }

            if (fallbackRequired)
            {
                UseFallback();
            }
        }

        private static AndroidJavaObject GetAndroidVibrator()
        {
            if (_vibrator != null)
                return _vibrator;

            var activity = AndroidApplication.currentActivity;
            if (activity == null)
                return null;

            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            if (version.GetStatic<int>("SDK_INT") >= AndroidSdkVibratorManager)
            {
                using var manager = activity.Call<AndroidJavaObject>(
                    "getSystemService",
                    "vibrator_manager");
                _vibrator = manager?.Call<AndroidJavaObject>("getDefaultVibrator");
            }
            else
            {
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }

            return _vibrator;
        }

        private static void ClearAndroidVibrator()
        {
            _vibrator?.Dispose();
            _vibrator = null;
        }
		#endif
	}
}
