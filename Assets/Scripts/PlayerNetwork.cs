using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> _randomNumber = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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
        if (NetworkManager.Singleton.ConnectedClientsList.Count == NetworkManager.Singleton.GetComponent<Relay>().maxConnections)
            PrepareGame();
    }

    private void PrepareGame()
    {
        if (IsServer && IsLocalPlayer)
        {
            _randomNumber.Value = Random.Range(50, 1000);
            Invoke("StartGameClientRpc", 3);
        }
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("Game started!");
        GameManager.Instance.StartGame(_randomNumber.Value);
    }
}
