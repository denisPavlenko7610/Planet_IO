using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    /// <summary>
    /// Fire-and-forget 2D sound playback with Resources-backed clip caching.
    /// </summary>
    public static class GameAudio
    {
        private const string ClipsFolder = "SFX/";
        private static readonly Dictionary<string, AudioClip> ClipCache = new();

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
            if (clip == null)
            {
                return;
            }

            GameObject sourceObject = new($"Sfx_{clip.name}");
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.pitch = Mathf.Max(0.1f, pitch);
            source.volume = volume;
            source.Play();
            Object.Destroy(sourceObject, clip.length / source.pitch + 0.1f);
        }
    }
}
