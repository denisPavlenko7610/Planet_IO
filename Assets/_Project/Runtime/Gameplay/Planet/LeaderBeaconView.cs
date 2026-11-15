using UnityEngine;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    public sealed class LeaderBeaconView : MonoBehaviour
    {
        private const float RingScaleMargin = 1.3f;
        private const float PulseSpeed = 3f;
        private const float PulseAmount = 0.05f;

        private static readonly Color BeaconColor = new(1f, 0.82f, 0.25f, 0.9f);

        private PlanetScale _planet;
        private SpriteRenderer _body;
        private SpriteRenderer _ring;
        private float _baseScale;

        private void Awake()
        {
            _planet = GetComponent<PlanetScale>();
            _body = GetComponent<SpriteRenderer>();
            CreateRing();
        }

        private void LateUpdate()
        {
            if (_ring == null || _planet == null)
            {
                return;
            }

            bool show = _planet.IsSpawned && _planet.IsLeader && _planet.AreMoonsVisible;
            if (_ring.enabled != show)
            {
                _ring.enabled = show;
            }

            if (!show)
            {
                return;
            }

            float pulse = Mathf.Sin(Time.unscaledTime * PulseSpeed);
            float scale = _baseScale * (1f + pulse * PulseAmount);
            _ring.transform.localScale = new Vector3(scale, scale, 1f);

            Color color = BeaconColor;
            color.a *= 0.75f + 0.25f * pulse;
            _ring.color = color;
            _ring.sortingLayerID = _body.sortingLayerID;
            _ring.sortingOrder = _body.sortingOrder - 2;
        }

        private void CreateRing()
        {
            if (_body == null)
            {
                return;
            }

            var ringObject = new GameObject("LeaderBeacon");
            ringObject.transform.SetParent(transform, false);

            Sprite sprite = OverlaySprites.Ring;
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            float localRadius = collider != null ? collider.radius : 0.5f;
            _baseScale = 2f * localRadius * RingScaleMargin / Mathf.Max(0.0001f, sprite.bounds.size.x);
            ringObject.transform.localScale = new Vector3(_baseScale, _baseScale, 1f);

            _ring = ringObject.AddComponent<SpriteRenderer>();
            _ring.sprite = sprite;
            _ring.enabled = false;
        }
    }
}
