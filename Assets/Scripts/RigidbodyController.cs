
using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyController : NetworkBehaviour
{

    private const double OwnedTime = 0.2f;
    
    [Header("Movement")]
    [SerializeField] private float maxVelocityMagnitude = 10f;  
    [SerializeField] private float forceAxeleration = 3f;  
    [SerializeField] private float forceDemping = 0.1f;
    [SerializeField] private AnimationCurve dampingCurve;
    
    [Header("AngularMovement")]
    [SerializeField] private float angularSpeed = 5.0f;  
    [SerializeField] private float yawSmoothTime = 0.06f;    // сглаживание (сек)
    [SerializeField] private float maxYawSpeed = 1080f;
    
    [Header("Ride")]
    [SerializeField] private float rideRaycastDistance;
    [SerializeField] private float rideRaycastOffset;
    [SerializeField] private LayerMask rideRaycastLayer;
    [SerializeField] private float rideHeight;
    [SerializeField] private float rideSpringStrength;
    [SerializeField] private float rideSpringDamper;

    [Header("Ride")]
    [SerializeField] private LayerMask dynamicRaycastLayer;
        
    private Rigidbody _rigidbody;
    
    private struct CollisionTimestamp
    {
        public double Timestamp;
        public bool IsOnCollision;
    }

    private Dictionary<NetworkObject, CollisionTimestamp> _ownedMap = new Dictionary<NetworkObject, CollisionTimestamp>();
    
    public Vector3 DownDirection { get; private set; } = Vector3.down;
    public Vector2 MoveInput { get; set; } // (x,z) в плоскости
    public Vector2 LookInput { get; set; } // (x,z) в плоскости

    public override void OnStartNetwork()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if(!IsOwner) return;
        
        ProcessOwnershipMap();
        DampVelocity();
        //DampVelocityServerRpc();
        AddRideForce(Time.fixedDeltaTime);
        AddMovementForce(Time.fixedDeltaTime);
    }
    
    [ServerRpc] 
    private void GiveOwnership(NetworkObject box, NetworkConnection toWho)
    {
        box.GiveOwnership(toWho); // короткая аренда: 0.2–0.5 c после последнего толчка
    }
    
    [ServerRpc]
    private void ReturnToServerAuth(NetworkObject no)
    {
        if (no != null)
            no.RemoveOwnership(); // сервер снова единственный авторитет
    }

    private void OnCollisionEnter(Collision other)
    {
        if (((1 << other.gameObject.layer) & dynamicRaycastLayer) == 0)
            return;

        if (other.rigidbody.TryGetComponent<NetworkObject>(out var networkObject))
        {
            if (networkObject != null && networkObject.IsSpawned)
            {
                if (!_ownedMap.ContainsKey(networkObject) && networkObject.Owner.ClientId == -1)
                {
                    Debug.Log($"Give ownership {networkObject}");
                    GiveOwnership(networkObject, Owner);
                    _ownedMap.Add(networkObject, new CollisionTimestamp
                    {
                        Timestamp = Time.timeAsDouble,
                        IsOnCollision = true
                    });
                }else if (_ownedMap.ContainsKey(networkObject))
                {
                    var timestamp = _ownedMap[networkObject];
                    timestamp.Timestamp = Time.timeAsDouble;
                    timestamp.IsOnCollision = true;
                    _ownedMap[networkObject] = timestamp;
                }
            }
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (((1 << other.gameObject.layer) & dynamicRaycastLayer) == 0)
            return;

        if (other.rigidbody.TryGetComponent<NetworkObject>(out var networkObject))
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
            if (!kvp.Value.IsOnCollision && time - kvp.Value.Timestamp > OwnedTime)
            {
                removeList.Add(kvp.Key);
                Debug.Log($"Return ownership {kvp.Key} : {time - kvp.Value.Timestamp}");
                ReturnToServerAuth(kvp.Key);
            }
        }

        foreach (var remove in removeList)
        {
            _ownedMap.Remove(remove);
        }
    }

    /*[ServerRpc]
    private void ChangeOwnershipServerRpc(NetworkObjectReference networkObjectReference, ulong clientId)
    {
        if (!networkObjectReference.TryGet(out NetworkObject target))
            return;

        target.ChangeOwnership(clientId);
    }*/
    
    private void DampVelocity()
    {
        _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, Vector3.zero, forceDemping * Time.fixedDeltaTime);
    }
    
    /*[ServerRpc]
    private void DampVelocityServerRpc()
    {
        _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, Vector3.zero, forceDemping * Time.fixedDeltaTime);
    }*/

    private void AddMovementForce(float delta)
    {
        Vector3 moveDirection = new Vector3(MoveInput.x, 0, MoveInput.y);
        Vector2 currentVelocity2D = new Vector2(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.z);


        float forceTime = (Vector3.Dot(currentVelocity2D.normalized, moveDirection.normalized) + 1f) / 2f;
        
        /*ApplyForceServerRpc(moveDirection.normalized * (forceAxeleration * dampingCurve.Evaluate(forceTime) * delta), 
            ForceMode.Acceleration);*/
        
        _rigidbody.AddForce(moveDirection.normalized * (forceAxeleration * dampingCurve.Evaluate(forceTime) * delta), 
            ForceMode.Acceleration);
    }

    private void AddRideForce(float delta)
    {
        if (!Physics.Raycast(transform.position - (DownDirection * rideRaycastOffset), 
                DownDirection, out var raycastHit, rideRaycastDistance, rideRaycastLayer))
        {
            return;
        }

        Vector3 vel = _rigidbody.linearVelocity;
        Vector3 rayDir = transform.TransformDirection(DownDirection);

        Vector3 otherVel = Vector3.zero;
        Rigidbody hitBody = raycastHit.rigidbody;
        if (hitBody != null)
        {
            otherVel = hitBody.linearVelocity;
        }

        float rayDirVel = Vector3.Dot(rayDir, vel);
        float otherDirVel = Vector3.Dot(rayDir, otherVel);

        float relVel = rayDirVel - otherDirVel;

        float x = raycastHit.distance - rideHeight;

        float springForce = (x * rideSpringStrength) - (relVel * rideSpringDamper) * delta;

        Debug.DrawLine(transform.position, transform.position + (rayDir * springForce), Color.yellow);

        _rigidbody.AddForce(rayDir * springForce);
        //ApplyForceServerRpc(rayDir * springForce);

        if (hitBody != null)
        {
            /*if (hitBody.TryGetComponent<NetworkObject>(out var networkObject))
            {
                if (networkObject.IsSpawned)
                {
                    var networkObjectReference = new NetworkObjectReference(networkObject);
                    AddForceAtPositionServerRpc(networkObjectReference, rayDir * -springForce, 
                        raycastHit.point);
                }
                
            }
            else
            {
                hitBody.AddForceAtPosition(rayDir * -springForce, raycastHit.point);
            }*/
            
            hitBody.AddForceAtPosition(rayDir * -springForce, raycastHit.point);
        }
    }

    /*[ServerRpc]
    private void ApplyForceServerRpc(Vector3 force, ForceMode forceMode = ForceMode.Force)
    {
        _rigidbody.AddForce(force, forceMode);
    }
    
    [ServerRpc]
    private void AddForceAtPositionServerRpc(NetworkObjectReference targetRef, Vector3 force, 
        Vector3 position, ForceMode forceMode = ForceMode.Force)
    {
        if (!targetRef.TryGet(out NetworkObject target))
            return;

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null || rb.isKinematic)
            return;

        rb.WakeUp();
        rb.AddForceAtPosition(force, position, forceMode);
    }*/
}