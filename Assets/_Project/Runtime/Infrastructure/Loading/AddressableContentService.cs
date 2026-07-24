using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PlanetIO.Infrastructure.Loading
{
    public sealed class AddressableContentService : IContentInitializationService
    {
        private const string PreloadLabel = "preload";
        private bool _initializationStarted;
        private bool _initializationComplete;

        public bool IsReady { get; private set; }

        public Awaitable InitializeAsync()
        {
            if (!_initializationStarted)
            {
                _initializationStarted = true;
                _ = InitializeInternalAsync();
            }

            return WaitForInitializationAsync();
        }

        private async Awaitable InitializeInternalAsync()
        {
            try
            {
                AsyncOperationHandle initializationHandle = Addressables.InitializeAsync(false);
                await initializationHandle.Task;

                if (initializationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw initializationHandle.OperationException
                        ?? new InvalidOperationException("Addressables initialization failed.");
                }

                if (initializationHandle.IsValid())
                {
                    Addressables.Release(initializationHandle);
                }

                IsReady = true;

                _ = WarmUpPreloadAssetsAsync();
            }
            catch (OperationCanceledException)
            {
                LoggerIO.LogError("Application is closing");
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
                IsReady = false;
            }
            finally
            {
                _initializationComplete = true;
            }
        }

        private static async Awaitable WarmUpPreloadAssetsAsync()
        {
            try
            {
                AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(PreloadLabel, false);
                await downloadHandle.Task;

                if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    LoggerIO.LogWarning(
                        $"Failed to warm up Addressables label " +
                        $"'{PreloadLabel}': " +
                        $"{downloadHandle.OperationException?.Message}");
                }

                if (downloadHandle.IsValid())
                {
                    Addressables.Release(downloadHandle);
                }
            }
            catch (OperationCanceledException)
            {
                LoggerIO.LogError("Application is closing");
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
            }
        }

        private async Awaitable WaitForInitializationAsync()
        {
            while (!_initializationComplete)
            {
                await Awaitable.NextFrameAsync();
            }
        }
    }
}
