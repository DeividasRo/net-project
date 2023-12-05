using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/ ReservoirScriptableObject", order = 1)]
public class ReservoirScriptableObject : ScriptableObject
{
    public GameObject reservoirPrefab;

    public float maxY, maxX;
}