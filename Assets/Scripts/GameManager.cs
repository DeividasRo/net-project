using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private ObjectSpawner _objectSpawner;

    public void StartGame(int objectCount, Vector2 maxSpawnPositions)
    {
        Debug.Log(objectCount);
        _objectSpawner.StartSpawning(objectCount, maxSpawnPositions);
    }
}
