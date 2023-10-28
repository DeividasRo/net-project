using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> _objectCount = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _spawnFrequency = new NetworkVariable<float>(0.1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> _maxSpawnPositions = new NetworkVariable<Vector2>(new Vector2(3, 3), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> countGuess = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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

    private IEnumerator PrepareGame()
    {
        if (IsServer)
        {
            Debug.Log("Preparing the game...");
            int secondsToPrepare = 3;
            _objectCount.Value = UnityEngine.Random.Range(80, 1000);
            _spawnFrequency.Value = 0.03f;
            while (secondsToPrepare > 0)
            {
                secondsToPrepare--;
                yield return new WaitForSeconds(1f);
            }
            SetGameStateServerRpc(GameState.Started);
            StartGameServerRpc();

            float timeTilGuessing = _objectCount.Value * _spawnFrequency.Value + 2;
            while (timeTilGuessing > 0)
            {
                timeTilGuessing--;
                yield return new WaitForSeconds(1f);
            }
            SetGameStateServerRpc(GameState.Guessing);
        }
    }

    private IEnumerator StartGuessingStage()
    {
        Debug.Log("Guessing started");
        for (int i = 10; i > 0; i--)
        {
            UIGameManager.Instance.SetCountdownText(i.ToString());
            yield return new WaitForSeconds(1f);
        }
        SetGameStateServerRpc(GameState.Ended);
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
        StartCoroutine(nameof(PrepareGame));
    }


    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        ObjectSpawner.Instance.StartSpawning(_objectCount.Value, _spawnFrequency.Value, _maxSpawnPositions.Value);
        Debug.Log("Game started!");
        Debug.Log($"Object count: {_objectCount.Value}");
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
        if (state == GameState.Ended)
        {
            Debug.Log("Game ended");
            UIGameManager.Instance.SetCountdownActive(false);
            ObjectSpawner.Instance.DestroyAllObjects();

        }
        if (state == GameState.Guessing)
        {
            UIGameManager.Instance.SetCountdownActive(true);
            GameManager.Instance.StopAllObjects(3f);
            StartCoroutine(nameof(StartGuessingStage));
        }
        else if (state == GameState.Started)
        {
            UIGameManager.Instance.SetGuessInputActive(true);
        }
        else if (state == GameState.Preparing)
        {
            UIGameManager.Instance.SetReadyButtonActive(false);
        }
        else if (state == GameState.Waiting)
        {
            UIGameManager.Instance.SetReadyButtonActive(true);
            UIGameManager.Instance.SetGuessInputActive(false);
        }
    }
}
