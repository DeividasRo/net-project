using System.Collections;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Unity.VisualScripting;

public class ObjectSpawner : NetworkSingleton<ObjectSpawner>
{
    [SerializeField]
    private GameObject _prefab;
    public NetworkVariable<int> objectMeshId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> objectColorId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private List<NetworkObject> _networkObjectsSpawned { get; set; }
    private float _objectSize;
    private int _objectCount;
    private float _spawnFrequency;
    private float _maxPosX, _maxPosZ;
    private int _objectsSpawnedCount = 0;

    public override void OnNetworkSpawn()
    {
        _networkObjectsSpawned = new List<NetworkObject>();
        NetworkObjectPool.Singleton.InitializePool();
    }

    public void StartSpawning(int objectCount, float objectSize, float spawnFrequency, Vector2 maxSpawnPositions)
    {
        ;
        objectMeshId.Value = Random.Range(0, 2);
        objectColorId.Value = Random.Range(0, 3);
        Debug.Log($"{objectMeshId.Value}, {objectColorId.Value}");
        _objectSize = objectSize;
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
        NetworkObject obj = NetworkObjectPool.Singleton.GetNetworkObject(_prefab, new Vector3(Random.Range(-_maxPosX, _maxPosX), 15, Random.Range(-_maxPosZ, _maxPosZ)), Quaternion.identity);

        obj.GetComponent<Transform>().localScale = new Vector3(_objectSize, _objectSize, _objectSize);
        if (!obj.IsSpawned)
        {
            obj.Spawn();
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
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.None;
            NetworkObjectPool.Singleton.ReturnNetworkObject(obj, _prefab);
            if (obj.IsSpawned)
                obj.Despawn();
        }
        _networkObjectsSpawned.Clear();
        _objectsSpawnedCount = 0;
    }

}
