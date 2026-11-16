using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UIElements;

namespace PlanetIO
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class OnScreenStickView : OnScreenControl
    {
        private const float MaxDeflection = 78f;
        private const float DeadZone = 0.2f;
        private const string ActiveClass = "stick-active";

        [InputControl(layout = "Vector2")]
        [SerializeField] private string _controlPath = "<Gamepad>/leftStick";

        private VisualElement _zone;
        private VisualElement _stick;
        private VisualElement _knob;
        private Vector2 _anchor;
        private int _pointerId = -1;

        protected override string controlPathInternal
        {
            get => _controlPath;
            set => _controlPath = value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _zone = root.Q<VisualElement>("stick-zone");
            _stick = root.Q<VisualElement>("stick");
            _knob = root.Q<VisualElement>("stick-knob");
            _zone.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _zone.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _zone.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _zone.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            _zone.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void OnDisable()
        {
            _zone.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _zone.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _zone.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _zone.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            _zone.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            Release();
            base.OnDisable();
        }

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (_pointerId >= 0 || pointerEvent.button > 0)
            {
                return;
            }

            _pointerId = pointerEvent.pointerId;
            _zone.CapturePointer(_pointerId);
            pointerEvent.StopPropagation();

            Vector2 finger = _zone.WorldToLocal(pointerEvent.position);
            _anchor = ClampAnchor(finger);
            PlaceStick();
            _stick.AddToClassList(ActiveClass);
            Drive(finger);
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (pointerEvent.pointerId == _pointerId)
            {
                Drive(_zone.WorldToLocal(pointerEvent.position));
            }
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (pointerEvent.pointerId != _pointerId)
            {
                return;
            }

            _zone.ReleasePointer(_pointerId);
            Release();
        }

        private void OnPointerCancel(PointerCancelEvent pointerEvent)
        {
            if (pointerEvent.pointerId == _pointerId)
            {
                Release();
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            if (pointerEvent.pointerId == _pointerId)
            {
                Release();
            }
        }

        private void Drive(Vector2 finger)
        {
            Vector2 offset = finger - _anchor;
            float distance = offset.magnitude;
            if (distance > MaxDeflection)
            {
                _anchor = ClampAnchor(_anchor + offset / distance * (distance - MaxDeflection));
                PlaceStick();
                offset = Vector2.ClampMagnitude(finger - _anchor, MaxDeflection);
            }

            _knob.style.translate = new Translate(offset.x, offset.y);

            Vector2 shaped = StickResponse.ApplyDeadZone(new Vector2(offset.x, -offset.y) / MaxDeflection, DeadZone);
            InputSettings settings = InputSystem.settings;
            SendValueToControl(StickResponse.PreCompensate(shaped, settings.defaultDeadzoneMin, settings.defaultDeadzoneMax));
        }

        private Vector2 ClampAnchor(Vector2 point)
        {
            Vector2 half = _stick.layout.size * 0.5f;
            Vector2 zoneSize = _zone.layout.size;
            return new Vector2(
                Mathf.Clamp(point.x, half.x, Mathf.Max(half.x, zoneSize.x - half.x)),
                Mathf.Clamp(point.y, half.y, Mathf.Max(half.y, zoneSize.y - half.y)));
        }

        private void PlaceStick()
        {
            Vector2 half = _stick.layout.size * 0.5f;
            _stick.style.left = _anchor.x - half.x;
            _stick.style.top = _anchor.y - half.y;
            _stick.style.right = StyleKeyword.Auto;
            _stick.style.bottom = StyleKeyword.Auto;
        }

        private void Release()
        {
            _pointerId = -1;
            if (_stick != null)
            {
                _stick.RemoveFromClassList(ActiveClass);
                _stick.style.left = StyleKeyword.Null;
                _stick.style.top = StyleKeyword.Null;
                _stick.style.right = StyleKeyword.Null;
                _stick.style.bottom = StyleKeyword.Null;
            }

            if (_knob != null)
            {
                _knob.style.translate = new Translate(0f, 0f);
            }

            SendValueToControl(Vector2.zero);
        }
    }
}
