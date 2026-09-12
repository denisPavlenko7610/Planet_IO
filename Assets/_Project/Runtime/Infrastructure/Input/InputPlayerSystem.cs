using PlanetIO.Core.Attributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetIO
{
    public sealed class InputPlayerSystem : NetworkBehaviour
    {
        [SerializeField, Assign] private PlayerMovement _playerMovement;

        private PlayerControls _controls;
        private Camera _camera;
        private bool _pointerSteeringActive;

		private void Awake()
        {
            if (_playerMovement == null)
            {
                _playerMovement = GetComponent<PlayerMovement>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _controls = new PlayerControls();
            _controls.Move.Movement
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _controls.Move.Movement
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _controls.Enable();
            _controls.Move.Movement.performed += UpdateInput;
            _controls.Move.Movement.canceled += CanceledInput;
        }

        public override void OnNetworkDespawn()
        {
            ReleaseInput();
            base.OnNetworkDespawn();
        }

        private void OnDisable() => ReleaseInput();

        private void Update()
        {
            if (!IsSpawned || !IsOwner || _playerMovement == null)
            {
                return;
            }

            if (TryGetTouchScreenPosition(out Vector2 touchPosition))
            {
                _pointerSteeringActive = true;
                SteerTo(touchPosition);
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.delta.ReadValue().sqrMagnitude > 0.01f)
            {
                _pointerSteeringActive = true;
            }

            if (!_pointerSteeringActive)
            {
                return;
            }

            SteerTo(mouse.position.ReadValue());
        }

        private bool TryGetTouchScreenPosition(out Vector2 position)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.isInProgress)
            {
                position = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }

        private void SteerTo(Vector2 screenPosition)
        {
            _camera ??= UnityEngine.Camera.main;
            if (_camera == null)
            {
                return;
            }

            Vector3 playerPosition = _playerMovement.Player.transform.position;
            Vector3 worldPosition = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y,
                -_camera.transform.position.z));
            _playerMovement.SetDirection(worldPosition - playerPosition);
        }

        private void UpdateInput(InputAction.CallbackContext context)
        {
            if (!IsOwner)
            {
                return;
            }

            _pointerSteeringActive = false;

            Vector2 direction = _controls.Move.Movement.ReadValue<Vector2>();
            _playerMovement.SetDirection(direction);
        }

        private void CanceledInput(InputAction.CallbackContext context)
        {
            _playerMovement.SetDirection(Vector2.zero);
        }

        private void ReleaseInput()
        {
            if (_controls == null)
            {
                return;
            }

            _controls.Move.Movement.performed -= UpdateInput;
            _controls.Move.Movement.canceled -= CanceledInput;
            _controls.Disable();
            _controls.Dispose();
            _controls = null;
        }
    }
}
