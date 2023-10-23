using UnityEngine;
using TMPro;
using Unity.Netcode;

public class UIGameManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _codeText;

    private void Start()
    {
        _codeText.text = GameObject.Find("NetworkManager").GetComponent<Relay>().joinCode;
    }
}
