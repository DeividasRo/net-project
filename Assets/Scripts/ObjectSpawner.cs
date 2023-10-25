using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class ObjectSpawner : NetworkSingleton<ObjectSpawner>
{
    private int _objectCount;
    private float _maxPosX, _maxPosZ;
    [SerializeField] private GameObject _prefab;
    [Range(0, 2)][SerializeField] private float _objectSize;
    private List<Rigidbody> spawnedRigidbodies;
    private int _objectsSpawnedCount = 0;

    public void StartSpawning(int objectCount, Vector2 maxSpawnPositions)
    {
        _objectCount = objectCount;
        _maxPosX = maxSpawnPositions.x;
        _maxPosZ = maxSpawnPositions.y;
        spawnedRigidbodies = new List<Rigidbody>();
        StartCoroutine("SpawnObjectsOverTime");
    }

    private IEnumerator SpawnObjectsOverTime()
    {
        while (_objectsSpawnedCount < _objectCount)
        {
            yield return new WaitForSeconds(0.04f);
            SpawnObject();
            _objectsSpawnedCount++;
        }
        if (_objectsSpawnedCount >= _objectCount)
        {
            Invoke("FreezeAllObjects", 8f);
        }
    }

    private void FreezeAllObjects()
    {
        foreach (Rigidbody rb in spawnedRigidbodies)
        {
            rb.constraints = RigidbodyConstraints.FreezePosition;
        }
    }

    private void SpawnObject()
    {
        if (!IsServer) return;

        GameObject prefabInstance = Instantiate(_prefab, new Vector3(Random.Range(-_maxPosX, _maxPosX), 8, Random.Range(-_maxPosZ, _maxPosZ)), Quaternion.identity);
        prefabInstance.transform.localScale = new Vector3(_objectSize, _objectSize, _objectSize);

        spawnedRigidbodies.Add(prefabInstance.GetComponent<Rigidbody>());

        prefabInstance.GetComponent<NetworkObject>().Spawn();
    }

}
