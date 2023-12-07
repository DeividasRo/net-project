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

    private bool _canJoin = false, _canHost = false;

    private void Awake()
    {
        _nameIF.text = PlayerPrefs.GetString("PlayerName", "");
    }

    private void Start()
    {
        _joinCodeIF.onValueChanged.AddListener(delegate { OnJoinCodeIFValueChanged(); });
        _nameIF.onValueChanged.AddListener(delegate { OnNameIFValueChanged(); });
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
        if (_nameIF.text.Length >= 3)
        {
            PlayerPrefs.SetString("PlayerName", _nameIF.text);
        }
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
        _relay.CreateRelay();
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
    }

    private IEnumerator FlashButtonOutlineRed(Button button)
    {
        Color startColor = button.GetComponent<Outline>().effectColor;
        button.GetComponent<Outline>().effectColor = Color.red;
        yield return new WaitForSeconds(0.2f);
        button.GetComponent<Outline>().effectColor = startColor;
    }
}
