
using FishNet.Object;
using UnityEngine;

public class OwnerAuthRigidbody : NetworkBehaviour
{
    /*private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    public override void OnNetworkSpawn()
    {
        ApplyMode();
        NetworkObject.OnOwnershipRequestResponse += OnOwnershipChanged;
    }

    public override void OnNetworkDespawn()
    {
        NetworkObject.OnOwnershipRequestResponse -= OnOwnershipChanged;
    }

    private void OnOwnershipChanged(NetworkObject.OwnershipRequestResponseStatus ownershiprequestresponse) => ApplyMode();

    private void ApplyMode()
    {
        // Важно: у префаба NetworkTransform -> Authority Mode = Owner
        // Владелец симулирует физику, остальные — нет
        if (IsOwner)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            rb.isKinematic = true;              // никакой локальной физики
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None; // сглаживание делает NetworkTransform
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }*/
}