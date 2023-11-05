using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> objectCount = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> spawnFrequency = new NetworkVariable<float>(0.1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> maxSpawnPositions = new NetworkVariable<Vector2>(new Vector2(2.8f, 2.8f), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Dictionary<ulong, int> guessesDict = new Dictionary<ulong, int>();
    public Dictionary<ulong, int> sortedResultsDict = new Dictionary<ulong, int>();
    private int _guessTime = 10;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        gameState.OnValueChanged += OnGameStateValueChanged;
        isReady.OnValueChanged += OnIsReadyValueChanged;
    }

    private void OnIsReadyValueChanged(bool prev, bool curr)
    {
        // Network variable synced across clients, not owned by player object
        Debug.Log($"{prev}, {curr}");
        if (curr == false)
        {
            guessesDict.Clear();
            GameResetServerRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        gameState.OnValueChanged -= OnGameStateValueChanged;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
    }

    private void OnGameStateValueChanged(GameState prev, GameState curr)
    {
        //Debug.Log($"{prev}, {curr}");
        if (curr == GameState.GameEnded)
        {
            Debug.Log("[GameEnded]");
            UIGameManager.Instance.SetCorrectAnswerText(objectCount.Value);
            UIGameManager.Instance.SetCorrectAnswerTextActive(true);
            UIGameManager.Instance.SetRoundScoresText(sortedResultsDict);
            UIGameManager.Instance.SetRoundScoresTextActive(true);
            ResetPlayerServerRpc();
        }
        else if (curr == GameState.GuessingEnded)
        {
            Debug.Log("[GuessingEnded]");
            UIGameManager.Instance.SetGuessInputActive(false);
            UIGameManager.Instance.SetCountdownTextActive(false);
            SetPlayerGuessServerRpc(NetworkManager.Singleton.LocalClientId, UIGameManager.Instance.GetGuessInputText());
        }
        else if (curr == GameState.Guessing)
        {
            Debug.Log("[Guessing]");
            UIGameManager.Instance.SetGuessInputActive(true);
            UIGameManager.Instance.SetCountdownTextActive(true);
            StartCoroutine(ObjectSpawner.Instance.FreezeAllObjectsWithDelay(3f));
            StartCoroutine(StartCountdown(_guessTime));
        }
        else if (curr == GameState.Started)
        {
            Debug.Log("[Started]");
            UIGameManager.Instance.SetCountdownTextActive(false);
        }
        else if (curr == GameState.Preparing)
        {
            Debug.Log("[Preparing]");
            UIGameManager.Instance.SetReadyButtonActive(false);
            UIGameManager.Instance.SetCountdownTextActive(true);
            StartCoroutine(StartCountdown(3));
        }
        else if (curr == GameState.Waiting)
        {
            Debug.Log("[Waiting]");
            UIGameManager.Instance.SetReadyButtonActive(true);
            UIGameManager.Instance.SetCorrectAnswerTextActive(false);
            UIGameManager.Instance.ResetGuessInputText();
            UIGameManager.Instance.UpdateReadyButtonColorByReadyState(false);
            UIGameManager.Instance.SetRoundScoresTextActive(false);
        }
    }

    private IEnumerator StartGameProcess()
    {
        if (IsServer)
        {
            Debug.Log("Preparing the game...");
            int secondsToPrepare = 3;
            objectCount.Value = UnityEngine.Random.Range(50, 300);
            spawnFrequency.Value = 0.03f;
            while (secondsToPrepare > 0)
            {
                secondsToPrepare--;
                yield return new WaitForSeconds(1f);
            }

            SetGameStateServerRpc(GameState.Started);
            StartGameServerRpc();


            yield return new WaitForSeconds(objectCount.Value * spawnFrequency.Value + 2);

            SetGameStateServerRpc(GameState.Guessing);

            yield return new WaitForSeconds(_guessTime);

            ObjectSpawner.Instance.DestroyAllObjects();

            SetGameStateServerRpc(GameState.GuessingEnded);
        }
    }

    private IEnumerator StartCountdown(int countdownTime)
    {
        for (int i = countdownTime; i > 0; i--)
        {
            UIGameManager.Instance.SetCountdownText(i.ToString());
            yield return new WaitForSeconds(1f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        bool isClientReady = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetwork>().isReady.Value;
        if (!isClientReady) return;
        Debug.Log($"isReady: {isReady.Value}, clientId: {clientId}");
        NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetwork>().isReady.Value = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void GameResetServerRpc()
    {
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            Debug.Log($"GR {networkClient.ClientId} - {networkClient.PlayerObject.GetComponent<PlayerNetwork>().isReady.Value}");
            if (networkClient.PlayerObject.GetComponent<PlayerNetwork>().isReady.Value) return;
        }
        StartCoroutine(ResetGame(8));
    }

    private IEnumerator ResetGame(int delay = 0)
    {
        if (IsServer)
        {
            while (delay > 0)
            {
                delay--;
                yield return new WaitForSeconds(1f);
            }
            SetGameStateServerRpc(GameState.Waiting);
        }
    }

    public void SetPlayerReady()
    {
        if (!IsOwner) return;
        UIGameManager.Instance.UpdateReadyButtonColorByReadyState(true);
        SetPlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc()
    {
        isReady.Value = true;
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
            if (!networkClient.PlayerObject.GetComponent<PlayerNetwork>().isReady.Value) return;
        EvaluateReadinessServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void EvaluateReadinessServerRpc()
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count != NetworkManager.Singleton.GetComponent<Relay>().maxConnections)
        {
            return;
        }
        SetGameStateServerRpc(GameState.Preparing);
        StartCoroutine(nameof(StartGameProcess));
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerGuessServerRpc(ulong clientId, string guessString)
    {
        if (guessString == String.Empty)
        {
            guessesDict[clientId] = 0;
        }
        else
        {
            guessesDict[clientId] = Int16.Parse(guessString);
        }
        Invoke(nameof(EvaluateGuessesServerRpc), 2f / NetworkManager.Singleton.NetworkTickSystem.TickRate);
    }

    [ServerRpc(RequireOwnership = false)]
    private void EvaluateGuessesServerRpc()
    {
        if (guessesDict.Count < NetworkManager.Singleton.ConnectedClientsList.Count) return;
        Dictionary<ulong, int> resultsDict = new Dictionary<ulong, int>();
        foreach (KeyValuePair<ulong, int> guess in guessesDict)
        {
            resultsDict[guess.Key] = Math.Abs(objectCount.Value - guess.Value);
            Debug.Log($"Key: {guess.Key}, Value:{guess.Value}");
        }
        sortedResultsDict = resultsDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        ulong[] clientIds = new ulong[sortedResultsDict.Count];
        int[] values = new int[sortedResultsDict.Count];
        int idx = 0;
        foreach (KeyValuePair<ulong, int> result in sortedResultsDict)
        {
            clientIds[idx] = result.Key;
            values[idx] = result.Value;
            idx++;
        }

        SyncResultsDictClientRpc(clientIds, values);
        SetGameStateServerRpc(GameState.GameEnded);
    }

    [ClientRpc]
    private void SyncResultsDictClientRpc(ulong[] clientIds, int[] values)
    {
        sortedResultsDict.Clear();
        for (int i = 0; i < clientIds.Length; i++)
            sortedResultsDict.Add(clientIds[i], values[i]);
    }


    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        ObjectSpawner.Instance.StartSpawning(objectCount.Value, spawnFrequency.Value, maxSpawnPositions.Value);
        Debug.Log("Game started!");
        Debug.Log($"Object count: {objectCount.Value}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGameStateServerRpc(GameState state)
    {
        if (gameState.Value != state)
            gameState.Value = state;
    }

}
