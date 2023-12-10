using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Collections;
using System.Collections.Generic;
using System;
using System.Collections;

public class UIGameManager : Singleton<UIGameManager>
{
    [SerializeField]
    private TMP_Text _codeText, _countdownText, _correctAnswerText, _resultsText, _connectedCountText;
    [SerializeField]
    private Button _readyButton;
    [SerializeField]
    private TMP_InputField _guessIF;
    private PlayerNetwork _playerNetwork;
    private int _maxConnections;


    private void Awake()
    {
        _codeText.text = NetworkManager.Singleton.GetComponent<Relay>().joinCode;
        _maxConnections = NetworkManager.Singleton.GetComponent<Relay>().maxConnections;
    }

    private void Start()
    {
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
        _playerNetwork.SetPlayerReady(true);
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
        _guessIF.Select();
    }

    public void SetCountdownTextActive(bool toActive)
    {
        _countdownText.gameObject.SetActive(toActive);
    }
    public void SetCorrectAnswerTextActive(bool toActive)
    {
        _correctAnswerText.gameObject.SetActive(toActive);
    }

    public void SetScoreboardTextActive(bool toActive)
    {
        _resultsText.gameObject.SetActive(toActive);
    }

    public void SetCountdownText(string countdownText)
    {
        _countdownText.text = countdownText;
    }

    public void SetScoreboardText(Dictionary<ulong, Tuple<FixedString32Bytes, int>> resultsDict)
    {
        _resultsText.text = "";
        foreach (KeyValuePair<ulong, Tuple<FixedString32Bytes, int>> result in resultsDict)
        {
            _resultsText.text += $"{result.Value.Item1} - {result.Value.Item2}\n";
        }
    }

    public void SetConnectedCountText(int count)
    {
        _connectedCountText.text = $"{count}/{_maxConnections}";
    }

    public void SetCorrectAnswerText(int answer)
    {
        _correctAnswerText.text = $"CORRECT ANSWER\n{answer}";
    }

    public void OnExitToMenuButtonClicked()
    {
        _playerNetwork.Disconnect();
    }
}
