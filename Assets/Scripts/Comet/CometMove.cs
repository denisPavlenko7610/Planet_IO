using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody2D))]
public class CometMove : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private float _maxSpeed = 0.01f;
    [SerializeField] private float _minSpeed = 0.03f;
    
    private float _currentSpeed = 0.004f;
    private Vector2 _direaction;

    private void OnEnable() => Move();
   
    private void OnDisable()
    {
        _currentSpeed = RandomSpeed();
        _direaction = DireactionMove(_direaction);
    }

    private Vector2 DireactionMove(Vector2 Direaction)
    {
        Direaction.x = Random.Range(-1f, 1f);
        Direaction.y = Random.Range(-1f, 1f);
        return Direaction;
    }
    private float RandomSpeed()
    {
        return Random.Range(_minSpeed, _maxSpeed);
    }
    private void Move()
    {
        _rigidbody2D.AddForce(_currentSpeed * _direaction);
    }
}
