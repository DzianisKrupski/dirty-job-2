using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyController : NetworkBehaviour
{
    
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
    
    private Rigidbody _rigidbody;
    
    public Vector3 DownDirection { get; private set; } = Vector3.down;
    public Vector2 MoveInput { get; set; } // (x,z) в плоскости
    public Vector2 LookInput { get; set; } // (x,z) в плоскости

    public override void OnNetworkSpawn()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if(!IsOwner) return;
        
        DampVelocity();
        AddRideForce();
        AddMovementForce();
    }

    private void DampVelocity()
    {
        _rigidbody.linearVelocity = Vector3.Lerp(_rigidbody.linearVelocity, Vector3.zero, forceDemping * Time.fixedDeltaTime);
    }

    private void AddMovementForce()
    {
        Vector3 moveDirection = new Vector3(MoveInput.x, 0, MoveInput.y);
        Vector2 currentVelocity2D = new Vector2(_rigidbody.linearVelocity.x, _rigidbody.linearVelocity.z);


        float forceTime = (Vector3.Dot(currentVelocity2D.normalized, moveDirection.normalized) + 1f) / 2f;
        
        _rigidbody.AddForce(moveDirection.normalized * (forceAxeleration * dampingCurve.Evaluate(forceTime)), 
            ForceMode.Acceleration);
    }

    private void AddRideForce()
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

        float springForce = (x * rideSpringStrength) - (relVel * rideSpringDamper);

        Debug.DrawLine(transform.position, transform.position + (rayDir * springForce), Color.yellow);

        _rigidbody.AddForce(rayDir * springForce);

        if (hitBody != null)
        {
            hitBody.AddForceAtPosition(rayDir * -springForce, raycastHit.point);
        }
    }
}