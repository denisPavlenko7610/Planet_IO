using UnityEngine;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    public sealed class MoonRingView : MonoBehaviour
    {
        private const float AppearSeconds = 0.25f;

        private readonly SpriteRenderer[] _moons = new SpriteRenderer[MoonLoadout.MaxSlots];
        private readonly float[] _appear = new float[MoonLoadout.MaxSlots];

		private PlanetBody _planet;
        private SpriteRenderer _body;
        private Transform _root;

        private void Awake()
        {
			_planet = GetComponent<PlanetBody>();
            _body = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
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
            if (_planet == null || _body == null || _body.sprite == null || !_planet.IsSpawned)
            {
                return;
            }

            int slots = _planet.MoonSlots;
            byte mask = _planet.MoonMask;
            bool visible = _planet.AreMoonsVisible;

            if (!visible && _root == null)
            {
                return;
            }

            float bodyRadius = _planet.BodyRadius;
            if (!ViewBounds.IsVisible(transform.position, MoonRules.OuterExtent(bodyRadius)))
            {
				if (_root != null && _root.gameObject.activeSelf)
				{
					_root.gameObject.SetActive(false);
				}

                return;
            }

            EnsureRoot();
			if (!_root.gameObject.activeSelf)
			{
				_root.gameObject.SetActive(true);
			}

            Sprite sprite = _body.sprite;
            float moonRadius = MoonRules.MoonRadius(bodyRadius);
            float moonScale = 2f * moonRadius / Mathf.Max(0.0001f, sprite.bounds.size.x);
            float phase = _planet.MoonPhaseDegrees;
            double time = OrbitClock.Now;
            Vector3 center = transform.position;
            float step = Time.deltaTime / AppearSeconds;

            for (int index = 0; index < MoonLoadout.MaxSlots; index++)
            {
                bool alive = visible && index < slots && (mask & (1 << index)) != 0;
                SpriteRenderer moon = _moons[index];

                if (!alive)
                {
                    _appear[index] = 0f;
                    if (moon != null && moon.enabled)
                    {
                        moon.enabled = false;
                    }

                    continue;
                }

                if (moon == null)
                {
                    moon = CreateMoon(index);
                }

                _appear[index] = Mathf.MoveTowards(_appear[index], 1f, step);
                Vector2 position = MoonRules.Position(center, bodyRadius, index, slots, time, phase);

                if (!moon.enabled)
                {
                    moon.enabled = true;
                }

                if (moon.sprite != sprite)
                {
                    moon.sprite = sprite;
                }

                Color bodyColor = _body.color;
                if (moon.color != bodyColor)
                {
                    moon.color = bodyColor;
                }

                int sortingLayerId = _body.sortingLayerID;
                if (moon.sortingLayerID != sortingLayerId)
                {
                    moon.sortingLayerID = sortingLayerId;
                }

                int sortingOrder = _body.sortingOrder + 1;
                if (moon.sortingOrder != sortingOrder)
                {
                    moon.sortingOrder = sortingOrder;
                }

                moon.transform.SetPositionAndRotation(
                    new Vector3(position.x, position.y, center.z),
                    Quaternion.identity);
                moon.transform.localScale = Vector3.one * (moonScale * Mathf.SmoothStep(0f, 1f, _appear[index]));
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject($"Moons_{name}").transform;
            SceneContainers.Attach(_root, SceneContainers.Effects);
        }

        private SpriteRenderer CreateMoon(int index)
        {
            var moonObject = new GameObject($"Moon_{index}");
            moonObject.transform.SetParent(_root, false);
            SpriteRenderer renderer = moonObject.AddComponent<SpriteRenderer>();
            _moons[index] = renderer;
            return renderer;
        }
    }
}
