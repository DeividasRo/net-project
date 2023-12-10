using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using UnityEngine.SceneManagement;
using System.Net.WebSockets;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.SocialPlatforms;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> objectCount = new NetworkVariable<int>(50, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> spawnFrequency = new NetworkVariable<float>(0.1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> objectSize = new NetworkVariable<float>(0.5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> maxSpawnPositions = new NetworkVariable<Vector2>(new Vector2(2, 2), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<ulong> serverClientId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> reservoirId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>("Guest", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<int> guess = new NetworkVariable<int>(0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> guessCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Dictionary<ulong, int> resultsDict = new Dictionary<ulong, int>();
    public Dictionary<ulong, Tuple<FixedString32Bytes, int>> sortedResultsDict = new Dictionary<ulong, Tuple<FixedString32Bytes, int>>();
    [SerializeField]
    private List<ReservoirScriptableObject> _reservoirSOList;
    private GameObject spawnedReservoir = null;
    private int _guessTime = 10;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && IsOwner)
        {
            serverClientId.Value = NetworkManager.Singleton.LocalClientId;
            if (spawnedReservoir == null)
            {
                spawnedReservoir = Instantiate(_reservoirSOList[reservoirId.Value].reservoirPrefab, Vector3.zero, Quaternion.identity);
                spawnedReservoir.GetComponent<NetworkObject>().Spawn();
            }
        }
        if (IsOwner)
        {
            playerName.Value = PlayerPrefs.GetString("PlayerName", "Guest");
            Debug.Log(playerName.Value);
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        isReady.OnValueChanged += OnIsReadyValueChanged;
        gameState.OnValueChanged += OnGameStateValueChanged;
        Debug.Log("Player spawned");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        gameState.OnValueChanged -= OnGameStateValueChanged;
        isReady.OnValueChanged -= OnIsReadyValueChanged;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnIsReadyValueChanged(bool prev, bool curr)
    {
        // Network variable synced across clients, not owned by player object
        Debug.Log($"Ready value changed: {prev} -> {curr}, {playerName.Value}");
        if (curr == true)
        {
            EvaluateReadinessServerRpc();
        }
        else
        {
            if (IsHost)
            {
                GameResetServerRpc();
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
    }

    [ClientRpc]
    private void DisconnectClientRpc()
    {
        if (IsHost) return;
        Disconnect();
    }

    public void Disconnect()
    {
        if (IsHost)
        {
            DisconnectClientRpc();
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        }

    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"{clientId} has disconnected.");
    }

    private void OnGameStateValueChanged(GameState prev, GameState curr)
    {
        //Debug.Log($"{prev}, {curr}");
        if (curr == GameState.GameEnded)
        {
            Debug.Log("[GameEnded]");
            UIGameManager.Instance.SetCorrectAnswerText(objectCount.Value);
            UIGameManager.Instance.SetCorrectAnswerTextActive(true);
            UIGameManager.Instance.SetScoreboardText(sortedResultsDict);
            //UIGameManager.Instance.SetScoreboardTextActive(true);
            SetPlayerReadyServerRpc(false);
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
            StartCoroutine(ObjectSpawner.Instance.FreezeAllObjectsWithDelay(3.5f));
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
            if (IsServer)
            {
                StartCoroutine(nameof(StartGameProcess));
            }
            UIGameManager.Instance.SetReadyButtonActive(false);
            UIGameManager.Instance.SetCountdownTextActive(true);
            StartCoroutine(StartCountdown(3));
        }
        else if (curr == GameState.Waiting)
        {
            Debug.Log("[Waiting]");
            if (IsServer)
            {
                PlayerNetwork serverPN = NetworkManager.Singleton.ConnectedClients[serverClientId.Value].PlayerObject.GetComponent<PlayerNetwork>();

                serverPN.spawnedReservoir.GetComponent<NetworkObject>().Despawn();
                Destroy(serverPN.spawnedReservoir);
                serverPN.spawnedReservoir = Instantiate(_reservoirSOList[reservoirId.Value].reservoirPrefab, Vector3.zero, Quaternion.identity);
                serverPN.spawnedReservoir.GetComponent<NetworkObject>().Spawn();

            }
            UIGameManager.Instance.SetReadyButtonActive(true);
            UIGameManager.Instance.SetCorrectAnswerTextActive(false);
            UIGameManager.Instance.ResetGuessInputText();
            UIGameManager.Instance.UpdateReadyButtonColorByReadyState(isReady.Value);
            //UIGameManager.Instance.SetScoreboardTextActive(false);
        }
    }

    private IEnumerator StartGameProcess()
    {
        Debug.Log("Preparing the game...");
        int secondsToPrepare = 3;
        guessCount.Value = 0;
        objectCount.Value = UnityEngine.Random.Range(50, 500);
        objectSize.Value = UnityEngine.Random.Range(0.32f, 0.65f);
        maxSpawnPositions.Value = new Vector2(_reservoirSOList[reservoirId.Value].maxX, _reservoirSOList[reservoirId.Value].maxY);
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
        reservoirId.Value = UnityEngine.Random.Range(0, 2);

        SetGameStateServerRpc(GameState.GuessingEnded);
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
    private void GameResetServerRpc()
    {
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

    public void SetPlayerReady(bool value)
    {
        if (!IsOwner) return;
        UIGameManager.Instance.UpdateReadyButtonColorByReadyState(value);
        SetPlayerReadyServerRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(bool value, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        // Debug.Log($"SPR - PlayerName: {NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetwork>().playerName.Value}, IsReady: {NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetwork>().isReady.Value}");
        NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetwork>().isReady.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void EvaluateReadinessServerRpc()
    {
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            // Debug.Log($"ER - PlayerName: {networkClient.PlayerObject.GetComponent<PlayerNetwork>().playerName.Value}, IsReady: {networkClient.PlayerObject.GetComponent<PlayerNetwork>().isReady.Value}");
            if (!networkClient.PlayerObject.GetComponent<PlayerNetwork>().isReady.Value) return;
        }
        if (NetworkManager.Singleton.ConnectedClientsList.Count != NetworkManager.Singleton.GetComponent<Relay>().maxConnections) return;
        SetGameStateServerRpc(GameState.Preparing);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerGuessServerRpc(ulong clientId, string guessString)
    {
        if (guessString == String.Empty)
        {
            NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetwork>().guess.Value = 0;
        }
        else
        {
            NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<PlayerNetwork>().guess.Value = Int16.Parse(guessString);
        }
        guessCount.Value += 1;
        if (guessCount.Value == NetworkManager.Singleton.ConnectedClientsList.Count)
        {
            EvaluateGuessesServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void EvaluateGuessesServerRpc()
    {
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (!resultsDict.ContainsKey(networkClient.ClientId))
            {
                resultsDict.Add(networkClient.ClientId, Math.Abs(objectCount.Value - networkClient.PlayerObject.GetComponent<PlayerNetwork>().guess.Value));
            }
            else
            {
                resultsDict[networkClient.ClientId] += Math.Abs(objectCount.Value - networkClient.PlayerObject.GetComponent<PlayerNetwork>().guess.Value);
            }
        }
        sortedResultsDict = resultsDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => new Tuple<FixedString32Bytes, int>(NetworkManager.Singleton.ConnectedClientsList[Convert.ToInt32(x.Key.ToString())].PlayerObject.GetComponent<PlayerNetwork>().playerName.Value, x.Value));


        ulong[] clientIds = new ulong[sortedResultsDict.Count];
        int[] values = new int[sortedResultsDict.Count];
        FixedString32Bytes[] names = new FixedString32Bytes[sortedResultsDict.Count];
        int idx = 0;

        foreach (KeyValuePair<ulong, Tuple<FixedString32Bytes, int>> result in sortedResultsDict)
        {
            clientIds[idx] = result.Key;
            values[idx] = result.Value.Item2;
            names[idx] = result.Value.Item1;
            idx++;
        }

        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            for (int i = 0; i < clientIds.Length; i++)
            {
                networkClient.PlayerObject.GetComponent<PlayerNetwork>().resultsDict[clientIds[i]] = values[i];
            }
        }

        SyncResultsDictClientRpc(clientIds, values, names);
        SetGameStateServerRpc(GameState.GameEnded);
    }

    [ClientRpc]
    private void SyncResultsDictClientRpc(ulong[] clientIds, int[] values, FixedString32Bytes[] names)
    {
        sortedResultsDict.Clear();
        for (int i = 0; i < clientIds.Length; i++)
        {
            Debug.Log($"Value: {values[i]}");
            sortedResultsDict[clientIds[i]] = new Tuple<FixedString32Bytes, int>(names[i], values[i]);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        ObjectSpawner.Instance.StartSpawning(objectCount.Value, objectSize.Value, spawnFrequency.Value, maxSpawnPositions.Value);
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
