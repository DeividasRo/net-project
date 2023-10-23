using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
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
            StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        GameManager.Instance.StartGame();
    }
}
