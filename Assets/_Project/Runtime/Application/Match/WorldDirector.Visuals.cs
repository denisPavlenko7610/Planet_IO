using UnityEngine;

namespace PlanetIO.Application
{
    public sealed partial class WorldDirector
    {
        private static readonly Color StarColor = new(1f, 0.85f, 0.35f, 0.9f);
        private static readonly Color WormholeColor = new(0.7f, 0.4f, 1f, 0.9f);
        private static readonly Color NebulaColor = new(0.4f, 0.55f, 1f, 0.14f);
        private static readonly Color BlackHoleColor = new(0.04f, 0f, 0.1f, 1f);
        private static readonly Color BandColor = new(1f, 0.55f, 0.2f, 0.12f);

        private Transform _root;
        private SpriteRenderer _band;
        private SpriteRenderer _supernova;
        private SpriteRenderer _blackHole;
        private SpriteRenderer _blackHoleRing;
        private SpriteRenderer _blackHoleZone;
        private SpriteRenderer _portalARing;
        private SpriteRenderer _portalBRing;

        private void BuildVisuals(Vector2[] stars, MapDisc[] nebulae)
        {
            DestroyVisuals();
            _root = new GameObject("WorldFeatures").transform;
            SceneContainers.Attach(_root, SceneContainers.Effects);

            foreach (MapDisc disc in nebulae)
            {
                SpriteRenderer nebula = Create("Nebula", OverlaySprites.Dot, NebulaColor, 30);
                Place(nebula, disc.Center, disc.Radius * 2f);
            }

            foreach (Vector2 star in stars)
            {
                SpriteRenderer core = Create("Star", OverlaySprites.Dot, StarColor, -5);
                Place(core, star, MapFeatureRules.StarRadius * 2f);
                SpriteRenderer zone = Create("StarZone", OverlaySprites.Ring, new Color(1f, 0.85f, 0.35f, 0.18f), -40);
                Place(zone, star, MapFeatureRules.StarZoneRadius * 2f);
            }

            _portalARing = Create("WormholeA", OverlaySprites.Ring, WormholeColor, -6);
            _portalBRing = Create("WormholeB", OverlaySprites.Ring, WormholeColor, -6);
            Place(_portalARing, _portalA, MapFeatureRules.WormholeRadius * 2.4f);
            Place(_portalBRing, _portalB, MapFeatureRules.WormholeRadius * 2.4f);

            _band = Create("MeteorBand", OverlaySprites.Dot, BandColor, -20);
            _supernova = Create("Supernova", OverlaySprites.Dot, StarColor, -4);
            _blackHole = Create("BlackHole", OverlaySprites.Dot, BlackHoleColor, -3);
            _blackHoleRing = Create("BlackHoleRing", OverlaySprites.Ring, WormholeColor, -2);
            _blackHoleZone = Create("BlackHoleZone", OverlaySprites.Ring, new Color(0.7f, 0.4f, 1f, 0.16f), -41);
            SetVisible(false, _band, _supernova);
            SetVisible(false, _blackHole, _blackHoleRing, _blackHoleZone);
        }

        private void DestroyVisuals()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }

            _root = null;
        }

        private void UpdateVisuals(double now)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin((float)now * 3f);
            float portalScale = MapFeatureRules.WormholeRadius * (2.4f + 0.3f * pulse);
            Place(_portalARing, _portalA, portalScale);
            Place(_portalBRing, _portalB, portalScale);

            bool active = _phase != Phase.Idle;
            SetVisible(active && _event.Type == CosmicEventType.MeteorShower, _band);
            SetVisible(active && _event.Type == CosmicEventType.Supernova, _supernova);
            SetVisible(_phase == Phase.Active && _event.Type == CosmicEventType.BlackHole,
                _blackHole, _blackHoleRing, _blackHoleZone);

            if (!active)
            {
                return;
            }

            switch (_event.Type)
            {
                case CosmicEventType.MeteorShower:
                    Place(_band, _event.Position, 1f);
                    _band.transform.localScale = new Vector3(
                        (float)(_event.Speed * _event.DurationSeconds), CosmicEventRules.MeteorBandHalfWidth * 2f, 1f);
                    _band.transform.rotation = Constants.DirectionToRotation(_event.Direction);
                    Color band = BandColor;
                    band.a = _phase == Phase.Warning ? 0.08f + 0.06f * pulse : 0.14f;
                    _band.color = band;
                    break;

                case CosmicEventType.Supernova:
                    double progress = Mathf.Clamp01((float)((now - _event.StartTime) / _event.DurationSeconds));
                    float rate = Mathf.Lerp(2f, 9f, (float)progress);
                    float beat = 0.5f + 0.5f * Mathf.Sin((float)now * rate);
                    Place(_supernova, _event.Position, Mathf.Lerp(3f, 7f, (float)progress) * (0.85f + 0.3f * beat));
                    break;

                case CosmicEventType.BlackHole:
                    Vector2 position = _event.PositionAt(now);
                    Place(_blackHole, position, CosmicEventRules.BlackHoleRadius * 2f);
                    Place(_blackHoleRing, position, CosmicEventRules.BlackHoleRadius * (2.6f + 0.4f * pulse));
                    Place(_blackHoleZone, position, CosmicEventRules.BlackHoleZoneRadius * 2f);
                    break;
            }
        }

        private SpriteRenderer Create(string objectName, Sprite sprite, Color color, int order)
        {
            var created = new GameObject(objectName);
            created.transform.SetParent(_root, false);
            SpriteRenderer renderer = created.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void Place(SpriteRenderer renderer, Vector2 position, float diameter)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.position = new Vector3(position.x, position.y, 0f);
            renderer.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        private static void SetVisible(
            bool visible,
            SpriteRenderer first,
            SpriteRenderer second = null,
            SpriteRenderer third = null)
        {
            Apply(first, visible);
            Apply(second, visible);
            Apply(third, visible);
        }

        private static void Apply(SpriteRenderer renderer, bool visible)
        {
            if (renderer != null && renderer.enabled != visible)
            {
                renderer.enabled = visible;
            }
        }
    }
}
