using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace UnityTemplates.Audio
{
	internal sealed class AudioVoice
	{
		public ulong Generation;

		public AudioSource Source;

		public AudioCue Cue;

		public int Priority;

		public double StartedAt;

		public Transform FollowTarget;

		public float BaseVolume;

		public float FadeGain = 1f;

		public float FadeFrom;

		public float FadeTo;

		public float FadeDuration;

		public double FadeElapsed;

		public double FadeLastUpdateAt;

		public bool FadeActive;

		public bool StopAfterFade;

		public float DefaultFadeOut;

		public bool Paused;

		public AudioVoiceState State;
	}

	[DisallowMultipleComponent]
	internal sealed class AudioServiceDriver : MonoBehaviour
	{
		internal AudioService Service;

		private void Update()
		{
			Service?.TickFromDriver();
		}

		private void OnDestroy()
		{
			AudioService service = Service;
			Service = null;
			service?.NotifyDriverDestroyed();
		}
	}

	public sealed class AudioService : IDisposable
	{
		private const float MinimumPitch = 0.01f;

		private const float MaximumPitch = 3f;

		private readonly AudioServiceOptions _options;

		private readonly Dictionary<AudioCue, double> _lastCueStarts = new();

		private readonly List<AudioVoice> _activeVoices = new();

		private readonly GameObject _root;

		private readonly AudioServiceDriver _driver;

		private readonly ObjectPool<AudioVoice> _voicePool;

		private bool _disposed;

		private bool _paused;

		public AudioService(Transform lifetimeOwner, AudioServiceOptions options = null)
		{
			if (lifetimeOwner == null)
			{
				throw new ArgumentNullException(nameof(lifetimeOwner));
			}

			_options = options ?? new AudioServiceOptions();
			_root = new GameObject("[UnityTemplates.Audio]");
			_root.transform.SetParent(lifetimeOwner, false);

			try
			{
				_driver = _root.AddComponent<AudioServiceDriver>();
				_driver.Service = this;

				_voicePool = new ObjectPool<AudioVoice>(
					CreateVoice,
					OnVoiceAcquired,
					OnVoiceReleased,
					actionOnDestroy: null,
					collectionCheck: true,
					defaultCapacity: Math.Max(1, _options.PrewarmVoiceCount),
					maxSize: _options.MaxVoiceCount
				);

				PrewarmVoices();
			}
			catch
			{
				if (_driver != null)
				{
					_driver.Service = null;
				}

				_voicePool?.Dispose();
				DestroyUnityObject(_root);
				throw;
			}
		}

		public int ActiveVoiceCount => _activeVoices.Count;

		public int MaxVoiceCount => _options.MaxVoiceCount;

		public bool IsPaused => _paused;

		public AudioVoiceHandle Play(AudioCue cue, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
		{
			return PlayInternal(
				cue,
				fixedPosition: null,
				followTarget: null,
				volumeMultiplier,
				pitchMultiplier
			);
		}

		public AudioVoiceHandle PlayAt(
			AudioCue cue,
			Vector3 worldPosition,
			float volumeMultiplier = 1f,
			float pitchMultiplier = 1f
		)
		{
			return PlayInternal(
				cue,
				worldPosition,
				followTarget: null,
				volumeMultiplier,
				pitchMultiplier
			);
		}

		public AudioVoiceHandle PlayFollowing(
			AudioCue cue,
			Transform followTarget,
			float volumeMultiplier = 1f,
			float pitchMultiplier = 1f
		)
		{
			if (followTarget == null)
			{
				throw new ArgumentNullException(nameof(followTarget));
			}

			return PlayInternal(
				cue,
				fixedPosition: null,
				followTarget,
				volumeMultiplier,
				pitchMultiplier
			);
		}

		public void SetPaused(bool paused)
		{
			ThrowIfDisposed();

			if (_paused == paused)
			{
				return;
			}

			_paused = paused;
			double now = Time.unscaledTimeAsDouble;

			for (int index = 0; index < _activeVoices.Count; index++)
			{
				AudioVoice voice = _activeVoices[index];

				if (voice.FadeActive)
				{
					voice.FadeLastUpdateAt = now;
				}

				if (voice.Paused || voice.Source == null)
				{
					continue;
				}

				if (paused)
				{
					voice.Source.Pause();
					voice.State = AudioVoiceState.Paused;
				}
				else
				{
					voice.Source.UnPause();
					voice.State = voice.FadeActive
						? AudioVoiceState.Fading
						: AudioVoiceState.Playing;
				}
			}
		}

		public int StopAll()
		{
			ThrowIfDisposed();

			int stopped = _activeVoices.Count;

			for (int index = _activeVoices.Count - 1; index >= 0; index--)
			{
				ReleaseVoice(_activeVoices[index]);
			}

			return stopped;
		}

		public void Dispose()
		{
			DisposeCore(destroyRoot: true);
		}

		internal bool IsHandleValid(AudioVoiceHandle handle)
		{
			return
				!_disposed
				&& ReferenceEquals(handle.Owner, this)
				&& IsVoiceActive(
					handle.Voice,
					handle.Generation
				);
		}

		internal AudioVoiceState GetVoiceState(AudioVoiceHandle handle)
		{
			if (!IsHandleValid(handle))
			{
				return AudioVoiceState.Invalid;
			}

			AudioVoice voice = handle.Voice;

			if (IsBlocked(voice))
			{
				return AudioVoiceState.Paused;
			}

			return voice.State;
		}

		internal void Stop(AudioVoiceHandle handle, float? fadeOutSeconds)
		{
			if (fadeOutSeconds.HasValue && (!AudioNumber.IsFinite(fadeOutSeconds.Value) || fadeOutSeconds.Value < 0f))
			{
				throw new ArgumentOutOfRangeException(nameof(fadeOutSeconds));
			}

			AudioVoice voice = GetRequiredVoice(handle);

			if (ReleaseIfNaturallyCompleted(voice))
			{
				return;
			}

			float duration = fadeOutSeconds ?? voice.DefaultFadeOut;

			if (duration <= 0f)
			{
				ReleaseVoice(voice);
				return;
			}

			BeginFade(
				voice,
				targetGain: 0f,
				duration,
				stopAfterFade: true,
				Time.unscaledTimeAsDouble
			);
		}

		internal void Pause(AudioVoiceHandle handle)
		{
			AudioVoice voice = GetRequiredVoice(handle);

			if (ReleaseIfNaturallyCompleted(voice))
			{
				return;
			}

			if (voice.Paused)
			{
				return;
			}

			voice.Paused = true;

			if (voice.FadeActive)
			{
				voice.FadeLastUpdateAt = Time.unscaledTimeAsDouble;
			}

			voice.Source?.Pause();
			voice.State = AudioVoiceState.Paused;
		}

		internal void Resume(AudioVoiceHandle handle)
		{
			AudioVoice voice = GetRequiredVoice(handle);

			if (!voice.Paused)
			{
				return;
			}

			voice.Paused = false;

			if (voice.FadeActive)
			{
				voice.FadeLastUpdateAt = Time.unscaledTimeAsDouble;
			}

			if (!_paused)
			{
				voice.Source?.UnPause();
			}

			voice.State = IsBlocked(voice)
				? AudioVoiceState.Paused
				: voice.FadeActive
					? AudioVoiceState.Fading
					: AudioVoiceState.Playing;
		}

		internal void FadeTo(AudioVoiceHandle handle, float targetGain, float durationSeconds)
		{
			ValidateFade(targetGain, durationSeconds);

			AudioVoice voice = GetRequiredVoice(handle);

			if (ReleaseIfNaturallyCompleted(voice))
			{
				return;
			}

			if (durationSeconds <= 0f)
			{
				voice.FadeActive = false;
				voice.StopAfterFade = false;
				voice.FadeGain = targetGain;
				ApplyVolume(voice);
				voice.State = IsBlocked(voice)
					? AudioVoiceState.Paused
					: AudioVoiceState.Playing;

				return;
			}

			BeginFade(
				voice,
				targetGain,
				durationSeconds,
				stopAfterFade: false,
				Time.unscaledTimeAsDouble
			);
		}

		internal void TickFromDriver()
		{
			if (!_disposed)
			{
				Tick();
			}
		}

		internal void NotifyDriverDestroyed()
		{
			if (!_disposed)
			{
				DisposeCore(destroyRoot: false);
			}
		}

		private AudioVoiceHandle PlayInternal(
			AudioCue cue,
			Vector3? fixedPosition,
			Transform followTarget,
			float volumeMultiplier,
			float pitchMultiplier
		)
		{
			ThrowIfDisposed();

			if (cue == null)
			{
				throw new ArgumentNullException(nameof(cue));
			}

			if (!cue.IsValidConfiguration)
			{
				throw new InvalidOperationException($"Audio cue '{cue.name}' has an invalid configuration.");
			}

			ValidateMultiplier(volumeMultiplier, nameof(volumeMultiplier), allowZero: true);
			ValidateMultiplier(pitchMultiplier, nameof(pitchMultiplier), allowZero: false);

			CleanupCompletedVoices();

			double now = Time.unscaledTimeAsDouble;

			if (cue.CooldownSeconds > 0f
				&& _lastCueStarts.TryGetValue(cue, out double lastStart)
				&& now - lastStart < cue.CooldownSeconds)
			{
				return default;
			}

			if (!cue.TrySelectClip(UnityEngine.Random.value, out AudioClip clip) || clip == null)
			{
				throw new InvalidOperationException($"Audio cue '{cue.name}' has no playable clip.");
			}

			EnforceCueLimit(cue);

			if (!MakeRoomFor(cue.Priority))
			{
				return default;
			}

			AudioVoice voice = _voicePool.Get();
			voice.Generation = NextGeneration(voice.Generation);
			voice.Cue = cue;
			voice.Priority = cue.Priority;
			voice.StartedAt = now;
			voice.FollowTarget = followTarget;
			voice.DefaultFadeOut = cue.FadeOutSeconds;
			_activeVoices.Add(voice);

			try
			{
				PrepareAndStartVoice(
					voice,
					cue,
					clip,
					fixedPosition,
					volumeMultiplier,
					pitchMultiplier,
					now
				);
			}
			catch
			{
				ReleaseVoice(voice);
				throw;
			}

			_lastCueStarts[cue] = now;

			return new AudioVoiceHandle(this, voice, voice.Generation);
		}

		private void PrepareAndStartVoice(
			AudioVoice voice,
			AudioCue cue,
			AudioClip clip,
			Vector3? fixedPosition,
			float volumeMultiplier,
			float pitchMultiplier,
			double now
		)
		{
			AudioSource source = voice.Source;

			if (source == null)
			{
				throw new MissingReferenceException("An owned AudioSource was destroyed.");
			}

			voice.BaseVolume = Mathf.Clamp01(SampleRange(cue.VolumeRange) * volumeMultiplier);
			voice.FadeGain = 1f;
			voice.FadeActive = false;
			voice.StopAfterFade = false;
			voice.Paused = false;

			source.clip = clip;
			source.outputAudioMixerGroup = cue.Output;
			source.loop = cue.Loop;
			source.pitch = Mathf.Clamp(
				SampleRange(cue.PitchRange) * pitchMultiplier,
				MinimumPitch,
				MaximumPitch
			);
			source.priority = cue.Priority;
			source.spatialBlend = cue.SpatialBlend;
			source.minDistance = cue.MinDistance;
			source.maxDistance = cue.MaxDistance;
			source.ignoreListenerPause = cue.IgnoreListenerPause;

			PositionVoice(voice, fixedPosition);

			if (cue.FadeInSeconds > 0f)
			{
				voice.FadeGain = 0f;
				BeginFade(
					voice,
					targetGain: 1f,
					cue.FadeInSeconds,
					stopAfterFade: false,
					now
				);
			}
			else
			{
				voice.State = IsBlocked(voice)
					? AudioVoiceState.Paused
					: AudioVoiceState.Playing;
			}

			ApplyVolume(voice);
			source.Play();

			if (_paused)
			{
				source.Pause();
				voice.State = AudioVoiceState.Paused;
			}
		}

		private void EnforceCueLimit(AudioCue cue)
		{
			if (cue.MaxInstances <= 0)
			{
				return;
			}

			while (CountActive(cue) >= cue.MaxInstances)
			{
				AudioVoice oldest = FindOldest(cue);

				if (oldest == null)
				{
					return;
				}

				ReleaseVoice(oldest);
			}
		}

		private bool MakeRoomFor(int incomingPriority)
		{
			if (_activeVoices.Count < _options.MaxVoiceCount)
			{
				return true;
			}

			AudioVoice victim = FindLeastImportantVoice();

			if (victim == null || victim.Priority < incomingPriority)
			{
				return false;
			}

			ReleaseVoice(victim);
			return true;
		}

		private int CountActive(AudioCue cue)
		{
			int count = 0;

			for (int index = 0; index < _activeVoices.Count; index++)
			{
				if (_activeVoices[index].Cue == cue)
				{
					count++;
				}
			}

			return count;
		}

		private AudioVoice FindOldest(AudioCue cue)
		{
			AudioVoice oldest = null;

			for (int index = 0; index < _activeVoices.Count; index++)
			{
				AudioVoice candidate = _activeVoices[index];

				if (candidate.Cue == cue && (oldest == null || candidate.StartedAt < oldest.StartedAt))
				{
					oldest = candidate;
				}
			}

			return oldest;
		}

		private AudioVoice FindLeastImportantVoice()
		{
			AudioVoice result = null;

			for (int index = 0; index < _activeVoices.Count; index++)
			{
				AudioVoice candidate = _activeVoices[index];

				if (result == null
					|| candidate.Priority > result.Priority
					|| candidate.Priority == result.Priority && candidate.StartedAt < result.StartedAt)
				{
					result = candidate;
				}
			}

			return result;
		}

		private void Tick()
		{
			double now = Time.unscaledTimeAsDouble;

			for (int index = _activeVoices.Count - 1; index >= 0; index--)
			{
				AudioVoice voice = _activeVoices[index];

				if (voice.FollowTarget != null && voice.Source != null)
				{
					voice.Source.transform.position = voice.FollowTarget.position;
				}

				bool playbackBlocked = IsBlocked(voice);

				if (voice.FadeActive)
				{
					UpdateFade(voice, now, canProgress: !playbackBlocked);

					if (!IsVoiceActive(voice, voice.Generation))
					{
						continue;
					}
				}

				if (!playbackBlocked && (voice.Source == null || !voice.Source.isPlaying))
				{
					ReleaseVoice(voice);
				}
			}
		}

		private void BeginFade(
			AudioVoice voice,
			float targetGain,
			float durationSeconds,
			bool stopAfterFade,
			double now
		)
		{
			voice.FadeFrom = voice.FadeGain;
			voice.FadeTo = targetGain;
			voice.FadeDuration = durationSeconds;
			voice.FadeElapsed = 0d;
			voice.FadeLastUpdateAt = now;
			voice.FadeActive = true;
			voice.StopAfterFade = stopAfterFade;
			voice.State = IsBlocked(voice)
				? AudioVoiceState.Paused
				: AudioVoiceState.Fading;
		}

		private void UpdateFade(AudioVoice voice, double now, bool canProgress)
		{
			double delta = Math.Max(0d, now - voice.FadeLastUpdateAt);

			voice.FadeLastUpdateAt = now;

			if (!canProgress)
			{
				return;
			}

			voice.FadeElapsed += delta;

			float progress = voice.FadeDuration <= 0f
				? 1f
				: Mathf.Clamp01((float)(voice.FadeElapsed / voice.FadeDuration));

			voice.FadeGain = Mathf.Lerp(voice.FadeFrom, voice.FadeTo, progress);
			ApplyVolume(voice);

			if (progress < 1f)
			{
				voice.State = AudioVoiceState.Fading;
				return;
			}

			voice.FadeActive = false;

			if (voice.StopAfterFade)
			{
				ReleaseVoice(voice);
				return;
			}

			voice.StopAfterFade = false;
			voice.State = AudioVoiceState.Playing;
		}

		private void CleanupCompletedVoices()
		{
			for (int index = _activeVoices.Count - 1; index >= 0; index--)
			{
				AudioVoice voice = _activeVoices[index];

				if (!voice.Paused
					&& !_paused
					&& !IsListenerPaused(voice)
					&& (voice.Source == null || !voice.Source.isPlaying))
				{
					ReleaseVoice(voice);
				}
			}
		}

		private bool ReleaseIfNaturallyCompleted(AudioVoice voice)
		{
			if (voice.Paused || _paused || IsListenerPaused(voice) || voice.Source != null && voice.Source.isPlaying)
			{
				return false;
			}

			ReleaseVoice(voice);
			return true;
		}

		private AudioVoice GetRequiredVoice(AudioVoiceHandle handle)
		{
			ThrowIfDisposed();

			if (!ReferenceEquals(handle.Owner, this))
			{
				throw new ArgumentException(
					"The voice handle belongs to another AudioService.",
					nameof(handle)
				);
			}

			if (!IsVoiceActive(handle.Voice, handle.Generation))
			{
				throw new InvalidOperationException("The audio voice handle is stale or no longer active.");
			}

			return handle.Voice;
		}

		private bool IsVoiceActive(AudioVoice voice, ulong generation)
		{
			return voice != null && voice.Generation == generation && _activeVoices.Contains(voice);
		}

		private AudioVoice CreateVoice()
		{
			return new AudioVoice
			{
				Source = CreateSource(),
				State = AudioVoiceState.Invalid
			};
		}

		private AudioSource CreateSource()
		{
			GameObject sourceObject = new GameObject("Audio Voice");

			try
			{
				sourceObject.transform.SetParent(_root.transform, false);

				AudioSource source = sourceObject.AddComponent<AudioSource>();
				source.playOnAwake = false;
				sourceObject.SetActive(false);
				return source;
			}
			catch
			{
				DestroyUnityObject(sourceObject);
				throw;
			}
		}

		private void OnVoiceAcquired(AudioVoice voice)
		{
			if (voice.Source == null)
			{
				voice.Source = CreateSource();
			}

			voice.Source.gameObject.SetActive(true);
		}

		private void OnVoiceReleased(AudioVoice voice)
		{
			voice.Cue = null;
			voice.FollowTarget = null;
			voice.FadeActive = false;
			voice.StopAfterFade = false;
			voice.FadeElapsed = 0d;
			voice.BaseVolume = 0f;
			voice.FadeGain = 1f;
			voice.DefaultFadeOut = 0f;
			voice.Paused = false;
			voice.State = AudioVoiceState.Invalid;

			if (voice.Source == null)
			{
				return;
			}

			voice.Source.Stop();
			voice.Source.clip = null;
			voice.Source.outputAudioMixerGroup = null;
			voice.Source.loop = false;
			voice.Source.ignoreListenerPause = false;
			voice.Source.transform.SetParent(_root.transform, false);
			voice.Source.transform.localPosition = Vector3.zero;
			voice.Source.gameObject.SetActive(false);
		}

		private void ReleaseVoice(AudioVoice voice)
		{
			if (voice == null || !_activeVoices.Remove(voice))
			{
				return;
			}

			_voicePool.Release(voice);
		}

		private void PrewarmVoices()
		{
			if (_options.PrewarmVoiceCount <= 0)
			{
				return;
			}

			AudioVoice[] voices = new AudioVoice[_options.PrewarmVoiceCount];

			for (int index = 0; index < voices.Length; index++)
			{
				voices[index] = _voicePool.Get();
			}

			for (int index = 0; index < voices.Length; index++)
			{
				_voicePool.Release(voices[index]);
			}
		}

		private void PositionVoice(AudioVoice voice, Vector3? fixedPosition)
		{
			if (voice.FollowTarget != null)
			{
				voice.Source.transform.position = voice.FollowTarget.position;
			}
			else if (fixedPosition.HasValue)
			{
				voice.Source.transform.position = fixedPosition.Value;
			}
			else
			{
				voice.Source.transform.position = _root.transform.position;
			}
		}

		private static void ApplyVolume(AudioVoice voice)
		{
			if (voice.Source != null)
			{
				voice.Source.volume = Mathf.Clamp01(voice.BaseVolume * voice.FadeGain);
			}
		}

		private static bool IsListenerPaused(AudioVoice voice)
		{
			return AudioListener.pause && voice.Source != null && !voice.Source.ignoreListenerPause;
		}

		private bool IsBlocked(AudioVoice voice)
		{
			return voice.Paused || _paused || IsListenerPaused(voice);
		}

		private static float SampleRange(Vector2 range)
		{
			return Mathf.Lerp(range.x, range.y, UnityEngine.Random.value);
		}

		private void DisposeCore(bool destroyRoot)
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			if (_driver != null)
			{
				_driver.Service = null;
			}

			for (int index = 0; index < _activeVoices.Count; index++)
			{
				AudioVoice voice = _activeVoices[index];

				if (voice.Source != null)
				{
					voice.Source.Stop();
					voice.Source.clip = null;
				}

				voice.State = AudioVoiceState.Invalid;
			}

			_activeVoices.Clear();
			_lastCueStarts.Clear();
			_voicePool.Dispose();

			if (destroyRoot && _root != null)
			{
				DestroyUnityObject(_root);
			}
		}

		private static void ValidateFade(float targetGain, float durationSeconds)
		{
			if (!AudioNumber.IsFinite(targetGain) || targetGain < 0f || targetGain > 1f)
			{
				throw new ArgumentOutOfRangeException(nameof(targetGain));
			}

			if (!AudioNumber.IsFinite(durationSeconds) || durationSeconds < 0f)
			{
				throw new ArgumentOutOfRangeException(nameof(durationSeconds));
			}
		}

		private static void ValidateMultiplier(float value, string parameterName, bool allowZero)
		{
			if (!AudioNumber.IsFinite(value) || value < 0f || !allowZero && Mathf.Approximately(value, 0f))
			{
				throw new ArgumentOutOfRangeException(parameterName);
			}
		}

		private static ulong NextGeneration(ulong generation)
		{
			return generation == ulong.MaxValue
				? 1UL
				: generation + 1UL;
		}

		private void ThrowIfDisposed()
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(AudioService));
			}
		}

		private static void DestroyUnityObject(Object value)
		{
			if (value == null)
			{
				return;
			}

			if (Application.isPlaying)
			{
				Object.Destroy(value);
			}
			else
			{
				Object.DestroyImmediate(value);
			}
		}
	}
}
