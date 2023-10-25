using UnityEngine;

public class QualityPrefs : MonoBehaviour
{
    void Awake()
    {
        QualitySettings.vSyncCount = 1;
#if UNITY_EDITOR
        Application.targetFrameRate = 60;
#endif
    }
}
