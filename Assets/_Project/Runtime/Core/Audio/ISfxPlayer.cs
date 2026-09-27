using System;
using UnityEngine;

namespace PlanetIO
{
    public enum SfxId : byte
    {
        Eat,
        Hit,
        Kill,
        Death,
        Boost
    }

    public interface ISfxPlayer
    {
        void Play(SfxId id, float pitchMultiplier = 1f, float volumeMultiplier = 1f);
        IDisposable PlayLoop(SfxId id, Transform follow);
    }
}
