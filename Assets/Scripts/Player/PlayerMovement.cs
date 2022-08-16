using System;
using Dythervin.AutoAttach;
using UnityEngine;

namespace PlanetIO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Attach(Attach.Scene)] private Camera mainCamera;
        [SerializeField, Attach] private Rigidbody2D rigidbody2D;
        [SerializeField] private float _normalSpeed = 3f;
        [SerializeField] private float _boostSpeed;
        private float _currentSpeed;
        private Vector2 mousePosition;

        private void Start()
        {
            _currentSpeed = _normalSpeed;
        }

        void Update()
        {
            mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        }

        private void FixedUpdate() => MovePlayer();

        private  void MovePlayer() =>  rigidbody2D.velocity = mousePosition.normalized * _currentSpeed;
        public void Boost() => _currentSpeed = _boostSpeed;
        public void NormalSpeed() => _currentSpeed = _normalSpeed;
    }
}