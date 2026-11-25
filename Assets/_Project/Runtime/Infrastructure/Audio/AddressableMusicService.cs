using RD.Foundation;
using System;
using UnityEngine;
using RD.AddressableAssets;
using System.Threading;
using UnityEngine.SceneManagement;
using RD.Settings;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace PlanetIO.Infrastructure.Audio
{
	public sealed class AddressableMusicService : IStartable, IDisposable
	{
		private const string MenuMusicAddress = "audio/menu-theme";
		private const string GameMusicAddress = "audio/game-theme";
		private const float MaximumMusicVolume = 0.45f;
		private const float FadeDurationSeconds = 0.25f;
		private const float RetryDelaySeconds = 5f;
		private static readonly string[] MenuMusicAddresses =
			{ MenuMusicAddress, "audio/steller-dreams" };
		private static readonly string[] GameMusicAddresses =
			{ GameMusicAddress, "audio/exploration-theme" };

		private readonly IContentInitializationService _contentInitializationService;
		private readonly ISettingsService _settings;
		private AudioSource _audioSource;
		private AddressableAssetLease<AudioClip> _clipLease;
		private readonly AddressableService _addressables;
		private readonly CancellationTokenSource _lifetime = new();
		private string _currentAddress = string.Empty;
		private int _playGeneration;
		private int _nextMenuTrack;
		private int _nextGameTrack;
		private bool _disposed;
		private string _musicSceneName = string.Empty;

		public AddressableMusicService(
			IContentInitializationService contentInitializationService,
			ISettingsService settings,
			AddressableService addressables
		)
		{
			_contentInitializationService = contentInitializationService ??
				throw new ArgumentNullException(nameof(contentInitializationService));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			_addressables = addressables ?? throw new ArgumentNullException(nameof(addressables));
		}

		private float TargetVolume => MaximumMusicVolume * _settings.Get(GameSettingKeys.MusicVolume);

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
			_audioSource.loop = false;
			_audioSource.spatialBlend = 0f;
			_audioSource.volume = 0f;

			SceneManager.sceneLoaded += OnSceneLoaded;
			_settings.Changed += OnSettingsChanged;
			PlayForScene(SceneManager.GetActiveScene());
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_lifetime.Cancel();
			_lifetime.Dispose();
			SceneManager.sceneLoaded -= OnSceneLoaded;
			_settings.Changed -= OnSettingsChanged;
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
			if (_disposed)
			{
				return;
			}

			if (scene.name == _musicSceneName)
			{
				return;
			}

			string[] addresses = scene.name switch
			{
				SceneNames.Menu => MenuMusicAddresses,
				SceneNames.Game => GameMusicAddresses,
				_ => null
			};

			if (addresses == null)
			{
				return;
			}

			_musicSceneName = scene.name;
			_ = PlayPlaylistAsync(addresses, ++_playGeneration);
		}

		private async Awaitable PlayPlaylistAsync(string[] addresses, int generation)
		{
			bool menu = addresses == MenuMusicAddresses;
			int index = menu ? _nextMenuTrack : _nextGameTrack;
			int failures = 0;

			while (IsCurrentRequest(generation))
			{
				string address = addresses[index];
				index = (index + 1) % addresses.Length;

				if (menu)
				{
					_nextMenuTrack = index;
				}
				else
				{
					_nextGameTrack = index;
				}

				if ((address == _currentAddress && _audioSource.isPlaying) ||
					await LoadAndPlayAsync(address, generation))
				{
					failures = 0;
					while (IsCurrentRequest(generation) && (_audioSource.isPlaying || AudioListener.pause))
					{
						await Awaitable.NextFrameAsync();
					}

					continue;
				}

				failures++;
				if (IsCurrentRequest(generation) && failures == addresses.Length)
				{
					failures = 0;
					float retryAt = Time.realtimeSinceStartup + RetryDelaySeconds;
					while (IsCurrentRequest(generation) && Time.realtimeSinceStartup < retryAt)
					{
						await Awaitable.NextFrameAsync();
					}
				}
			}
		}

		private async Awaitable<bool> LoadAndPlayAsync(string address, int generation)
		{
			AddressableAssetLease<AudioClip> lease = null;
			bool ownershipTransferred = false;

			try
			{
				await _contentInitializationService.InitializeAsync();
				if (!_contentInitializationService.IsReady ||
					!IsCurrentRequest(generation))
				{
					return false;
				}

				lease = await _addressables.LoadAssetAsync<AudioClip>(address, _lifetime.Token);
				if (!IsCurrentRequest(generation) || lease.Asset == null)
				{
					return false;
				}

				if (_audioSource.isPlaying)
				{
					await FadeToAsync(0f, generation);
				}

				if (!IsCurrentRequest(generation))
				{
					return false;
				}

				ReleaseClip();
				_clipLease = lease;
				ownershipTransferred = true;
				_currentAddress = address;
				_audioSource.clip = lease.Asset;
				_audioSource.volume = 0f;
				_audioSource.Play();
				await FadeToAsync(TargetVolume, generation);
				return IsCurrentRequest(generation);
			}
			catch (OperationCanceledException) { }
			catch (Exception exception)
			{
				UnityLogger.LogException(exception);
			}
			finally
			{
				if (!ownershipTransferred)
				{
					lease?.Dispose();
				}
			}

			return false;
		}

		private void OnSettingsChanged(SettingChanged _)
		{
			if (_audioSource != null && _audioSource.isPlaying)
			{
				_audioSource.volume = TargetVolume;
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
				_audioSource.volume = Mathf.Lerp(
					initialVolume,
					targetVolume,
					Mathf.Clamp01(elapsed / FadeDurationSeconds)
				);
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

		private void ReleaseClip()
		{
			if (_audioSource != null)
			{
				_audioSource.Stop();
				_audioSource.clip = null;
			}

			_clipLease?.Dispose();
			_clipLease = null;

			_currentAddress = string.Empty;
		}
	}
}