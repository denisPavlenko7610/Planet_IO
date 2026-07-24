using System;
using PlanetIO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace PlanetIO.Infrastructure.Audio
{
    public sealed class AddressableMusicService :
        IStartable,
        IDisposable
    {
        private const string MenuMusicAddress = "audio/mars";
        private const string GameMusicAddress = "audio/map";
        private const float MusicVolume = 0.35f;
        private const float FadeDurationSeconds = 0.25f;

        private readonly IContentInitializationService
            _contentInitializationService;
        private AudioSource _audioSource;
        private AsyncOperationHandle<AudioClip> _clipHandle;
        private string _currentAddress = string.Empty;
        private string _requestedAddress = string.Empty;
        private int _playGeneration;

        public AddressableMusicService(
            IContentInitializationService contentInitializationService)
        {
            _contentInitializationService = contentInitializationService
                ?? throw new ArgumentNullException(
                    nameof(contentInitializationService));
        }

        public void Start()
        {
            GameObject audioObject = new("Addressable Music");
            Object.DontDestroyOnLoad(audioObject);
            _audioSource = audioObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 0f;

            SceneManager.sceneLoaded += OnSceneLoaded;
            PlayForScene(SceneManager.GetActiveScene());
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _playGeneration++;
            ReleaseClip();

            if (_audioSource != null)
            {
                Object.Destroy(_audioSource.gameObject);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayForScene(scene);
        }

        private void PlayForScene(Scene scene)
        {
            string address = scene.name switch
            {
                SceneNames.Menu => MenuMusicAddress,
                SceneNames.Game => GameMusicAddress,
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            if (string.Equals(
                    address,
                    _requestedAddress,
                    StringComparison.Ordinal))
            {
                if (_audioSource != null &&
                    _audioSource.clip != null &&
                    !_audioSource.isPlaying)
                {
                    _audioSource.UnPause();
                }

                return;
            }

            _requestedAddress = address;
            _ = LoadAndPlayAsync(address, ++_playGeneration);
        }

        private async Awaitable LoadAndPlayAsync(
            string address,
            int generation)
        {
            AsyncOperationHandle<AudioClip> handle = default;
            bool ownershipTransferred = false;

            try
            {
                await _contentInitializationService.InitializeAsync();
                if (generation != _playGeneration)
                {
                    return;
                }

                handle = Addressables.LoadAssetAsync<AudioClip>(address);

                while (!handle.IsDone)
                {
                    await Awaitable.NextFrameAsync();
                }

                if (generation != _playGeneration)
                {
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded ||
                    handle.Result == null)
                {
                    Debug.LogWarning(
                        $"Failed to load music '{address}': " +
                        $"{handle.OperationException?.Message}");
                    return;
                }

                await FadeToAsync(0f, generation);
                if (generation != _playGeneration)
                {
                    return;
                }

                ReleaseClip();
                _clipHandle = handle;
                ownershipTransferred = true;
                _currentAddress = address;
                _requestedAddress = address;
                _audioSource.clip = handle.Result;
                _audioSource.volume = 0f;
                _audioSource.Play();
                await FadeToAsync(MusicVolume, generation);
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
                if (!ownershipTransferred && handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                if (!ownershipTransferred &&
                    generation == _playGeneration)
                {
                    _requestedAddress = _currentAddress;
                }
            }
        }

        private async Awaitable FadeToAsync(
            float targetVolume,
            int generation)
        {
            if (_audioSource == null)
            {
                return;
            }

            float initialVolume = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < FadeDurationSeconds &&
                   generation == _playGeneration)
            {
                elapsed += Time.unscaledDeltaTime;
                _audioSource.volume = Mathf.Lerp(
                    initialVolume,
                    targetVolume,
                    Mathf.Clamp01(elapsed / FadeDurationSeconds));
                await Awaitable.NextFrameAsync();
            }

            if (generation == _playGeneration &&
                _audioSource != null)
            {
                _audioSource.volume = targetVolume;
            }
        }

        private void ReleaseClip()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.clip = null;
            }

            if (_clipHandle.IsValid())
            {
                Addressables.Release(_clipHandle);
                _clipHandle = default;
            }

            _currentAddress = string.Empty;
        }
    }
}
