using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance { get { return _instance; } }

    [SerializeField]
    private ObjectSpawner _objectSpawner;


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public void StartGame(int objectCount)
    {
        _objectSpawner._objectCount = objectCount;
        Debug.Log(_objectSpawner._objectCount);
        _objectSpawner.StartSpawning();
    }
}
