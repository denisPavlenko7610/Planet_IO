using System;
using Planet_IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PlanetIO.Infrastructure.Loading
{
    public sealed class AddressableContentService :
        IContentInitializationService
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
            AsyncOperationHandle initializationHandle =
                Addressables.InitializeAsync(false);

            try
            {
                await WaitForCompletionAsync(initializationHandle);
                if (initializationHandle.Status !=
                    AsyncOperationStatus.Succeeded)
                {
                    throw initializationHandle.OperationException ??
                          new InvalidOperationException(
                              "Addressables initialization failed.");
                }

                AsyncOperationHandle downloadHandle =
                    Addressables.DownloadDependenciesAsync(
                        PreloadLabel,
                        false);
                try
                {
                    await WaitForCompletionAsync(downloadHandle);
                    if (downloadHandle.Status !=
                        AsyncOperationStatus.Succeeded)
                    {
                        Debug.LogWarning(
                            $"Failed to warm up Addressables label " +
                            $"'{PreloadLabel}': " +
                            $"{downloadHandle.OperationException?.Message}");
                    }
                }
                finally
                {
                    if (downloadHandle.IsValid())
                    {
                        Addressables.Release(downloadHandle);
                    }
                }

                IsReady = true;
            }
            catch (OperationCanceledException)
            {
                // Application is closing.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
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

        private async Awaitable WaitForInitializationAsync()
        {
            while (!_initializationComplete)
            {
                await Awaitable.NextFrameAsync(
                    Application.exitCancellationToken);
            }
        }

        private static async Awaitable WaitForCompletionAsync(
            AsyncOperationHandle handle)
        {
            while (!handle.IsDone)
            {
                await Awaitable.NextFrameAsync(
                    Application.exitCancellationToken);
            }
        }
    }
}
