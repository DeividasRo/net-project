using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class PlayerCamera : MonoBehaviour
{

    [SerializeField]
    private float _speed = 5f;
    [SerializeField]
    private float minFov = 40f;
    [SerializeField]
    private float maxFov = 80f;
    [SerializeField]
    private float sensitivity = 2f;

    public Transform target;

    void Update()
    {
        transform.RotateAround(target.position, transform.up, Input.GetAxisRaw("Horizontal") * -_speed);

        float fov = GetComponent<Camera>().fieldOfView;
        fov += Input.GetAxisRaw("Vertical") * -sensitivity;
        fov = Mathf.Clamp(fov, minFov, maxFov);
        GetComponent<Camera>().fieldOfView = fov;
    }
}
