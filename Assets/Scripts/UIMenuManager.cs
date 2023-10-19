using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField]
    private Relay relay;
    [SerializeField]
    private TMP_Text joinInputText;
    [SerializeField]
    private Button joinButton;
    [SerializeField]
    private TMP_InputField joinCodeIF;

    private void Update()
    {
        if (joinInputText.text.Length != joinCodeIF.characterLimit + 1)
        {
            joinButton.interactable = false;
        }
        else
        {
            joinButton.interactable = true;
        }
    }

    public void OnJoinButtonClicked()
    {
        relay.joinCode = joinInputText.text.Substring(0, joinCodeIF.characterLimit);
        relay.JoinRelay();
    }

    public void OnHostButtonClicked()
    {
        relay.CreateRelay();
    }
}
