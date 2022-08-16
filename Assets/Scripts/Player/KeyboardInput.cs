using System;
using UnityEngine;
using Zenject;

namespace PlanetIO
{
    public class KeyboardInput : MonoBehaviour
    {
        private PlayerMovement _playerMovement;

        [Inject]
        private void Construct(PlayerMovement playerMovement) => _playerMovement = playerMovement;
        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift)) 
                _playerMovement.Boost();
            else
                _playerMovement.NormalSpeed();
        }
    }
}

