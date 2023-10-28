using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public void StopAllObjects(float delay = 0f)
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("FallingObject"))
        {
            Rigidbody rb = go.GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePosition;
            rb.GetComponent<Collider>().enabled = false;
        }
    }

}
