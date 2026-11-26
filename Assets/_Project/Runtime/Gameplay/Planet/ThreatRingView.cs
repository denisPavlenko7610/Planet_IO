using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    public sealed class ThreatRingView : MonoBehaviour
    {
        private enum ThreatLevel : byte
        {
            None,
            Neutral,
            Prey,
            Danger
        }

        private const float RefreshInterval = 0.2f;
        private const float RingScaleMargin = 1.12f;
        private const float DangerPulseSpeed = 6f;
        private const float GravityPulseSpeed = 2.5f;

        private static readonly Color NeutralColor = new(1f, 1f, 1f, 0.22f);
        private static readonly Color PreyColor = new(0.35f, 1f, 0.45f, 0.65f);
        private static readonly Color DangerColor = new(1f, 0.28f, 0.22f, 0.95f);
        private static readonly Color GravityColor = new(1f, 0.6f, 0.15f, 0.4f);

        private PlanetBody _planet;
        private SpriteRenderer _body;
        private CircleCollider2D _collider;
        private SpriteRenderer _ring;
        private SpriteRenderer _gravityRing;
        private ThreatLevel _level;
        private bool _pullsLocal;
        private float _nextRefresh;

        private void Awake()
        {
            _planet = GetComponent<PlanetBody>();
            _body = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            CreateRings();
        }

        private void OnEnable()
        {
            _nextRefresh = 0f;
        }

        private void LateUpdate()
        {
            if (_ring == null ||
                !ViewBounds.IsVisible(transform.position, _planet.GravityZoneRadius))
            {
                return;
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshInterval;
                Evaluate();
                UpdateGravityRingScale();
            }

            UpdateThreatRing();
            UpdateGravityRing();
        }

        private void UpdateThreatRing()
        {
            if (_level == ThreatLevel.None)
            {
                if (_ring.enabled)
                {
                    _ring.enabled = false;
                }

                return;
            }

            Color color = _level switch
            {
                ThreatLevel.Danger => DangerColor,
                ThreatLevel.Prey => PreyColor,
                _ => NeutralColor
            };

            if (_level == ThreatLevel.Danger)
            {
                color.a *= 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * DangerPulseSpeed);
            }

            Sprite shape = _level == ThreatLevel.Danger ? OverlaySprites.ThickRing : OverlaySprites.Ring;
            if (_ring.sprite != shape)
            {
                _ring.sprite = shape;
            }

            _ring.enabled = true;
            _ring.color = color;
            _ring.sortingLayerID = _body.sortingLayerID;
            _ring.sortingOrder = _body.sortingOrder - 1;
        }

        private void UpdateGravityRing()
        {
            bool show = _pullsLocal && _level != ThreatLevel.None;
            if (_gravityRing.enabled != show)
            {
                _gravityRing.enabled = show;
            }

            if (!show)
            {
                return;
            }

            Color color = GravityColor;
            color.a *= 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * GravityPulseSpeed);
            _gravityRing.color = color;
            _gravityRing.sortingLayerID = _body.sortingLayerID;
            _gravityRing.sortingOrder = _body.sortingOrder - 3;
        }

        private void UpdateGravityRingScale()
        {
            Sprite sprite = _gravityRing.sprite;
            float localRadius = _collider != null ? _collider.radius : 0.5f;
            float zoneLocalRadius = localRadius * GravityRules.ZoneFactor * Mathf.Max(1f, _planet.GravityZoneMultiplier);
            float scale = 2f * zoneLocalRadius / Mathf.Max(0.0001f, sprite.bounds.size.x);
            _gravityRing.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void Evaluate()
        {
            _level = ThreatLevel.None;
            _pullsLocal = false;

            if (_planet == null || !_planet.IsSpawned || !_planet.AreMoonsVisible)
            {
                return;
            }

            Player local = ResolveLocalPlayer();
            if (local == null || local == _planet || local.IsDefeated)
            {
                return;
            }

            _pullsLocal = _planet.Pulls(local);

            if (_planet.CanDevour(local))
            {
                _level = ThreatLevel.Danger;
            }
            else
            {
                _level = local.CanDevour(_planet) ? ThreatLevel.Prey : ThreatLevel.Neutral;
            }
        }

        private static Player ResolveLocalPlayer()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.LocalClient == null)
            {
                return null;
            }

            NetworkObject playerObject = manager.LocalClient.PlayerObject;
            return playerObject != null ? playerObject.GetComponent<Player>() : null;
        }

        private void CreateRings()
        {
            if (_body == null)
            {
                return;
            }

            Sprite sprite = OverlaySprites.Ring;
            float localRadius = _collider != null ? _collider.radius : 0.5f;
            float scale = 2f * localRadius * RingScaleMargin / Mathf.Max(0.0001f, sprite.bounds.size.x);

            _ring = CreateRing("ThreatRing", sprite, scale);
            _gravityRing = CreateRing("GravityRing", sprite, scale * GravityRules.ZoneFactor);
        }

        private SpriteRenderer CreateRing(string ringName, Sprite sprite, float scale)
        {
            var ringObject = new GameObject(ringName);
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localScale = new Vector3(scale, scale, 1f);

            SpriteRenderer renderer = ringObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.enabled = false;
            return renderer;
        }
    }
}
