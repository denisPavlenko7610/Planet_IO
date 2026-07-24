using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    [CreateAssetMenu(menuName = "Planet IO/Enemies/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [SerializeField] private List<Sprite> _enemySprites = new();

        public Sprite GetSprite(ulong deterministicSeed)
        {
            if (_enemySprites == null || _enemySprites.Count == 0)
            {
                return null;
            }

            int index = (int)(deterministicSeed % (ulong)_enemySprites.Count);
            return _enemySprites[index];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _enemySprites?.RemoveAll(sprite => sprite == null);
        }
#endif
    }
}
