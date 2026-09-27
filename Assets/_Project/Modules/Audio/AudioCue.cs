using System;
using UnityEngine;
using UnityEngine.Audio;

namespace UnityTemplates.Audio
{
	[Serializable]
	public struct AudioCueClip
	{
		[SerializeField]
		private AudioClip _clip;

		[SerializeField, Min(0f)]
		private float _weight;

		public AudioCueClip(
			AudioClip clip,
			float weight = 1f
		)
		{
			if (!AudioNumber.IsFinite(weight) || weight < 0f)
			{
				throw new ArgumentOutOfRangeException(nameof(weight));
			}

			_clip = clip;
			_weight = weight;
		}

		public AudioClip Clip => _clip;

		public float Weight => _weight;

		internal bool IsPlayable => _clip != null && AudioNumber.IsFinite(_weight) && _weight > 0f;

		internal bool IsValid => AudioNumber.IsFinite(_weight) && _weight >= 0f && (_clip != null || _weight == 0f);
	}

	[CreateAssetMenu(
		fileName = "AudioCue",
		menuName = "Unity Templates/Audio/Audio Cue"
	)]
	public sealed class AudioCue : ScriptableObject
	{
		private const float MinimumPitch = 0.01f;

		private const float MaximumPitch = 3f;

		private const int MinPriority = 0;

		private const int MaxPriority = 255;

		private const int DefaultPriority = 128;

		[SerializeField]
		private AudioCueClip[] _clips = Array.Empty<AudioCueClip>();

		[Header("Routing")]
		[SerializeField]
		private AudioMixerGroup _output;

		[Tooltip("Allows UI and other deliberate cues to play while AudioListener.pause is enabled.")]
		[SerializeField]
		private bool _ignoreListenerPause;

		[Header("Playback")]
		[SerializeField]
		private bool _loop;

		[Tooltip("Unity convention: 0 is highest priority, 255 is lowest.")]
		[SerializeField, Range(MinPriority, MaxPriority)]
		private int _priority = DefaultPriority;

		[SerializeField]
		private Vector2 _volumeRange = Vector2.one;

		[SerializeField]
		private Vector2 _pitchRange = Vector2.one;

		[SerializeField, Range(0f, 1f)]
		private float _spatialBlend;

		[SerializeField, Min(0f)]
		private float _minDistance = 1f;

		[SerializeField, Min(0.01f)]
		private float _maxDistance = 500f;

		[SerializeField, Min(0f)]
		private float _fadeInSeconds;

		[SerializeField, Min(0f)]
		private float _fadeOutSeconds;

		[Header("Limits")]
		[Tooltip("Zero means unlimited within the service voice budget. The oldest instance is replaced when full.")]
		[SerializeField, Min(0)]
		private int _maxInstances;

		[Tooltip("Minimum unscaled time between successful starts. Zero disables cooldown.")]
		[SerializeField, Min(0f)]
		private float _cooldownSeconds;

		public AudioMixerGroup Output => _output;

		public bool IgnoreListenerPause => _ignoreListenerPause;

		public bool Loop => _loop;

		public int Priority => _priority;

		public Vector2 VolumeRange => _volumeRange;

		public Vector2 PitchRange => _pitchRange;

		public float SpatialBlend => _spatialBlend;

		public float MinDistance => _minDistance;

		public float MaxDistance => _maxDistance;

		public float FadeInSeconds => _fadeInSeconds;

		public float FadeOutSeconds => _fadeOutSeconds;

		public int MaxInstances => _maxInstances;

		public float CooldownSeconds => _cooldownSeconds;

