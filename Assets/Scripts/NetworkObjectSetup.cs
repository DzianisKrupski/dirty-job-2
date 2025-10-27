using Unity.Netcode;
using UnityEngine;

public class NetworkObjectSetup : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if(!IsServer) return;
        
        var no = GetComponent<NetworkObject>();
        // на всякий случай снимем лок
        no.SetOwnershipLock(false);
    }
}