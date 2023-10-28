using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class UIGameManager : Singleton<UIGameManager>
{
    [SerializeField]
    private TMP_Text _codeText;
    [SerializeField]
    private TMP_Text _countdownText;
    [SerializeField]
    private Button _readyButton;
    [SerializeField]
    private TMP_InputField _guessIF;
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

    public void SetReadyButtonActive(bool toActive)
    {
        _readyButton.gameObject.SetActive(toActive);
    }

    public void SetGuessInputActive(bool toActive)
    {
        _guessIF.gameObject.SetActive(toActive);
    }

    public void SetCountdownActive(bool toActive)
    {
        _countdownText.gameObject.SetActive(toActive);
    }

    public void SetCountdownText(string countdownText)
    {
        _countdownText.text = countdownText;
    }
}
