using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using RDTools.AutoAttach;
using UnityEngine;
using Zenject;

namespace Planet_IO.Camera
{
    public class PlayerCamera : MonoBehaviour
    {
        [field:SerializeField, Attach] public UnityEngine.Camera Camera;
        
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

        public async UniTaskVoid UpdateFOV(float value)
        {
            var increasedValue = Camera.orthographicSize + value;
            while (Camera.orthographicSize < increasedValue)
            {
                Camera.orthographicSize = Mathf.Lerp(Camera.orthographicSize, Camera.orthographicSize + value, Time.deltaTime);
                await UniTask.Yield();
            }
        }
    }
}