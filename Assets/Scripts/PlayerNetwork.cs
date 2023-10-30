using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> _objectCount = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> _spawnFrequency = new NetworkVariable<float>(0.1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> _maxSpawnPositions = new NetworkVariable<Vector2>(new Vector2(2.8f, 2.8f), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public Dictionary<ulong, int> guessesDict = new Dictionary<ulong, int>();

    private int guessTime = 10;

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
            _objectCount.Value = UnityEngine.Random.Range(80, 100);
            _spawnFrequency.Value = 0.03f;
            while (secondsToPrepare > 0)
            {
                secondsToPrepare--;
                yield return new WaitForSeconds(1f);
            }

            SetGameStateServerRpc(GameState.Started);
            StartGameServerRpc();


            yield return new WaitForSeconds(_objectCount.Value * _spawnFrequency.Value + 2);

            SetGameStateServerRpc(GameState.Guessing);

            yield return new WaitForSeconds(guessTime);

            ObjectSpawner.Instance.DestroyAllObjects();

            SetGameStateServerRpc(GameState.Ended);
            Invoke(nameof(EvaluateGuessesServerRpc), 1f);
        }
    }

    private IEnumerator StartCountdown(int countdownTime)
    {
        Debug.Log("Guessing started");
        for (int i = countdownTime; i > 0; i--)
        {
            UIGameManager.Instance.SetCountdownText(i.ToString());
            yield return new WaitForSeconds(1f);
        }
    }

    public void SetPlayerReady()
    {
        if (!IsOwner) return;
        isReady.Value = true;
        UIGameManager.Instance.UpdateReadyButtonColor();
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
            Debug.Log(guessString);
            guessesDict[clientId] = Int16.Parse(guessString);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void EvaluateGuessesServerRpc()
    {
        ulong winnerId = 0;
        int closestGuess = 9999;
        foreach (KeyValuePair<ulong, int> guess in guessesDict)
        {
            if (closestGuess > Math.Abs(_objectCount.Value - guess.Value))
            {
                winnerId = guess.Key;
                closestGuess = guess.Value;
            }
            Debug.Log($"Key: {guess.Key}, Value:{guess.Value}");
        }
        Debug.Log($"Winner key: {winnerId}, value:{closestGuess}");
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
            Debug.Log("[Ended]");
            UIGameManager.Instance.SetGuessInputActive(false);
            UIGameManager.Instance.SetCountdownActive(false);
            SetPlayerGuessServerRpc(NetworkManager.Singleton.LocalClientId, UIGameManager.Instance.GetGuessInputText());
        }
        else if (state == GameState.Guessing)
        {
            Debug.Log("[Guessing]");
            UIGameManager.Instance.SetGuessInputActive(true);
            UIGameManager.Instance.SetCountdownActive(true);
            StartCoroutine(ObjectSpawner.Instance.FreezeAllObjectsWithDelay(3f));
            StartCoroutine(StartCountdown(guessTime));
        }
        else if (state == GameState.Started)
        {
            Debug.Log("[Started]");
            UIGameManager.Instance.SetCountdownActive(false);
        }
        else if (state == GameState.Preparing)
        {
            Debug.Log("[Preparing]");
            UIGameManager.Instance.SetReadyButtonActive(false);
            UIGameManager.Instance.SetCountdownActive(true);
            StartCoroutine(StartCountdown(3));
        }
        else if (state == GameState.Waiting)
        {
            Debug.Log("[Waiting]");
            UIGameManager.Instance.SetReadyButtonActive(true);
            //SetPlayerReady(false);
        }
    }

}
