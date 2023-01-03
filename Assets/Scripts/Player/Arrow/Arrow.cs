using DG.Tweening;
using Planet_IO.Utils;
using RDTools.AutoAttach;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Planet_IO.Arrow
{
    public class Arrow : MonoBehaviour
    {
        [SerializeField, Attach] private Image arrowImage;

        private Player _player;
        private PlayerMovement _playerMovement;
        private UnityEngine.Camera _playerCamera;

        [Inject]
        private void Construct(PlayerMovement playerMovement, UnityEngine.Camera playerCamera)
        {
            _player = playerMovement.Player;
            _playerMovement = playerMovement;
            _playerCamera = playerCamera;
        }

        private void LateUpdate()
        {
            arrowImage.transform
                .DOMove(_playerCamera
                    .WorldToScreenPoint(_player.transform.position + _player.transform.right *
                                                 (_player.Capacity * Constants.ArrowCapacityMultiplayer)), Time.deltaTime);
            
            var rotationAngle = Mathf.Atan2(_playerMovement.Direction.y, _playerMovement.Direction.x) * Mathf.Rad2Deg;
            arrowImage.transform.rotation = Quaternion.Euler(0, 0, rotationAngle + Constants.AdditionRotationAngle);
            //arrowImage.transform.localScale = arrowImage.transform.localScale * _player.Capacity;
        }
    }
}