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
        [InputControl(layout = "Vector2")]
        [SerializeField] private string _controlPath = "<Gamepad>/leftStick";

        [SerializeField, Min(8f)] private float _movementRange = 100f;

        private VisualElement _stick;
        private VisualElement _knob;
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
            _stick = root.Q<VisualElement>("stick");
            _knob = root.Q<VisualElement>("stick-knob");
            _stick.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _stick.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _stick.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _stick.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            _stick.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void OnDisable()
        {
            _stick.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _stick.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            _stick.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _stick.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            _stick.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            Release();
            base.OnDisable();
        }

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (_pointerId >= 0)
            {
                return;
            }

            _pointerId = pointerEvent.pointerId;
            _stick.CapturePointer(_pointerId);
            pointerEvent.StopPropagation();
            Drive(pointerEvent.localPosition);
        }

        private void OnPointerMove(PointerMoveEvent pointerEvent)
        {
            if (pointerEvent.pointerId == _pointerId)
            {
                Drive(pointerEvent.localPosition);
            }
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (pointerEvent.pointerId != _pointerId)
            {
                return;
            }

            _stick.ReleasePointer(_pointerId);
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

        private void Drive(Vector2 localPosition)
        {
            Vector2 center = _stick.contentRect.size * 0.5f;
            Vector2 offset = localPosition - center;
            offset = Vector2.ClampMagnitude(offset, _movementRange);

            _knob.style.translate = new Translate(offset.x, offset.y);
            SendValueToControl(new Vector2(offset.x, -offset.y) / _movementRange);
        }

        private void Release()
        {
            _pointerId = -1;
            if (_knob != null)
            {
                _knob.style.translate = new Translate(0f, 0f);
            }

            SendValueToControl(Vector2.zero);
        }
    }
}
