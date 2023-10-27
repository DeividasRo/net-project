using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> _randomObjectCount = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> _maxSpawnPositions = new NetworkVariable<Vector2>(new Vector2(3, 3), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Initialize()
    {
        if (!IsOwner) return;
        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
    }

    public void SetPlayerReady()
    {
        if (!IsOwner) return;
        isReady.Value = true;
        Invoke(nameof(SetPlayerReadyServerRpc), 2f / NetworkManager.Singleton.NetworkTickSystem.TickRate);
    }

    public void PrepareGame()
    {
        if (IsServer)
        {
            Debug.Log("Preparing the game...");
            _randomObjectCount.Value = UnityEngine.Random.Range(50, 1000);
            Invoke(nameof(StartGameClientRpc), 3);
            SetGameStateServerRpc(GameState.Started);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc()
    {
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (!networkClient.PlayerObject.GetComponent<PlayerNetwork>().isReady.Value)
                return;
        }
        if (NetworkManager.Singleton.ConnectedClientsList.Count != NetworkManager.Singleton.GetComponent<Relay>().maxConnections)
        {
            return;
        }
        SetGameStateServerRpc(GameState.Preparing);
        PrepareGame();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGameStateServerRpc(GameState state)
    {
        gameState.Value = state;
        UpdateGameStateClientRpc(state);
    }

    [ClientRpc]
    private void UpdateGameStateClientRpc(GameState state)
    {
        UIGameManager.Instance.ReadyButtonVisibilityByState(state);
    }


    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log(gameState.Value);
        GameManager.Instance.StartGame(_randomObjectCount.Value, _maxSpawnPositions.Value);
        Debug.Log("Game started!");
    }


}
