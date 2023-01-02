using UnityEngine;
using Zenject;

namespace Planet_IO.Camera
{
    public class PlayerCamera : MonoBehaviour
    {
        private Player _player;
        private Vector3 offset;

        [Inject]
        private void Construct(Player player)
        {
            _player = player;
        }

        private void Start()
        {
            offset = transform.position;
        }

        private void LateUpdate()
        {
            transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y, offset.z);
        }
    }
}