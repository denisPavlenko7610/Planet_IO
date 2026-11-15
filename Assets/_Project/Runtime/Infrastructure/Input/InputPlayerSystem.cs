using RD.Attributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlanetIO
{
    public sealed class InputPlayerSystem : NetworkBehaviour
    {
        [SerializeField, Assign] private PlayerMovement _playerMovement;

        private PlayerControls _controls;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _controls = new PlayerControls();
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

        private void UpdateInput(InputAction.CallbackContext context)
        {
            if (!IsOwner)
            {
                return;
            }

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
