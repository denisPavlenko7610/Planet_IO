using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace PlanetIO
{
    public class Comet : MonoBehaviour
    {
        private IObjectPool<Comet> _cometPool;

        public void SetPool(IObjectPool<Comet> cometPool) => _cometPool = cometPool;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent<Player>(out Player player))
            {
                _cometPool.Release(this);
            }
        }
    }
}