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
        private Awaitable _initialization;
        private bool _initializationStarted;

        public bool IsReady { get; private set; }

        public Awaitable InitializeAsync()
        {
            if (_initializationStarted)
            {
                return _initialization;
            }

            _initializationStarted = true;
            _initialization = InitializeInternalAsync();
            return _initialization;
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
                            $"Не удалось прогреть Addressables label " +
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
