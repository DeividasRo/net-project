using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using JetBrains.Annotations;
using System.Collections.Generic;

public class UIGameManager : Singleton<UIGameManager>
{
    [SerializeField]
    private TMP_Text _codeText, _countdownText, _correctAnswerText, _resultsText;
    [SerializeField]
    private Button _readyButton;
    [SerializeField]
    private TMP_InputField _guessIF;
    private PlayerNetwork _playerNetwork;


    private void Start()
    {
        _codeText.text = GameObject.Find("NetworkManager").GetComponent<Relay>().joinCode;
        _guessIF.onValidateInput += ValidateGuessInput;
        _playerNetwork = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
    }

    private char ValidateGuessInput(string text, int charIndex, char addedChar)
    {
        if (!char.IsDigit(addedChar))
        {
            return '\0';
        }
        return addedChar;
    }

    public void OnReadyButtonClicked()
    {
        _playerNetwork.SetPlayerReady();
    }

    public void UpdateReadyButtonColorByReadyState(bool ready)
    {
        if (ready)
        {
            _readyButton.GetComponent<Image>().color = Color.green;
        }
        else
        {
            _readyButton.GetComponent<Image>().color = Color.white;
        }
    }

    public string GetGuessInputText()
    {
        return _guessIF.text;
    }
    public void ResetGuessInputText()
    {
        _guessIF.text = "";
    }

    public void SetReadyButtonActive(bool toActive)
    {
        _readyButton.gameObject.SetActive(toActive);
    }

    public void SetGuessInputActive(bool toActive)
    {
        _guessIF.gameObject.SetActive(toActive);
    }

    public void SetCountdownTextActive(bool toActive)
    {
        _countdownText.gameObject.SetActive(toActive);
    }
    public void SetCorrectAnswerTextActive(bool toActive)
    {
        _correctAnswerText.gameObject.SetActive(toActive);
    }

    public void SetRoundScoresTextActive(bool toActive)
    {
        _resultsText.gameObject.SetActive(toActive);
    }

    public void SetCountdownText(string countdownText)
    {
        _countdownText.text = countdownText;
    }

    public void SetRoundScoresText(Dictionary<ulong, int> resultsDict)
    {
        _resultsText.text = "";
        foreach (KeyValuePair<ulong, int> result in resultsDict)
        {
            _resultsText.text += $"{result.Key} - {result.Value}\n";
        }
    }

    public void SetCorrectAnswerText(int answer)
    {
        _correctAnswerText.text = $"CORRECT ANSWER\n{answer}";
    }
}
