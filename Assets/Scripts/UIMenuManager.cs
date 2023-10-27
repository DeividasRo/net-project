using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField]
    private Relay _relay;
    [SerializeField]
    private TMP_Text _joinInputText;
    [SerializeField]
    private Button _hostButton, _joinButton;
    [SerializeField]
    private TMP_InputField _joinCodeIF;

    private void Update()
    {
        if (_joinInputText.text.Length != _joinCodeIF.characterLimit + 1)
        {
            _joinButton.interactable = false;
        }
        else
        {
            _joinButton.interactable = true;
        }
    }

    public void OnJoinButtonClicked()
    {
        _relay.joinCode = _joinInputText.text.Substring(0, _joinCodeIF.characterLimit);
        _relay.JoinRelay();
    }

    public void OnHostButtonClicked()
    {
        _hostButton.interactable = false;
        _joinButton.interactable = false;
        _relay.CreateRelay();
    }
}
