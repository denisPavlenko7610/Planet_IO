using UnityEngine;
using UnityTemplates.Audio;

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

        public AudioCue Get(SfxId id) => id switch
        {
            SfxId.Eat => _eat,
            SfxId.Hit => _hit,
            SfxId.Kill => _kill,
            SfxId.Death => _death,
            SfxId.Boost => _boost,
            _ => null
        };
    }
}
