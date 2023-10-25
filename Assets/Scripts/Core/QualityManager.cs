using UnityEngine;

public class QualityManager : MonoBehaviour
{
    void Awake()
    {
        QualitySettings.SetQualityLevel(4, true);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }
}
