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
            AsyncOperationHandle initializationHandle = default;

            try
            {
                initializationHandle = Addressables.InitializeAsync(false);
                await initializationHandle.Task;

                if (initializationHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw initializationHandle.OperationException
                        ?? new InvalidOperationException("Addressables initialization failed.");
                }

                IsReady = true;
                _ = WarmUpPreloadAssetsAsync();
            }
            catch (OperationCanceledException)
            {
                IsReady = false;
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
                IsReady = false;
            }
            finally
            {
                if (initializationHandle.IsValid())
                {
                    Addressables.Release(initializationHandle);
                }

                _initializationComplete = true;
            }
        }

        private static async Awaitable WarmUpPreloadAssetsAsync()
        {
            AsyncOperationHandle downloadHandle = default;

            try
            {
                downloadHandle = Addressables.DownloadDependenciesAsync(PreloadLabel, false);
                await downloadHandle.Task;

                if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    LoggerIO.LogWarning(
                        $"Failed to warm up Addressables label " +
                        $"'{PreloadLabel}': " +
                        $"{downloadHandle.OperationException?.Message}");
                }
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
                if (downloadHandle.IsValid())
                {
                    Addressables.Release(downloadHandle);
                }
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
