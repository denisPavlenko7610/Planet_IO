using System;
using RD.Foundation;
using RD.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PlanetIO.Infrastructure.Loading
{
    public sealed class AddressableContentService : IContentInitializationService
    {
        private const string PreloadLabel = "preload";
        private readonly AsyncGate _gate = new();
        private readonly AddressableService _addressables;

        public AddressableContentService(AddressableService addressables)
        {
            _addressables = addressables ?? throw new ArgumentNullException(nameof(addressables));
        }

        public bool IsReady { get; private set; }

        public async Awaitable InitializeAsync()
        {
            using (await _gate.EnterAsync())
            {
                if (IsReady)
				{
					return;
				}
				var handle = Addressables.InitializeAsync(false);
                try
                {
                    await handle.ToAwaitable();
                    IsReady = true;
                }
                finally
                {
                    if (handle.IsValid())
					{
						Addressables.Release(handle);
					}
				}
                _ = WarmUpAsync();
            }
        }

        private async Awaitable WarmUpAsync()
        {
            try { await _addressables.DownloadDependenciesAsync(PreloadLabel); }
            catch (OperationCanceledException) { }
            catch (Exception exception) { UnityLogger.LogWarning($"Preload failed: {exception.Message}"); }
        }
    }
}
