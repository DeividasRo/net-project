using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField]
    private Button _hostButton, _joinButton;
    [SerializeField]
    private TMP_InputField _joinCodeIF, _nameIF;
    private Relay _relay;
    private bool _canJoin = false, _canHost = false;

    private void Awake()
    {
        _nameIF.text = PlayerPrefs.GetString("PlayerName", "");
    }

    private void Start()
    {
        UpdateHostState();
        _joinCodeIF.onValueChanged.AddListener(delegate { OnJoinCodeIFValueChanged(); });
        _nameIF.onValueChanged.AddListener(delegate { OnNameIFValueChanged(); });
        _relay = GameObject.Find("NetworkManager").GetComponent<Relay>();
    }

    private void UpdateJoinState()
    {
        if (_joinCodeIF.text.Length == _joinCodeIF.characterLimit && _joinCodeIF.text.All(char.IsLetterOrDigit) &&
            _nameIF.text.Length >= 3 && _nameIF.text.All(char.IsLetterOrDigit))
        {
            _canJoin = true;
        }
        else
        {
            _canJoin = false;
        }
    }

    private void UpdateHostState()
    {
        if (_nameIF.text.Length >= 3 && _nameIF.text.All(char.IsLetterOrDigit))
            _canHost = true;
        else
            _canHost = false;

    }

    private void OnJoinCodeIFValueChanged()
    {
        UpdateJoinState();
    }

    private void OnNameIFValueChanged()
    {
        UpdateJoinState();
        UpdateHostState();
    }

    public void OnJoinButtonClicked()
    {
        if (!_canJoin)
        {
            StartCoroutine(FlashButtonOutlineRed(_joinButton));
            return;
        }
        _relay.joinCode = _joinCodeIF.text.Substring(0, _joinCodeIF.characterLimit);
        PlayerPrefs.SetString("PlayerName", _nameIF.text);
        _relay.JoinRelay();
    }

    public void OnHostButtonClicked()
    {
        if (!_canHost)
        {
            StartCoroutine(FlashButtonOutlineRed(_hostButton));
            return;
        }
        _hostButton.interactable = false;
        _joinButton.interactable = false;
        PlayerPrefs.SetString("PlayerName", _nameIF.text);
        _relay.CreateRelay();
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    private IEnumerator FlashButtonOutlineRed(Button button)
    {
        button.GetComponent<Outline>().effectColor = Color.red;
        yield return new WaitForSeconds(0.2f);
        button.GetComponent<Outline>().effectColor = Color.white;
    }
}
