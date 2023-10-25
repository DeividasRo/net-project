using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;

public class UIGameManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _codeText;
    [SerializeField]
    private Button _readyButton;

    private void Start()
    {
        _codeText.text = GameObject.Find("NetworkManager").GetComponent<Relay>().joinCode;
    }

    public void OnReadyButtonClicked()
    {
        PlayerNetwork playerNetwork = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
        playerNetwork.isReady.OnValueChanged += OnReadyValueChanged;
        playerNetwork.ChangeReadyState();
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
}
