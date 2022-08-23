using Dythervin.AutoAttach;
using UnityEngine;
using System.Collections;


namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Script Player")]
        [SerializeField, Attach(Attach.Scene)] private AccelerationButton _accelerationButton;
        [SerializeField, Attach] private Player _player;
        
        [Space]
        [SerializeField, Attach(Attach.Scene)] private Camera mainCamera;
        [SerializeField, Attach] private Rigidbody2D rigidbody2D;
        
        [Header("Speed")]
        [SerializeField] private float _normalSpeed = 3f;
        [SerializeField] private float _boostSpeed;
        [SerializeField] private float _timeToTeak = 1f;
        private float _currentSpeed;
        private bool _isBoost;

        private bool _mouseInPlayer;
        private float _angel;
        private Vector2 _mousePosition;
        private Coroutine _boostCoroutine;

        private void Start() => _currentSpeed = _normalSpeed;

        void Update()
        {
            if (!_mouseInPlayer)
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

        private void OnMouseEnter() => _mouseInPlayer = true;
        private void OnMouseExit() => _mouseInPlayer = false;

        private void RotationPlayer()
        {
            _angel = Mathf.Atan2(_mousePosition.y, _mousePosition.x) * Mathf.Rad2Deg - 90f;
            rigidbody2D.rotation = _angel;
        }


        private void FixedUpdate()
        {
            MovePlayer();
            RotationPlayer();
        } 

        private void Boost()
        {
            _isBoost = true;
            _currentSpeed = _boostSpeed;
            _boostCoroutine ??= StartCoroutine(ScaleBoost());
        }

        private void SetNormalSpeed()
        {
            _isBoost = false;
            _currentSpeed = _normalSpeed;
        }
        private void MovePlayer() => rigidbody2D.velocity = _mousePosition.normalized * _currentSpeed;

        private IEnumerator ScaleBoost()
        {
            while (_isBoost)
            {
                _player.SpeedLogics();
                yield return new WaitForSeconds(_timeToTeak);
            }
            _boostCoroutine = null;
        }
        
    }
}