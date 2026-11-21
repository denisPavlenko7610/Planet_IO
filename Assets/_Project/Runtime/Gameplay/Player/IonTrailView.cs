using UnityEngine;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    public sealed class IonTrailView : MonoBehaviour
    {
        private const int DotCapacity = 24;
        private const float SpawnInterval = 0.1f;
        private const float MaximumAlpha = 0.8f;

		private const float SparkLengthFactor = 0.85f;
		private const float SparkWidthFactor = 0.18f;

        private static readonly Color TrailColor = new(1f, 0.55f, 0.2f, 1f);

        private readonly SpriteRenderer[] _dots = new SpriteRenderer[DotCapacity];
        private readonly float[] _remaining = new float[DotCapacity];

        private Player _player;
        private SpriteRenderer _body;
        private Transform _root;
        private int _next;
        private float _spawnTimer;

        private void Awake()
        {
            _player = GetComponent<Player>();
            _body = GetComponent<SpriteRenderer>();
        }

        private void OnDisable()
        {
            for (int index = 0; index < DotCapacity; index++)
            {
                _remaining[index] = 0f;
                if (_dots[index] != null)
                {
                    _dots[index].enabled = false;
                }
            }
        }

        private void OnDestroy()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }
        }

        private void LateUpdate()
        {
            if (_player == null || _body == null || !_player.IsSpawned)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            bool emitting = _player.IsBoosting && _player.HasPerk(PerkId.IonTrail) && _player.AreMoonsVisible;

            if (emitting)
            {
                _spawnTimer -= deltaTime;
                if (_spawnTimer <= 0f)
                {
                    _spawnTimer = SpawnInterval;
                    Spawn();
                }
            }

            for (int index = 0; index < DotCapacity; index++)
            {
                SpriteRenderer dot = _dots[index];
                if (dot == null || _remaining[index] <= 0f)
                {
                    continue;
                }

                _remaining[index] -= deltaTime;
                if (_remaining[index] <= 0f)
                {
                    dot.enabled = false;
                    continue;
                }

                Color color = TrailColor;
                color.a = MaximumAlpha * Mathf.Clamp01(_remaining[index] / PerkRules.IonTrailPointSeconds);
                dot.color = color;
            }
        }

        private void Spawn()
        {
            EnsureDots();

            SpriteRenderer dot = _dots[_next];
            _remaining[_next] = PerkRules.IonTrailPointSeconds;
            _next = (_next + 1) % DotCapacity;

			float diameter = _player.TrailRadius * 2f;
			dot.transform.SetPositionAndRotation(transform.position, transform.rotation);
			dot.transform.localScale = new Vector3(diameter * SparkLengthFactor, diameter * SparkWidthFactor, 1f);
            dot.sortingLayerID = _body.sortingLayerID;
            dot.sortingOrder = _body.sortingOrder - 1;
            dot.color = TrailColor;
            dot.enabled = true;
        }

        private void EnsureDots()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject($"IonTrail_{name}").transform;
            SceneContainers.Attach(_root, SceneContainers.Effects);

            for (int index = 0; index < DotCapacity; index++)
            {
                var dotObject = new GameObject($"Dot_{index}");
                dotObject.transform.SetParent(_root, false);
                SpriteRenderer renderer = dotObject.AddComponent<SpriteRenderer>();
                renderer.sprite = OverlaySprites.Dot;
                renderer.enabled = false;
                _dots[index] = renderer;
            }
        }
    }
}
