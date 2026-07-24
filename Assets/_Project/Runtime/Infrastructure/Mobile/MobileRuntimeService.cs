using System;
using PlanetIO;
using PlanetIO.UI.Mobile;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace PlanetIO.Infrastructure.Mobile
{
    public sealed class MobileRuntimeService :
        IStartable,
        ITickable,
        IDisposable
    {
        private const int TargetFrameRate = 60;

        private readonly INetworkSessionService _networkSessionService;
        private bool _returnToMenuInProgress;
        private bool _memoryCleanupInProgress;

        public MobileRuntimeService(
            INetworkSessionService networkSessionService)
        {
            _networkSessionService = networkSessionService
                ?? throw new ArgumentNullException(
                    nameof(networkSessionService));
        }

        public void Start()
        {
            if (UnityEngine.Application.isMobilePlatform &&
                QualitySettings.names.Length > 1)
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
            if (!UnityEngine.Application.isMobilePlatform ||
                Keyboard.current?.escapeKey.wasPressedThisFrame != true)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == SceneNames.Game ||
                activeScene.name == SceneNames.Loading)
            {
                _ = ReturnToMenuAsync();
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneSettings(scene);
        }

        private static void ApplySceneSettings(Scene scene)
        {
            Screen.sleepTimeout = scene.name == SceneNames.Game
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;

            Canvas.ForceUpdateCanvases();
            RectTransform[] rectTransforms =
                Object.FindObjectsByType<RectTransform>(
                    FindObjectsInactive.Include);

            foreach (RectTransform rectTransform in rectTransforms)
            {
                if (rectTransform.name is "Score" or
                    "Joystick " or
                    "Joystick" or
                    "AccelerationButton")
                {
                    SafeAreaElement.AttachTo(rectTransform);
                }
            }
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
                // Application is closing.
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
                await _networkSessionService
                    .ShutdownAndReturnToMenuAsync();
            }
            catch (OperationCanceledException)
            {
                // Application is closing.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _returnToMenuInProgress = false;
            }
        }
    }
}
