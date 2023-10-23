using System.Collections;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [HideInInspector]
    public float maxPosX, maxPosZ;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _objectCount;
    [Range(0, 2)]
    [SerializeField] private float _objectSize;
    private int _objectsSpawnedCount = 0;

    public void StartSpawning()
    {
        maxPosX = 3;
        maxPosZ = 3;
        StartCoroutine("SpawnObjectsOverTime");
    }

    private IEnumerator SpawnObjectsOverTime()
    {
        while (_objectsSpawnedCount < _objectCount)
        {
            yield return new WaitForSeconds(0.04f);
            SpawnObject();
            Debug.Log(_objectsSpawnedCount);
            _objectsSpawnedCount++;
        }
    }

    private void SpawnObject()
    {
        GameObject prefabInstance = Instantiate(_prefab, new Vector3(Random.Range(-maxPosX, maxPosX), 8, Random.Range(-maxPosZ, maxPosZ)), Quaternion.identity);
        prefabInstance.transform.localScale = new Vector3(_objectSize, _objectSize, _objectSize);
    }

}
