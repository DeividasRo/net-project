using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System;

public class UIGameManager : Singleton<UIGameManager>
{
    [SerializeField]
    private TMP_Text _codeText;
    [SerializeField]
    private Button _readyButton;
    private PlayerNetwork _playerNetwork;


    private void Start()
    {
        _codeText.text = GameObject.Find("NetworkManager").GetComponent<Relay>().joinCode;
        _playerNetwork = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
    }

    public void OnReadyButtonClicked()
    {
        _playerNetwork.SetPlayerReady();
        _readyButton.GetComponent<Image>().color = Color.green;
    }

    public void ReadyButtonVisibilityByState(GameState state)
    {
        Debug.Log($"UIGameManager: {state}");
        if (state == GameState.Started || state == GameState.Preparing)
        {
            _readyButton.gameObject.SetActive(false);
        }
        else
        {
            _readyButton.gameObject.SetActive(true);
        }
    }
}
