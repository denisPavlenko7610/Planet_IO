using RDTools;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    [RequireComponent(typeof(EnemyScale))]
    public class Enemy : Planet
    {
        [Expandable]
        [SerializeField] private EnemyDate _enemyDate;

        private Sprite _sprite;

        private void Start() => SetRandomSprite(_sprite);

        [Inject]
        private void Construct(CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.TryGetComponent(out Point point))
            {
                _scale.SetCapacity(point.Capacity);
                _pointsSpawnerLogics.CreatePoint(point);
            }
            else if (col.TryGetComponent(out Comet comet))
            {
                _scale.SetCapacity(-comet.Capacity);
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
