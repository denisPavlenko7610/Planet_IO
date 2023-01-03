using Planet_IO.Camera;
using Planet_IO.Utils;
using RDTools;
using RDTools.AutoAttach;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Planet_IO.Arrow
{
    public class Arrow : MonoBehaviour
    {
        [SerializeField, Attach] private Image _arrowImage;
        [SerializeField, ReadOnly] private PlayerCamera _playerCamera;

        private Player _player;
        private PlayerMovement _playerMovement;

        [Inject]
        private void Construct(PlayerMovement playerMovement, PlayerCamera playerCamera)
        {
            _player = playerMovement.Player;
            _playerMovement = playerMovement;
            _playerCamera = playerCamera;
        }

        private void LateUpdate()
        {
            SetArrowPosition();
            SetArrowRotation();
        }

        private void SetArrowPosition()
        {
            _arrowImage.transform.position = _playerCamera.Camera.WorldToScreenPoint(_player.transform.position + _player.transform.right *
                (_player.Capacity * Constants.ArrowPositionMult));
        }

        private void SetArrowRotation()
        {
            var rotationAngle = Mathf.Atan2(_playerMovement.Direction.y, _playerMovement.Direction.x) * Mathf.Rad2Deg;
            _arrowImage.transform.rotation = Quaternion.Euler(0, 0, rotationAngle + Constants.AdditionRotationAngle);
        }
    }
}