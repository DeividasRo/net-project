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
    public NetworkList<ulong> readyClientIds = new NetworkList<ulong>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Dictionary<ulong, int> guessesDict = new Dictionary<ulong, int>();
    public Dictionary<ulong, int> sortedResultsDict = new Dictionary<ulong, int>();
    private int _guessTime = 5;
    private bool _isReady = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        gameState.OnValueChanged += OnGameStateValueChanged;
        readyClientIds.OnListChanged += OnReadyClientIdsValueChanged;
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
        Debug.Log(gameState.Value);
        if (curr == GameState.GameEnded)
        {
            Debug.Log("[GameEnded]");
            UIGameManager.Instance.SetCorrectAnswerText(objectCount.Value);
            UIGameManager.Instance.SetCorrectAnswerTextActive(true);
            UIGameManager.Instance.SetRoundScoresText(sortedResultsDict);
            UIGameManager.Instance.SetRoundScoresTextActive(true);
            Invoke(nameof(ResetPlayer), 5f);
        }
        else if (curr == GameState.GuessingEnded)
        {
            Debug.Log("[GuessingEnded]");
            Debug.Log($"{_isReady} - {NetworkManager.Singleton.LocalClientId}");
            UIGameManager.Instance.SetGuessInputActive(false);
            UIGameManager.Instance.SetCountdownTextActive(false);
            SetPlayerGuessServerRpc(NetworkManager.Singleton.LocalClientId, UIGameManager.Instance.GetGuessInputText());
        }
        else if (curr == GameState.Guessing)
        {
            Debug.Log("[Guessing]");
            Debug.Log($"{_isReady} - {NetworkManager.Singleton.LocalClientId}");
            UIGameManager.Instance.SetGuessInputActive(true);
            UIGameManager.Instance.SetCountdownTextActive(true);
            StartCoroutine(ObjectSpawner.Instance.FreezeAllObjectsWithDelay(3f));
            StartCoroutine(StartCountdown(_guessTime));
        }
        else if (curr == GameState.Started)
        {
            Debug.Log("[Started]");
            Debug.Log($"{_isReady} - {NetworkManager.Singleton.LocalClientId}");
            UIGameManager.Instance.SetCountdownTextActive(false);
        }
        else if (curr == GameState.Preparing)
        {
            Debug.Log("[Preparing]");
            Debug.Log($"{_isReady} - {NetworkManager.Singleton.LocalClientId}");
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
            UIGameManager.Instance.UpdateReadyButtonColorByReadyState(_isReady);
            UIGameManager.Instance.SetRoundScoresTextActive(false);
        }
    }

    private void OnReadyClientIdsValueChanged(NetworkListEvent<ulong> changeEvent)
    {
        Debug.Log(changeEvent.PreviousValue);
        Debug.Log(readyClientIds.Count);
    }

    private IEnumerator StartGameProcess()
    {
        if (IsServer)
        {
            Debug.Log("Preparing the game...");
            int secondsToPrepare = 3;
            objectCount.Value = UnityEngine.Random.Range(50, 90);
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

    private void ResetPlayer()
    {
        Debug.Log($"{_isReady} - {NetworkManager.Singleton.LocalClientId}");
        if (_isReady == false) return;
        _isReady = false;
        guessesDict.Clear();
        ResetPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetPlayerServerRpc(ulong clientId)
    {
        readyClientIds.Remove(clientId);
        Debug.Log(readyClientIds.Count);
        if (readyClientIds.Count == 0)
            SetGameStateServerRpc(GameState.Waiting);
    }

    public void SetPlayerReady()
    {
        if (!IsOwner) return;
        if (_isReady) return;
        _isReady = true;
        UIGameManager.Instance.UpdateReadyButtonColorByReadyState(_isReady);
        SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    private void SetPlayerReadyServerRpc(ulong clientId)
    {
        readyClientIds.Add(clientId);
        EvaluateReadinessServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void EvaluateReadinessServerRpc()
    {
        Debug.Log(readyClientIds.Count);
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (!readyClientIds.Contains(networkClient.ClientId)) return;
        }
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
        if (sortedResultsDict.Count != 0) return;
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
        gameState.Value = state;
    }

}
