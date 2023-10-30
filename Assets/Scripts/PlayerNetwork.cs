using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> objectCount = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> spawnFrequency = new NetworkVariable<float>(0.1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> maxSpawnPositions = new NetworkVariable<Vector2>(new Vector2(2.8f, 2.8f), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<ulong> winnerId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Dictionary<ulong, int> guessesDict = new Dictionary<ulong, int>();
    private ulong _winnerId = 0;
    private int _guessTime = 5;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner) return;
        if (IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
    }

    private IEnumerator StartGameProcess()
    {
        if (IsServer)
        {
            Debug.Log("Preparing the game...");
            int secondsToPrepare = 3;
            objectCount.Value = UnityEngine.Random.Range(50, 90);
            winnerId.Value = 0;
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
            Invoke(nameof(EvaluateGuessesServerRpc), 2f / NetworkManager.Singleton.NetworkTickSystem.TickRate);
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
        if (!IsOwner) return;
        isReady.Value = false;
        Invoke(nameof(ResetPlayerServerRpc), 2f / NetworkManager.Singleton.NetworkTickSystem.TickRate);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ResetPlayerServerRpc()
    {
        isReady.Value = false;
        SetGameStateServerRpc(GameState.Waiting);
    }

    public void SetPlayerReady()
    {
        if (!IsOwner) return;
        isReady.Value = true;
        UIGameManager.Instance.UpdateReadyButtonColorByReadyState(isReady.Value);
        Invoke(nameof(SetPlayerReadyServerRpc), 2f / NetworkManager.Singleton.NetworkTickSystem.TickRate);
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
    }

    [ServerRpc(RequireOwnership = false)]
    private void EvaluateGuessesServerRpc()
    {
        int closestGuess = 0;
        int closestGuessDistance = 9999;
        foreach (KeyValuePair<ulong, int> guess in guessesDict)
        {

            if (closestGuessDistance > Math.Abs(objectCount.Value - guess.Value))
            {
                _winnerId = guess.Key;
                closestGuess = guess.Value;
                closestGuessDistance = objectCount.Value - guess.Value;
            }
            Debug.Log($"Key: {guess.Key}, Value:{guess.Value}");
        }
        Debug.Log($"Winner key: {_winnerId}, value:{closestGuess}");
        winnerId.Value = _winnerId;
        SyncWinnerIdsClientRpc(_winnerId);
        SetGameStateServerRpc(GameState.GameEnded);
    }

    [ClientRpc]
    private void SyncWinnerIdsClientRpc(ulong id)
    {
        _winnerId = id;
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
        UpdateGameStateClientRpc(state);
    }

    [ClientRpc]
    private void UpdateGameStateClientRpc(GameState state)
    {
        if (state == GameState.GameEnded)
        {
            Debug.Log("[GameEnded]");
            UIGameManager.Instance.SetWinnerText(_winnerId, objectCount.Value);
            UIGameManager.Instance.SetWinnerTextActive(true);
            Invoke(nameof(ResetPlayer), 5f);

        }
        else if (state == GameState.GuessingEnded)
        {
            Debug.Log("[GuessingEnded]");
            UIGameManager.Instance.SetGuessInputActive(false);
            UIGameManager.Instance.SetCountdownTextActive(false);
            SetPlayerGuessServerRpc(NetworkManager.Singleton.LocalClientId, UIGameManager.Instance.GetGuessInputText());
        }
        else if (state == GameState.Guessing)
        {
            Debug.Log("[Guessing]");
            UIGameManager.Instance.SetGuessInputActive(true);
            UIGameManager.Instance.SetCountdownTextActive(true);
            StartCoroutine(ObjectSpawner.Instance.FreezeAllObjectsWithDelay(3f));
            StartCoroutine(StartCountdown(_guessTime));
        }
        else if (state == GameState.Started)
        {
            Debug.Log("[Started]");
            UIGameManager.Instance.SetCountdownTextActive(false);
        }
        else if (state == GameState.Preparing)
        {
            Debug.Log("[Preparing]");
            UIGameManager.Instance.SetReadyButtonActive(false);
            UIGameManager.Instance.SetCountdownTextActive(true);
            StartCoroutine(StartCountdown(3));
        }
        else if (state == GameState.Waiting)
        {
            Debug.Log("[Waiting]");
            Debug.Log(isReady.Value);
            UIGameManager.Instance.SetReadyButtonActive(true);
            UIGameManager.Instance.SetWinnerTextActive(false);
            UIGameManager.Instance.ResetGuessInputText();
            UIGameManager.Instance.UpdateReadyButtonColorByReadyState(isReady.Value);
        }
    }

}
