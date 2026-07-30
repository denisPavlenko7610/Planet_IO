using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Mobile
{
    public sealed class MobileRuntimeService : IStartable, ITickable, IDisposable
    {
        private const int TargetFrameRate = 60;

        private readonly INetworkSessionService _networkSessionService;
        private bool _returnToMenuInProgress;
        private bool _memoryCleanupInProgress;

        public MobileRuntimeService(INetworkSessionService networkSessionService)
        {
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
            if (activeScene.name is SceneNames.Game or SceneNames.Loading)
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
                LoggerIO.LogException(exception);
            }
            finally
            {
                _returnToMenuInProgress = false;
            }
        }
    }
}
