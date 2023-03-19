using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Planet_IO.Camera
{
    public class PlayerCamera : MonoBehaviour
    {
        public UnityEngine.Camera Camera { get; private set; }
        
        private Player _player;
        private Vector3 _offset;

        private void Start()
        {
            Camera = UnityEngine.Camera.main;
            _player = FindObjectOfType<Player>();
            _offset = transform.position;
        }

        private void LateUpdate()
        {
            if (_player)
            {
                transform.position = new Vector3(_player.transform.position.x, _player.transform.position.y, _offset.z);
            }
        }

        public async Task UpdateFOV(float value)
        {
            var increasedValue = Camera.orthographicSize + value;

            if (value > 0)
            {
                while (Camera.orthographicSize < increasedValue)
                {
                    await UpdateCameraSize(value);
                }
            }
            else
            {
                while (Camera.orthographicSize > increasedValue)
                {
                    await UpdateCameraSize(value);
                }
            }
        }

        private async Task UpdateCameraSize(float value)
        {
            Camera.orthographicSize =
                Mathf.Lerp(Camera.orthographicSize, Camera.orthographicSize + value, Time.deltaTime);

            await Task.Yield();
        }
    }
}