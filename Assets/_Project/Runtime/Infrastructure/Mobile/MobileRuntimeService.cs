using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using UnityTemplates.Localization;

namespace PlanetIO.Infrastructure.Mobile
{
    public sealed class MobileRuntimeService : IStartable, ITickable, IDisposable
    {
        private const int TargetFrameRate = 60;
        private const float LeaveConfirmWindowSeconds = 2f;

        private readonly INetworkSessionService _networkSessionService;
        private readonly ILocalizationService _localization;
        private bool _returnToMenuInProgress;
        private bool _memoryCleanupInProgress;
        private float _leaveConfirmDeadline;

        public MobileRuntimeService(INetworkSessionService networkSessionService, ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
        }

        public void Start()
        {
            if (UnityEngine.Application.isMobilePlatform && QualitySettings.names.Length > 1)
            {
                QualitySettings.SetQualityLevel(1, true);
            }

            QualitySettings.vSyncCount = 0;
            UnityEngine.Application.targetFrameRate = TargetFrameRate;

            UnityEngine.Application.lowMemory += OnLowMemory;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySceneSettings(SceneManager.GetActiveScene());
        }

        public void Tick()
        {
            if (!UnityEngine.Application.isMobilePlatform || Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == SceneNames.Loading)
            {
                _ = ReturnToMenuAsync();
                return;
            }

            if (activeScene.name == SceneNames.Game)
            {
                if (Time.unscaledTime <= _leaveConfirmDeadline)
                {
                    _leaveConfirmDeadline = 0f;
                    _ = ReturnToMenuAsync();
                    return;
                }

                _leaveConfirmDeadline = Time.unscaledTime + LeaveConfirmWindowSeconds;
                ShowToast(_localization.Get(LocalizationKeys.HudBackToLeave));
                return;
            }

            if (activeScene.name == SceneNames.Menu)
            {
                UnityEngine.Application.Quit();
            }
        }

        public void Dispose()
        {
            UnityEngine.Application.lowMemory -= OnLowMemory;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }

        private static void ShowToast(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                using AndroidJavaClass toastClass = new("android.widget.Toast");
                using AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>(
                    "makeText", activity, message, toastClass.GetStatic<int>("LENGTH_SHORT"));
                toast.Call("show");
            }));
#else
            GameLogger.Log(message);
#endif
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            ApplySceneSettings(scene);
        }

        private static void ApplySceneSettings(Scene scene)
        {
            Screen.sleepTimeout = scene.name == SceneNames.Game
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;
        }

        private void OnLowMemory()
        {
            if (!_memoryCleanupInProgress)
            {
                _ = ReleaseUnusedResourcesAsync();
            }
        }

        private async Awaitable ReleaseUnusedResourcesAsync()
        {
            _memoryCleanupInProgress = true;
            try
            {
                AsyncOperation operation = Resources.UnloadUnusedAssets();
                while (!operation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _memoryCleanupInProgress = false;
            }
        }

        private async Awaitable ReturnToMenuAsync()
        {
            if (_returnToMenuInProgress)
            {
                return;
            }

            _returnToMenuInProgress = true;
            try
            {
                await _networkSessionService.ShutdownAndReturnToMenuAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                GameLogger.LogException(exception);
            }
            finally
            {
                _returnToMenuInProgress = false;
            }
        }
    }
}
