using System.Collections;
using UnityEngine;

public class Comet : MonoBehaviour
{
    [HideInInspector] public static Comet Instance { get; private set; }
    
    [SerializeField] private int _maxTimeLifeComet = 15;
    [SerializeField] private int _minTimeLifeComet = 5;

    private int _currentTimeLifeComet = 1;

    private void Awake()
    {
        Instance = this;
    }
    private void OnDisable() => StopCoroutine(DeathComet());
    
    private void OnEnable() => StartCoroutine(DeathComet());


    IEnumerator DeathComet()
    {
        _currentTimeLifeComet = RandomLifeTime();
        yield return new WaitForSeconds(_currentTimeLifeComet);
        gameObject.SetActive(false);
    }
    private int RandomLifeTime()
    {
        return Random.Range(_minTimeLifeComet, _maxTimeLifeComet);
    }
}
