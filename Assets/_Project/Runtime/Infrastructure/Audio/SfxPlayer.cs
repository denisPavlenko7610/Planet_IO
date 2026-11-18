using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using RD.Audio;
using RD.Settings;
using Object = UnityEngine.Object;

namespace PlanetIO.Infrastructure.Audio
{
	public sealed class SfxPlayer : ISfxPlayer, IDisposable
	{
		private const int MaxVoiceCount = 24;
		private const int PrewarmVoiceCount = 6;
		private const float MaximumSfxVolume = 0.2f;

		private readonly SfxCatalog _catalog;
		private readonly ISettingsService _settings;
		private readonly GameObject _owner;
		private readonly AudioService _audio;
		private readonly List<LoopHandle> _loops = new();

		public SfxPlayer(SfxCatalog catalog, ISettingsService settings)
		{
			_catalog = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			_owner = new GameObject("Sfx");
			Object.DontDestroyOnLoad(_owner);
			_audio = new AudioService(_owner.transform, new AudioServiceOptions(MaxVoiceCount, PrewarmVoiceCount));
			_settings.Changed += OnSettingsChanged;
		}

		private float Volume => MaximumSfxVolume * _settings.Get(GameSettingKeys.SfxVolume);

		public void Play(SfxId id, float pitchMultiplier = 1f, float volumeMultiplier = 1f)
		{
			AudioCue cue = _catalog.Get(id);
			float volume = Volume * volumeMultiplier;
			if (cue == null || volume <= 0f)
			{
				return;
			}

			_audio.Play(cue, volume, pitchMultiplier);
		}

		public IDisposable PlayLoop(SfxId id, Transform follow)
		{
			AudioCue cue = _catalog.Get(id);
			if (cue == null || follow == null)
			{
				return EmptyLoop.Instance;
			}

			LoopHandle loop = new(_audio.PlayFollowing(cue, follow, MaximumSfxVolume), this);
			_loops.Add(loop);
			loop.SetGain(_settings.Get(GameSettingKeys.SfxVolume));
			return loop;
		}

		private void OnSettingsChanged(SettingChanged _)
		{
			float gain = _settings.Get(GameSettingKeys.SfxVolume);
			foreach (LoopHandle loop in _loops)
			{
				loop.SetGain(gain);
			}
		}

		public void Dispose()
		{
			_settings.Changed -= OnSettingsChanged;
			while (_loops.Count > 0)
			{
				_loops[_loops.Count - 1].Dispose();
			}

			_audio.Dispose();
			if (_owner != null)
			{
				Object.Destroy(_owner);
			}
		}

		private sealed class LoopHandle : IDisposable
		{
			private AudioVoiceHandle _handle;
			private readonly SfxPlayer _owner;

			public LoopHandle(AudioVoiceHandle handle, SfxPlayer owner)
			{
				_handle = handle;
				_owner = owner;
			}

			public void SetGain(float gain) => _handle.FadeTo(gain, 0f);

			public void Dispose()
			{
				if (_handle.IsValid)
				{
					_handle.Stop();
				}

				_handle = default;
				_owner._loops.Remove(this);
			}
		}

		[NoAutoStaticsCleanup]
		private sealed class EmptyLoop : IDisposable
		{
			public static readonly EmptyLoop Instance = new();

			public void Dispose() { }
		}
	}
}