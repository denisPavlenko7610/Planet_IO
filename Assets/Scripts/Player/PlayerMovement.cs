using Dythervin.AutoAttach;
using UnityEngine;

namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Attach(Attach.Scene)] private Camera mainCamera;
        [SerializeField, Attach(Attach.Scene)] private AccelerationButton _accelerationButton;
        [SerializeField, Attach] private Rigidbody2D rigidbody2D;
        [SerializeField] private float _normalSpeed = 3f;
        [SerializeField] private float _boostSpeed;

        private bool _isBoost;
        private float _currentSpeed;
        private Vector2 _mousePosition;

        private void Start()
        {
            _currentSpeed = _normalSpeed;
        }

        void Update()
        {
            _mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;

            if (_accelerationButton.IsPressed)
            {
                Boost();
            }
            else
            {
                SetNormalSpeed();
            }
        }

        private void FixedUpdate() => MovePlayer();

        private void Boost() => _currentSpeed = _boostSpeed;
        private void SetNormalSpeed() => _currentSpeed = _normalSpeed;
        private void MovePlayer() => rigidbody2D.velocity = _mousePosition.normalized * _currentSpeed;
    }
}