using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace PlanetIO.Infrastructure.Audio
{
    public sealed class AddressableMusicService : IStartable, IDisposable
    {
        private const string MenuMusicAddress = "audio/mars";
        private const string GameMusicAddress = "audio/map";
        private const float MusicVolume = 0.35f;
        private const float FadeDurationSeconds = 0.25f;

        private readonly IContentInitializationService _contentInitializationService;
        private AudioSource _audioSource;
        private AsyncOperationHandle<AudioClip> _clipHandle;
        private string _currentAddress = string.Empty;
        private string _requestedAddress = string.Empty;
        private int _playGeneration;
        private bool _disposed;

        public AddressableMusicService(IContentInitializationService contentInitializationService)
        {
            _contentInitializationService = contentInitializationService ?? throw new ArgumentNullException(nameof(contentInitializationService));
        }

        public void Start()
        {
            if (_disposed || _audioSource != null)
            {
                return;
            }

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
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _playGeneration++;
            ReleaseClip();

            if (_audioSource != null)
            {
                Object.Destroy(_audioSource.gameObject);
                _audioSource = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
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

            if (_disposed || string.IsNullOrEmpty(address))
            {
                return;
            }

            if (string.Equals(address, _requestedAddress, StringComparison.Ordinal))
            {
                ResumeIfNeeded();
                return;
            }

            if (string.Equals(address, _currentAddress, StringComparison.Ordinal) &&
                _audioSource != null &&
                _audioSource.clip != null)
            {
                int generation = ++_playGeneration;
                _requestedAddress = address;
                ResumeIfNeeded();
                _ = FadeToAsync(MusicVolume, generation);
                return;
            }

            _requestedAddress = address;
            _ = LoadAndPlayAsync(address, ++_playGeneration);
        }

        private async Awaitable LoadAndPlayAsync(string address, int generation)
        {
            AsyncOperationHandle<AudioClip> handle = default;
            bool ownershipTransferred = false;

            try
            {
                await _contentInitializationService.InitializeAsync();
                if (!_contentInitializationService.IsReady ||
                    !IsCurrentRequest(generation))
                {
                    return;
                }

                handle = Addressables.LoadAssetAsync<AudioClip>(address);

                while (!handle.IsDone)
                {
                    await Awaitable.NextFrameAsync();
                }

                if (!IsCurrentRequest(generation))
                {
                    return;
                }

                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    LoggerIO.LogWarning(
                        $"Failed to load music '{address}': " +
                        $"{handle.OperationException?.Message}");
                    return;
                }

                await FadeToAsync(0f, generation);
                if (!IsCurrentRequest(generation))
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
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
            }
            finally
            {
                if (!ownershipTransferred && handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                if (!ownershipTransferred && generation == _playGeneration)
                {
                    _requestedAddress = _currentAddress;
                }
            }
        }

        private async Awaitable FadeToAsync(float targetVolume, int generation)
        {
            if (!IsCurrentRequest(generation) || _audioSource == null)
            {
                return;
            }

            float initialVolume = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < FadeDurationSeconds && IsCurrentRequest(generation))
            {
                elapsed += Time.unscaledDeltaTime;
                _audioSource.volume = Mathf.Lerp(initialVolume, targetVolume, Mathf.Clamp01(elapsed / FadeDurationSeconds));
                await Awaitable.NextFrameAsync();
            }

            if (IsCurrentRequest(generation) && _audioSource != null)
            {
                _audioSource.volume = targetVolume;
            }
        }

        private bool IsCurrentRequest(int generation)
        {
            return !_disposed && generation == _playGeneration;
        }

        private void ResumeIfNeeded()
        {
            if (_audioSource != null &&
                _audioSource.clip != null &&
                !_audioSource.isPlaying)
            {
                _audioSource.UnPause();
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