		internal bool IsValidConfiguration
		{
			get
			{
				if (_clips == null
					|| _clips.Length == 0
					|| !IsRangeValid(_volumeRange, 0f, 1f)
					|| !IsRangeValid(_pitchRange, MinimumPitch, MaximumPitch)
					|| !AudioNumber.IsFinite(_spatialBlend)
					|| _spatialBlend < 0f
					|| _spatialBlend > 1f
					|| !AudioNumber.IsFinite(_minDistance)
					|| !AudioNumber.IsFinite(_maxDistance)
					|| _minDistance < 0f
					|| _maxDistance < _minDistance
					|| !AudioNumber.IsFiniteNonNegative(_fadeInSeconds)
					|| !AudioNumber.IsFiniteNonNegative(_fadeOutSeconds)
					|| !AudioNumber.IsFiniteNonNegative(_cooldownSeconds))
				{
					return false;
				}

				bool hasPlayableClip = false;

				for (int index = 0; index < _clips.Length; index++)
				{
					AudioCueClip entry = _clips[index];

					if (!entry.IsValid)
					{
						return false;
					}

					hasPlayableClip |= entry.IsPlayable;
				}

				return hasPlayableClip;
			}
		}

		internal bool TrySelectClip(float normalizedSample, out AudioClip clip)
		{
			clip = null;

			double totalWeight = 0d;

			for (int index = 0; index < _clips.Length; index++)
			{
				AudioCueClip entry = _clips[index];

				if (entry.IsPlayable)
				{
					totalWeight += entry.Weight;
				}
			}

			if (totalWeight <= 0d)
			{
				return false;
			}

			double target = Mathf.Clamp01(normalizedSample) * totalWeight;

			double accumulated = 0d;
			AudioClip lastPlayable = null;

			for (int index = 0; index < _clips.Length; index++)
			{
				AudioCueClip entry = _clips[index];

				if (!entry.IsPlayable)
				{
					continue;
				}

				lastPlayable = entry.Clip;
				accumulated += entry.Weight;

				if (target <= accumulated)
				{
					clip = entry.Clip;
					return true;
				}
			}

			clip = lastPlayable;
			return clip != null;
		}

		private void OnValidate()
		{
			_clips ??= Array.Empty<AudioCueClip>();

			for (int index = 0; index < _clips.Length; index++)
			{
				AudioCueClip entry = _clips[index];
				float weight = AudioNumber.IsFinite(entry.Weight)
					? Mathf.Max(0f, entry.Weight)
					: 0f;

				_clips[index] = new AudioCueClip(entry.Clip, weight);
			}

			_priority = Mathf.Clamp(_priority, MinPriority, MaxPriority);

			_volumeRange.x = Mathf.Clamp01(Sanitize(_volumeRange.x, 1f));
			_volumeRange.y = Mathf.Clamp01(Sanitize(_volumeRange.y, 1f));
			SortRange(ref _volumeRange);

			_pitchRange.x = Mathf.Clamp(
				Sanitize(_pitchRange.x, 1f),
				MinimumPitch,
				MaximumPitch
			);
			_pitchRange.y = Mathf.Clamp(
				Sanitize(_pitchRange.y, 1f),
				MinimumPitch,
				MaximumPitch
			);
			SortRange(ref _pitchRange);

			_spatialBlend = Mathf.Clamp01(Sanitize(_spatialBlend, 0f));
			_minDistance = Mathf.Max(0f, Sanitize(_minDistance, 1f));
			_maxDistance = Mathf.Max(
				_minDistance,
				Sanitize(_maxDistance, 500f)
			);
			_fadeInSeconds = Mathf.Max(0f, Sanitize(_fadeInSeconds, 0f));
			_fadeOutSeconds = Mathf.Max(0f, Sanitize(_fadeOutSeconds, 0f));
			_maxInstances = Mathf.Max(0, _maxInstances);
			_cooldownSeconds = Mathf.Max(0f, Sanitize(_cooldownSeconds, 0f));
		}

		private static bool IsRangeValid(Vector2 range, float minimum, float maximum)
		{
			return
				AudioNumber.IsFinite(range.x)
				&& AudioNumber.IsFinite(range.y)
				&& range.x >= minimum
				&& range.x <= maximum
				&& range.y >= minimum
				&& range.y <= maximum
				&& range.x <= range.y;
		}

		private static float Sanitize(float value, float fallback)
		{
			return AudioNumber.IsFinite(value) ? value : fallback;

		}

		private static void SortRange(ref Vector2 range)
		{
			if (range.y < range.x)
			{
				(range.x, range.y) = (range.y, range.x);
			}
		}
	}
}
