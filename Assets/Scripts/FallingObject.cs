using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class FallingObject : NetworkBehaviour
{
    public Mesh[] meshes;
    public Material[] materials;
    private int _objectMeshId = 0;
    private int _objectColorId = 0;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ModifyObjectType();
    }

    private void ModifyObjectType()
    {
        _objectMeshId = ObjectSpawner.Instance.objectMeshId.Value;
        _objectColorId = ObjectSpawner.Instance.objectColorId.Value;
        Destroy(GetComponent<Collider>());
        GetComponent<MeshFilter>().mesh = meshes[_objectMeshId];
        GetComponent<MeshRenderer>().material = materials[_objectColorId];
        Invoke(nameof(AddCollider), 1f);

    }

    private void AddCollider()
    {
        if (_objectMeshId == 0)
        {
            this.AddComponent<BoxCollider>();
        }
        else if (_objectMeshId == 1)
        {
            this.AddComponent<SphereCollider>();
        }
        else if (_objectMeshId == 2)
        {
            this.AddComponent<CapsuleCollider>();
        }
    }
}
