using UnityEngine;

namespace Planet_IO.Camera
{
	[RequireComponent(typeof(UnityEngine.Camera))]
	[DefaultExecutionOrder(1000)]
	public sealed class PlayerCamera : MonoBehaviour
	{
		[SerializeField, Min(0.01f)] private float _zoomSmoothTime = 0.2f;
		[SerializeField, Min(0f)] private float _zoomPerCapacityUnit = 5f;
		[SerializeField, Min(1f)] private float _maximumOrthographicSize = 30f;

		public UnityEngine.Camera Camera { get; private set; }

		private Player _player;
		private float _zoomVelocity;
		private float _cameraDepth;
		private float _baseOrthographicSize;
		private float _baseCapacity;
		private float _targetOrthographicSize;

		private void Awake()
		{
			Camera = GetComponent<UnityEngine.Camera>();
			if (Camera == null)
			{
				Debug.LogError(
					$"{nameof(PlayerCamera)} requires a Camera component on the same GameObject.",
					this);
				enabled = false;
				return;
			}

			_cameraDepth = transform.position.z;
			_baseOrthographicSize = Camera.orthographicSize;
			_targetOrthographicSize = _baseOrthographicSize;
		}

		private void LateUpdate()
		{
			if (!TryBindLocalPlayer())
			{
				return;
			}

			Vector3 targetPosition = new Vector3(
				_player.transform.position.x,
				_player.transform.position.y,
				_cameraDepth
			);

			transform.position = targetPosition;

			Camera.orthographicSize = Mathf.SmoothDamp(
				Camera.orthographicSize,
				_targetOrthographicSize,
				ref _zoomVelocity,
				_zoomSmoothTime
			);
		}

		private void OnDestroy()
		{
			UnbindPlayer();
		}

		private bool TryBindLocalPlayer()
		{
			if (_player != null && _player.IsSpawned && _player.IsOwner)
			{
				return true;
			}

			UnbindPlayer();

			foreach (Player candidate in FindObjectsByType<Player>(FindObjectsInactive.Exclude))
			{
				if (!candidate.IsSpawned || !candidate.IsOwner)
				{
					continue;
				}

				_player = candidate;
				_baseCapacity = Mathf.Max(candidate.Capacity, 0.01f);
				_player.CapacityChanged += OnCapacityChanged;
				OnCapacityChanged(candidate.Capacity);
				return true;
			}

			return false;
		}

		private void UnbindPlayer()
		{
			if (_player == null)
			{
				return;
			}

			_player.CapacityChanged -= OnCapacityChanged;
			_player = null;
		}

		private void OnCapacityChanged(float capacity)
		{
			float additionalZoom = Mathf.Max(0f, capacity - _baseCapacity) * _zoomPerCapacityUnit;
			_targetOrthographicSize = Mathf.Clamp(
				_baseOrthographicSize + additionalZoom,
				_baseOrthographicSize,
				_maximumOrthographicSize
			);
		}
	}
}
