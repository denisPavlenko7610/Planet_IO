using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CometPool : MonoBehaviour
{
    [SerializeField] private GameObject _container;
    [SerializeField] private int _capacity;

    private List<GameObject> _pools = new List<GameObject>();

    protected void Initialize(GameObject prefabs)
    {
        for (int i = 0; i < _capacity; i++)
        {
            GameObject spawned = Instantiate(prefabs, _container.transform);
            spawned.SetActive(false);
            _pools.Add(spawned);
        }
    }
    protected bool TryGetObject(out GameObject result)
    {
        result = _pools.FirstOrDefault(p => p.activeSelf == false);
        return result != null;
    }
}
