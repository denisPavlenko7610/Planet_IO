using Planet_IO.Camera;
using Planet_IO.Utils;
using RDTools;
using RDTools.AutoAttach;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Planet_IO.Arrow
{
    public class Arrow : NetworkBehaviour
    {
        [SerializeField, Attach] private Image _arrowImage;
        [SerializeField, ReadOnly] private PlayerCamera _playerCamera;

        private Player _player;
        private PlayerMovement _playerMovement;

        private void Start()
        {
            _playerMovement = FindObjectOfType<PlayerMovement>();
            _playerCamera = FindObjectOfType<PlayerCamera>();
            
            if(_playerMovement)
                _player = _playerMovement.Player;
        }

        private void LateUpdate()
        {
            if(_playerMovement == null)
                return;
            
            if (_player && _playerMovement && _playerCamera)
            {
                SetArrowPosition();
                SetArrowRotation();
            }
        }

        private void SetArrowPosition()
        {
            _arrowImage.transform.position = _playerCamera.Camera
                .WorldToScreenPoint(_player.transform.position + _player.transform.right *
                (_player.Capacity * Constants.ArrowPositionMult));
        }

        private void SetArrowRotation()
        {
            var rotationAngle = Mathf.Atan2(_playerMovement.Direction.y, _playerMovement.Direction.x) * Mathf.Rad2Deg;
            _arrowImage.transform.rotation = Quaternion.Euler(0, 0, rotationAngle + Constants.AdditionRotationAngle);
        }
    }
}