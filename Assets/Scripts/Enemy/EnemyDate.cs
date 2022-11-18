using System.Collections.Generic;
using UnityEngine;

namespace Planet_IO
{
    [CreateAssetMenu(menuName = "Sample/Enemy/Date")]
    public class EnemyDate : ScriptableObject
    {
        [field: SerializeField] public List<Sprite> EnemySprites { private set; get; }
    }
}