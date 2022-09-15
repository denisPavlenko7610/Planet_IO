using UnityEngine;

namespace Planet_IO
{
    public abstract class EnemyState : MonoBehaviour
    {
        public abstract EnemyState RunCurrentState();
    }
}
