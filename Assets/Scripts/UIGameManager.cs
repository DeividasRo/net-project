using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

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
        _playerNetwork.isReady.OnValueChanged += OnReadyValueChanged;
    }

    public void OnReadyButtonClicked()
    {
        _playerNetwork.ChangeReadyState();
    }

    private void OnReadyValueChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            _readyButton.GetComponent<Image>().color = Color.green;
        }
        else
        {
            _readyButton.GetComponent<Image>().color = Color.white;
        }
    }

    public void ShowReadyButton(bool show)
    {
        _readyButton.gameObject.SetActive(show);
    }
}
