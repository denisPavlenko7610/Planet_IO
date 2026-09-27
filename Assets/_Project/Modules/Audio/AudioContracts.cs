using System;

namespace UnityTemplates.Audio
{
	public enum AudioVoiceState
	{
		Invalid,
		Playing,
		Fading,
		Paused
	}

	public sealed class AudioServiceOptions
	{
		private const int DefaultMaxVoiceCount = 32;
		private const int DefaultPrewarmVoiceCount = 8;

		public AudioServiceOptions(int maxVoiceCount = DefaultMaxVoiceCount, int prewarmVoiceCount = DefaultPrewarmVoiceCount)
		{
			if (maxVoiceCount <= 0)
			{
				throw new ArgumentOutOfRangeException(
					nameof(maxVoiceCount),
					maxVoiceCount,
					"Maximum voices must be positive."
				);
			}

			if (prewarmVoiceCount < 0 || prewarmVoiceCount > maxVoiceCount)
			{
				throw new ArgumentOutOfRangeException(
					nameof(prewarmVoiceCount),
					prewarmVoiceCount,
					"Prewarm voice count must be between zero and MaxVoiceCount."
				);
			}

			MaxVoiceCount = maxVoiceCount;
			PrewarmVoiceCount = prewarmVoiceCount;
		}

		public int MaxVoiceCount { get; }

		public int PrewarmVoiceCount { get; }
	}

	public readonly struct AudioVoiceHandle : IEquatable<AudioVoiceHandle>
	{
		private readonly AudioVoice _voice;

		private readonly ulong _generation;

		internal AudioVoiceHandle(AudioService owner, AudioVoice voice, ulong generation)
		{
			Owner = owner;
			_voice = voice;
			_generation = generation;
		}

		public bool IsValid => Owner != null && Owner.IsHandleValid(this);

		public AudioVoiceState State => Owner?.GetVoiceState(this) ?? AudioVoiceState.Invalid;

		public bool IsPlaying => State is AudioVoiceState.Playing or AudioVoiceState.Fading;

		public void Stop()
		{
			RequireOwner();
			Owner.Stop(this, fadeOutSeconds: null);
		}

		public void Stop(float fadeOutSeconds)
		{
			RequireOwner();
			Owner.Stop(this, fadeOutSeconds);
		}

		public void Pause()
		{
			RequireOwner();
			Owner.Pause(this);
		}

		public void Resume()
		{
			RequireOwner();
			Owner.Resume(this);
		}

		public void FadeTo(
			float targetGain,
			float durationSeconds
		)
		{
			RequireOwner();
			Owner.FadeTo(this, targetGain, durationSeconds);
		}

		public bool Equals(AudioVoiceHandle other)
		{
			return
				ReferenceEquals(Owner, other.Owner)
				&& ReferenceEquals(_voice, other._voice)
				&& _generation == other._generation;
		}

		public override bool Equals(object obj)
		{
			return obj is AudioVoiceHandle other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = Owner != null
					? Owner.GetHashCode()
					: 0;

				hash = (hash * 397) ^ (_voice != null ? _voice.GetHashCode() : 0);

				return (hash * 397) ^ _generation.GetHashCode();
			}
		}

		public static bool operator ==(AudioVoiceHandle left, AudioVoiceHandle right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(AudioVoiceHandle left, AudioVoiceHandle right)
		{
			return !left.Equals(right);
		}

		internal AudioService Owner { get; }

		internal AudioVoice Voice => _voice;

		internal ulong Generation => _generation;

		private void RequireOwner()
		{
			if (Owner == null)
			{
				throw new InvalidOperationException("The default audio voice handle is invalid.");
			}
		}
	}

	internal static class AudioNumber
	{
		public static bool IsFinite(float value)
			=> !float.IsNaN(value) && !float.IsInfinity(value);

		public static bool IsFiniteNonNegative(float value)
			=> IsFinite(value) && value >= 0f;
	}
}
