using System.Collections.Generic;
using UnityEngine;

namespace Planet_IO
{
    [CreateAssetMenu(menuName = "Planet IO/Enemies/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [field: SerializeField] public List<Sprite> EnemySprites { get; private set; }
    }
}
