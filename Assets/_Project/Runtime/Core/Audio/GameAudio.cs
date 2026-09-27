using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    public static class GameAudio
    {
        private const string ClipsFolder = "SFX/";
        private const int SourcePoolSize = 8;
        private static readonly Dictionary<string, AudioClip> ClipCache = new();
        private static readonly List<AudioSource> Sources = new();
        private static GameObject _host;
        private static int _nextSource;

        public static float SfxVolume { get; set; } = 1f;

        public static AudioClip Load(string clipName)
        {
            if (ClipCache.TryGetValue(clipName, out AudioClip cached))
            {
                return cached;
            }

            AudioClip clip = Resources.Load<AudioClip>(ClipsFolder + clipName);
            if (clip == null)
            {
                GameLogger.Log($"Audio clip not found: {clipName}");
                return null;
            }

            ClipCache[clipName] = clip;
            return clip;
        }

        public static void Play2D(string clipName, float pitch = 1f, float volume = 1f)
        {
            Play2D(Load(clipName), pitch, volume);
        }

        public static void Play2D(AudioClip clip, float pitch = 1f, float volume = 1f)
        {
            float finalVolume = volume * Mathf.Clamp01(SfxVolume);
            if (clip == null || finalVolume <= 0f || !UnityEngine.Application.isPlaying)
            {
                return;
            }

            AudioSource source = NextSource();
            source.pitch = Mathf.Max(0.1f, pitch);
            source.PlayOneShot(clip, finalVolume);
        }

        private static AudioSource NextSource()
        {
            if (_host == null)
            {
                _host = new GameObject("GameAudio");
                Object.DontDestroyOnLoad(_host);
                Sources.Clear();
                for (int index = 0; index < SourcePoolSize; index++)
                {
                    AudioSource created = _host.AddComponent<AudioSource>();
                    created.playOnAwake = false;
                    Sources.Add(created);
                }
            }

            AudioSource source = Sources[_nextSource];
            _nextSource = (_nextSource + 1) % Sources.Count;
            return source;
        }
    }
}
