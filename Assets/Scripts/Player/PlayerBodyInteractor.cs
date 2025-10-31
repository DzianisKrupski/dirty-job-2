
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public struct CollisionTimestamp
    {
        public double Timestamp;
        public bool IsOnCollision;
    }
    
    public class PlayerBodyInteractor : NetworkBehaviour
    {
        [SerializeField] private  double ownedTime = 1f;
        [SerializeField] private LayerMask dynamicRaycastLayer;
        
        private Dictionary<NetworkObject, CollisionTimestamp> _ownedMap = new Dictionary<NetworkObject, CollisionTimestamp>();

        private void OnTriggerEnter(Collider other)
        {
            if (((1 << other.gameObject.layer) & dynamicRaycastLayer) == 0)
                return;
            
            
            
            if (other.attachedRigidbody.TryGetComponent<NetworkObject>(out var networkObject))
            {
                if (networkObject != null && networkObject.IsSpawned)
                {
                    if (!_ownedMap.ContainsKey(networkObject) && networkObject.Owner.ClientId == -1)
                    {
                        Debug.Log($"Give ownership {networkObject}");
                        networkObject.GiveOwnership(Owner);
                        _ownedMap.Add(networkObject,
                            new CollisionTimestamp
                            {
                                Timestamp = Time.timeAsDouble,
                                IsOnCollision = true
                            });
                    }
                    else if (_ownedMap.ContainsKey(networkObject))
                    {
                        var timestamp = _ownedMap[networkObject];
                        timestamp.Timestamp = Time.timeAsDouble;
                        timestamp.IsOnCollision = true;
                        _ownedMap[networkObject] = timestamp;
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (((1 << other.gameObject.layer) & dynamicRaycastLayer) == 0)
                return;
            
            if (other.attachedRigidbody.TryGetComponent<NetworkObject>(out var networkObject))
            {
                if (networkObject != null && networkObject.IsSpawned)
                {
                    if (_ownedMap.ContainsKey(networkObject))
                    {
                        var timestamp = _ownedMap[networkObject];
                        timestamp.Timestamp = Time.timeAsDouble;
                        timestamp.IsOnCollision = false;
                        _ownedMap[networkObject] = timestamp;
                    }
                }
            }
        }
        
        private void ProcessOwnershipMap()
        {
            var time = Time.timeAsDouble;
            List<NetworkObject> removeList = new List<NetworkObject>(_ownedMap.Count);
        
            foreach (var kvp in _ownedMap)
            {
                if (!kvp.Value.IsOnCollision && time - kvp.Value.Timestamp > ownedTime)
                {
                    removeList.Add(kvp.Key);
                    Debug.Log($"Return ownership {kvp.Key} : {time - kvp.Value.Timestamp}");
                    kvp.Key.RemoveOwnership();
                }
            }

            foreach (var remove in removeList)
            {
                _ownedMap.Remove(remove);
            }
        }

        private void FixedUpdate()
        {
            ProcessOwnershipMap();
        }
    }
}