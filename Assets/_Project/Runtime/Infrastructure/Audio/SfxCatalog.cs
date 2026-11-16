using UnityEngine;
using RD.Audio;

namespace PlanetIO.Infrastructure.Audio
{
    [CreateAssetMenu(menuName = "Planet IO/Audio/Sfx Catalog")]
    public sealed class SfxCatalog : ScriptableObject
    {
        [SerializeField] private AudioCue _eat;
        [SerializeField] private AudioCue _hit;
        [SerializeField] private AudioCue _kill;
        [SerializeField] private AudioCue _death;
        [SerializeField] private AudioCue _boost;
        [SerializeField] private AudioCue _moonHit;
        [SerializeField] private AudioCue _tierUp;
        [SerializeField] private AudioCue _eventWarning;

        public AudioCue Get(SfxId id) => id switch
        {
            SfxId.Eat => _eat,
            SfxId.Hit => _hit,
            SfxId.Kill => _kill,
            SfxId.Death => _death,
            SfxId.Boost => _boost,
            SfxId.MoonHit => _moonHit,
            SfxId.TierUp => _tierUp,
            SfxId.EventWarning => _eventWarning,
            _ => null
        };
    }
}
