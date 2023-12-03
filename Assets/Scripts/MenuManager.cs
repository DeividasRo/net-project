using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _networkManager;

    private void Awake()
    {
        if (!GameObject.Find("NetworkManager"))
        {
            GameObject nm = Instantiate(_networkManager, Vector3.zero, Quaternion.identity);
            nm.name = "NetworkManager";
        }

    }
}
