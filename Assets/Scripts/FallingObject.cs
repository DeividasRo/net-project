using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class FallingObject : NetworkBehaviour
{
    public Mesh[] meshes;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ModifyObjectType();
    }

    private void ModifyObjectType()
    {
        int objectTypeId = ObjectSpawner.Instance.objectTypeId.Value;
        Destroy(GetComponent<Collider>());
        GetComponent<MeshFilter>().mesh = meshes[objectTypeId];
        if (objectTypeId == 0)
        {
            this.AddComponent<BoxCollider>();
        }
        else if (objectTypeId == 1)
        {
            this.AddComponent<SphereCollider>();
        }

    }
}
