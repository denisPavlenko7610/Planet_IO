using UnityEngine;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    public sealed class CometTailView : MonoBehaviour
    {
        private TrailRenderer _trail;
        [SerializeField] private Material _material;
        private Comet _comet;
        private Vector3 _previous;

        private void Awake()
        {
            _comet = GetComponent<Comet>();
            SpriteRenderer body = GetComponentInChildren<SpriteRenderer>();
            var tail = new GameObject("CometTail");
            tail.transform.SetParent(transform, false);
            _trail = tail.AddComponent<TrailRenderer>();
            _trail.sharedMaterial = _material;
            var texture = new MaterialPropertyBlock();
            texture.SetTexture("_MainTex", OverlaySprites.Dot.texture);
            _trail.SetPropertyBlock(texture);
            _trail.time = CometRules.TailSeconds;
            _trail.minVertexDistance = 0.08f;
            _trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.9f, 0.98f, 1f), 0f),
                    new GradientColorKey(new Color(0.1f, 0.45f, 1f), 0.4f),
                    new GradientColorKey(new Color(0.05f, 0.2f, 0.8f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            _trail.colorGradient = gradient;
            _trail.numCapVertices = 3;
            _trail.sortingLayerID = body != null ? body.sortingLayerID : 0;
            _trail.sortingOrder = body != null ? body.sortingOrder - 1 : -1;
            Clear();
        }

        private void OnEnable() => Clear();
        private void OnDisable() => Clear();

        public void Clear()
        {
            _previous = transform.position;
            if (_trail != null)
            {
                _trail.emitting = false;
                _trail.Clear();
            }
        }

        private void LateUpdate()
        {
            float distance = Vector3.Distance(transform.position, _previous);
            if (!_comet.IsSpawned || distance > Mathf.Max(1f, Time.deltaTime * 20f))
            {
                Clear();
                return;
            }

            _trail.widthMultiplier = Mathf.Clamp(transform.lossyScale.x * 0.65f, 0.12f, 1.2f);
            _trail.emitting = distance > 0.00001f;
            _previous = transform.position;
        }

    }
}
