using PlanetIO.Core.Attributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet_IO
{
    public sealed class InputPlayerSystem : NetworkBehaviour
    {
        [SerializeField, Assign] private PlayerMovement _playerMovement;

        private PlayerInput _playerInput;
        private UnityEngine.Camera _camera;

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

            _playerInput = new PlayerInput();
            _playerInput.Move.Movement
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _playerInput.Move.Movement
                .AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _playerInput.Enable();
            _playerInput.Move.Movement.performed += UpdateInput;
            _playerInput.Move.Movement.canceled += CanceledInput;
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

            _camera ??= UnityEngine.Camera.main;
            if (_camera == null || Mouse.current == null)
            {
                return;
            }

            if (Mouse.current.delta.ReadValue().sqrMagnitude <= 0.01f)
            {
                return;
            }

            Vector3 playerPosition = _playerMovement.Player.transform.position;
            Vector2 pointerPosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = _camera.ScreenToWorldPoint(new Vector3(
                pointerPosition.x,
                pointerPosition.y,
                -_camera.transform.position.z));
            _playerMovement.SetDirection(worldPosition - playerPosition);
        }

        private void UpdateInput(InputAction.CallbackContext context)
        {
            if (!IsOwner)
            {
                return;
            }

            Vector2 direction = _playerInput.Move.Movement.ReadValue<Vector2>();
            _playerMovement.SetDirection(direction);
        }

        private void CanceledInput(InputAction.CallbackContext context)
        {
            _playerMovement.SetDirection(Vector2.zero);
        }

        private void ReleaseInput()
        {
            if (_playerInput == null)
            {
                return;
            }

            _playerInput.Move.Movement.performed -= UpdateInput;
            _playerInput.Move.Movement.canceled -= CanceledInput;
            _playerInput.Disable();
            _playerInput.Dispose();
            _playerInput = null;
        }
    }
}
