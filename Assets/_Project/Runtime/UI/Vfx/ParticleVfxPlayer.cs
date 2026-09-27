using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PlanetIO.UI.Vfx
{
    public sealed class ParticleVfxPlayer : MonoBehaviour, IGameVfxPlayer
    {
        private const int DefaultPoolCapacity = 16;
        private const int MaximumPoolSize = 64;

        [SerializeField] private ParticleSystem _eatPrefab;
        [SerializeField] private ParticleSystem _deathPrefab;
        [SerializeField] private ParticleSystem _impactPrefab;
        [SerializeField] private Vector2 _eatScaleRange = new(0.8f, 1.6f);
        [SerializeField] private Vector2 _deathScaleRange = new(0.6f, 1.8f);
        [SerializeField] private Vector2 _impactScaleRange = new(0.6f, 1.2f);
        [SerializeField] private Vector2 _sourceSizeRange = new(0.1f, 2f);

        private readonly List<(ParticleSystem Effect, IObjectPool<ParticleSystem> Pool)> _active = new();
        private IObjectPool<ParticleSystem> _eatPool;
        private IObjectPool<ParticleSystem> _deathPool;
        private IObjectPool<ParticleSystem> _impactPool;

        private void Awake()
        {
            _eatPool = CreatePool(_eatPrefab);
            _deathPool = CreatePool(_deathPrefab);
            _impactPool = CreatePool(_impactPrefab);
        }

        private void OnEnable()
        {
            GameVfx.Player = this;
        }

        private void OnDisable()
        {
            if (ReferenceEquals(GameVfx.Player, this))
            {
                GameVfx.Player = null;
            }
        }

        private void Update()
        {
            for (int index = _active.Count - 1; index >= 0; index--)
            {
                (ParticleSystem effect, IObjectPool<ParticleSystem> pool) = _active[index];
                if (effect != null && effect.IsAlive(true))
                {
                    continue;
                }

                _active.RemoveAt(index);
                if (effect != null)
                {
                    pool.Release(effect);
                }
            }
        }

        public void PlayEat(Vector2 position, Color color, float size)
        {
            Play(_eatPool, position, color, ScaleFor(size, _eatScaleRange));
        }

        public void PlayDeath(Vector2 position, Color color, float size)
        {
            Play(_deathPool, position, color, ScaleFor(size, _deathScaleRange));
        }

        public void PlayImpact(Vector2 position, float size)
        {
            Play(_impactPool, position, Color.white, ScaleFor(size, _impactScaleRange));
        }

        private float ScaleFor(float sourceSize, Vector2 range)
        {
            return Mathf.Lerp(range.x, range.y, Mathf.InverseLerp(_sourceSizeRange.x, _sourceSizeRange.y, sourceSize));
        }

        private void Play(IObjectPool<ParticleSystem> pool, Vector2 position, Color color, float scale)
        {
            if (pool == null)
            {
                return;
            }

            ParticleSystem effect = pool.Get();
            Transform effectTransform = effect.transform;
            effectTransform.SetPositionAndRotation(position, Quaternion.identity);
            effectTransform.localScale = Vector3.one * scale;

            ParticleSystem.MainModule main = effect.main;
            main.startColor = color;
            effect.Play(true);
            _active.Add((effect, pool));
        }

        private IObjectPool<ParticleSystem> CreatePool(ParticleSystem prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            return new ObjectPool<ParticleSystem>(
                () =>
                {
                    ParticleSystem created = Instantiate(prefab, SceneContainers.Get(SceneContainers.Effects));
                    created.gameObject.SetActive(false);
                    return created;
                },
                effect => effect.gameObject.SetActive(true),
                effect =>
                {
                    effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    effect.gameObject.SetActive(false);
                },
                effect =>
                {
                    if (effect != null)
                    {
                        Destroy(effect.gameObject);
                    }
                },
                false,
                DefaultPoolCapacity,
                MaximumPoolSize);
        }
    }
}
