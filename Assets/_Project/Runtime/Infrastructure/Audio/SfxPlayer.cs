using System;
using UnityEngine;
using UnityTemplates.Audio;
using UnityTemplates.Settings;
using Object = UnityEngine.Object;

namespace PlanetIO.Infrastructure.Audio
{
    public sealed class SfxPlayer : ISfxPlayer, IDisposable
    {
        private const int MaxVoiceCount = 24;
        private const int PrewarmVoiceCount = 6;

        private readonly SfxCatalog _catalog;
        private readonly ISettingsService _settings;
        private readonly GameObject _owner;
        private readonly AudioService _audio;

        public SfxPlayer(SfxCatalog catalog, ISettingsService settings)
        {
            _catalog = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _owner = new GameObject("Sfx");
            Object.DontDestroyOnLoad(_owner);
            _audio = new AudioService(_owner.transform, new AudioServiceOptions(MaxVoiceCount, PrewarmVoiceCount));
        }

        private float Volume => _settings.Get(GameSettingKeys.SfxVolume);

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
            if (cue == null || follow == null || Volume <= 0f)
            {
                return EmptyLoop.Instance;
            }

            return new LoopHandle(_audio.PlayFollowing(cue, follow, Volume));
        }

        public void Dispose()
        {
            _audio.Dispose();
            if (_owner != null)
            {
                Object.Destroy(_owner);
            }
        }

        private sealed class LoopHandle : IDisposable
        {
            private AudioVoiceHandle _handle;

            public LoopHandle(AudioVoiceHandle handle)
            {
                _handle = handle;
            }

            public void Dispose()
            {
                if (_handle.IsValid)
                {
                    _handle.Stop();
                }

                _handle = default;
            }
        }

        private sealed class EmptyLoop : IDisposable
        {
            public static readonly EmptyLoop Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
