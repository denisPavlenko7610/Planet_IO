using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PlanetIO
{
    public class CometPool : MonoBehaviour
    {
        [field: SerializeField] public int CometCount { get; private set; } = 100;
        [field: SerializeField] public Comet cometPrefab { get; private set; }
        [field: SerializeField] public IObjectPool<Comet> PoolComet { get; private set; }

        private void Awake()
        {
            var maxCometsMultiplier = 2;
            PoolComet = new ObjectPool<Comet>(OnCreateComet, OnGetComet, OnDisableComet, OnDestroyComet,true,
                                              CometCount, CometCount * maxCometsMultiplier);
        }

        private void OnDestroyComet(Comet comet)
        {
            Destroy(comet.gameObject);
        }

        private void OnGetComet(Comet comet)
        {
            comet.gameObject.SetActive(true);
            comet.transform.SetParent(transform, true);
        }

        private void OnDisableComet(Comet comet)
        {
            comet.gameObject.SetActive(false);
        }

        private Comet OnCreateComet()
        {
            var comet = Instantiate(cometPrefab);
            comet.SetPool(PoolComet);
            comet.gameObject.SetActive(false);
            return comet;
        }
    }
}

