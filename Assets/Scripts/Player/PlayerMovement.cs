using Dythervin.AutoAttach;
using UnityEngine;

namespace PlanetIO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Attach(Attach.Scene)] private Camera mainCamera;
        [SerializeField, Attach] private Rigidbody2D rigidbody2D;
        [SerializeField] private float speed = 3f;
        private Vector2 mousePosition;

        void Update()
        {
            mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        }

        private void FixedUpdate()
        {
            rigidbody2D.velocity = mousePosition.normalized * speed;
        }
    }
}