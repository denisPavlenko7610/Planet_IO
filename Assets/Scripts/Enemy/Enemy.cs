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
        private CometsSpawnerLogics _cometsSpawnerLogics;
        private PointsSpawnerLogics _pointsSpawnerLogics;
        
        [Inject]
        private void Construct(CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }

        private void Start()
        {
            Init();
            SetRandomSprite(_sprite);
        }

        private void OnTriggerEnter2D(Collider2D collider2D)
        {
            if (collider2D.TryGetComponent(out Point point))
            {
                SetCapacity(point.Capacity);
                if (_pointsSpawnerLogics == null)
                {
                    return;
                }
                
                _pointsSpawnerLogics.CreatePoint(point);
            }
            else if (collider2D.TryGetComponent(out Comet comet))
            {
                SetCapacity(-comet.Capacity);

                if (_cometsSpawnerLogics == null)
                {
                    return;
                }
                
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