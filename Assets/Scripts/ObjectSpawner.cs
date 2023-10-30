using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ObjectSpawner : Singleton<ObjectSpawner>
{
    [SerializeField]
    private GameObject _prefab;
    [Range(0, 2)]
    [SerializeField]
    private float _objectSize;
    private List<NetworkObject> _networkObjectsSpawned { get; set; }
    private int _objectCount;
    private float _spawnFrequency;
    private float _maxPosX, _maxPosZ;
    private int _objectsSpawnedCount = 0;

    private void Awake()
    {
        _networkObjectsSpawned = new List<NetworkObject>();
        NetworkManager.Singleton.OnServerStarted += InitializeObjectPool;
    }

    private void InitializeObjectPool()
    {
        Debug.Log("Object pool initialized.");
        NetworkObjectPool.Singleton.InitializePool();
    }

    public void StartSpawning(int objectCount, float spawnFrequency, Vector2 maxSpawnPositions)
    {

        _objectCount = objectCount;
        _spawnFrequency = spawnFrequency;
        _maxPosX = maxSpawnPositions.x;
        _maxPosZ = maxSpawnPositions.y;
        StartCoroutine("SpawnObjectsOverTime");
    }

    private IEnumerator SpawnObjectsOverTime()
    {
        while (_objectsSpawnedCount < _objectCount)
        {
            yield return new WaitForSeconds(_spawnFrequency);
            SpawnObject();
            _objectsSpawnedCount++;
        }
    }

    private void SpawnObject()
    {
        NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(_prefab, new Vector3(Random.Range(-_maxPosX, _maxPosX), 8, Random.Range(-_maxPosZ, _maxPosZ)), Quaternion.identity);

        obj.GetComponent<Transform>().localScale = new Vector3(_objectSize, _objectSize, _objectSize);


        if (!obj.IsSpawned)
        {
            obj.Spawn(destroyWithScene: true);
            _networkObjectsSpawned.Add(obj);
        }
    }

    public IEnumerator FreezeAllObjectsWithDelay(float delay = 3f)
    {
        if (_networkObjectsSpawned.Count > 0)
        {
            yield return new WaitForSeconds(delay);
            FreezeAllObjects();
        }
    }

    public void FreezeAllObjects()
    {
        foreach (NetworkObject obj in _networkObjectsSpawned)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePosition;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    public void DestroyAllObjects()
    {
        foreach (NetworkObject obj in _networkObjectsSpawned)
        {
            NetworkObjectPool.Singleton.ReturnNetworkObject(obj, _prefab);
            if (obj.IsSpawned)
                obj.Despawn();
        }
    }

}
