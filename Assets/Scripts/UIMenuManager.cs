using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField]
    private Relay _relay;
    [SerializeField]
    private Button _hostButton, _joinButton;
    [SerializeField]
    private TMP_InputField _joinCodeIF, _nameIF;

    private void Awake()
    {
        _nameIF.text = PlayerPrefs.GetString("PlayerName", "");
    }

    private void Start()
    {
        _joinCodeIF.onValueChanged.AddListener(delegate { OnJoinCodeIFValueChanged(); });
        _nameIF.onValueChanged.AddListener(delegate { OnNameIFValueChanged(); });
    }

    private void UpdateJoinButtonState()
    {
        if (_joinCodeIF.text.Length == _joinCodeIF.characterLimit && _joinCodeIF.text.All(char.IsLetterOrDigit) &&
            _nameIF.text.Length >= 3 && _nameIF.text.All(char.IsLetterOrDigit))
        {
            _joinButton.interactable = true;
        }
        else
        {
            _joinButton.interactable = false;
        }
    }

    private void OnJoinCodeIFValueChanged()
    {
        UpdateJoinButtonState();
    }

    private void OnNameIFValueChanged()
    {
        if (_nameIF.text.Length >= 3)
        {
            PlayerPrefs.SetString("PlayerName", _nameIF.text);
        }
        UpdateJoinButtonState();
    }

    public void OnJoinButtonClicked()
    {
        _relay.joinCode = _joinCodeIF.text.Substring(0, _joinCodeIF.characterLimit);
        _relay.JoinRelay();
    }

    public void OnHostButtonClicked()
    {
        _hostButton.interactable = false;
        _joinButton.interactable = false;
        _relay.CreateRelay();
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }
}
