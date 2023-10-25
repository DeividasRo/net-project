using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> _randomObjectCount = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> _maxSpawnPositions = new NetworkVariable<Vector2>(new Vector2(3, 3), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Initialize()
    {
        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
    }

    public void ChangeReadyState()
    {
        if (!IsOwner) return;
        isReady.Value = !isReady.Value;
        Invoke("CheckReadinessServerRpc", 2f / NetworkManager.Singleton.NetworkTickSystem.TickRate);
    }

    [ServerRpc]
    public void CheckReadinessServerRpc()
    {
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            Debug.Log(networkClient.ClientId);
            Debug.Log(networkClient.PlayerObject.GetComponent<PlayerNetwork>().isReady.Value);
            if (!networkClient.PlayerObject.GetComponent<PlayerNetwork>().isReady.Value)
                return;
        }
        if (NetworkManager.Singleton.ConnectedClientsList.Count != NetworkManager.Singleton.GetComponent<Relay>().maxConnections)
        {
            return;
        }
        PrepareGame();
    }

    public void PrepareGame()
    {
        if (IsServer)
        {
            Debug.Log("Preparing the game...");
            _randomObjectCount.Value = Random.Range(50, 1000);
            Invoke("StartGameClientRpc", 3);
        }
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {

        Debug.Log("Game started!");
        GameManager.Instance.StartGame(_randomObjectCount.Value, _maxSpawnPositions.Value);
    }



}
