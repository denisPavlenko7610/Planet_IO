using RDTools;
using RDTools.AutoAttach;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    public class Enemy : PlanetScale
    {
        [Expandable] [SerializeField] private EnemyDate _enemyDate;
        [SerializeField, Attach] private SpriteRenderer _spriteRenderer;

        private Sprite _sprite;
        
        [Header("Spawner")] 
        [SerializeField] private CometsSpawnerLogics _cometsSpawnerLogics;
        [SerializeField] private PointsSpawnerLogics _pointsSpawnerLogics;

        private void Start()
        {
            Init();
            SetRandomSprite(_sprite);
        }

        public void InitDependencies(CometsSpawnerLogics cometsSpawnerLogics,PointsSpawnerLogics pointsSpawnerLogics )
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }

        private void OnTriggerEnter2D(Collider2D collider2D)
        {
            if (collider2D.TryGetComponent(out Point point))
            {
                SetCapacity(point.Capacity);
                _pointsSpawnerLogics.CreatePoint(point);
            }
            else if (collider2D.TryGetComponent(out Comet comet))
            {
                SetCapacity(-comet.Capacity);
                _cometsSpawnerLogics.CreateComet(comet);
            }
        }

        private void SetRandomSprite(Sprite sprite)
        {
            var index = Random.Range(0, _enemyDate.EnemySprites.Count);
            sprite = _enemyDate.EnemySprites[index];
            _spriteRenderer.sprite = sprite;
        }
    }
}