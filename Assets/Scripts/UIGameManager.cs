using UnityEngine;
using TMPro;
using Unity.Netcode;

public class UIGameManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text CodeText;

    private void Start()
    {
        CodeText.text = GameObject.Find("NetworkManager").GetComponent<Relay>().joinCode;
    }
}
