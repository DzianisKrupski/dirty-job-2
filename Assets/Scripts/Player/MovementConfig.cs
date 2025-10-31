#nullable enable
using UnityEngine;
using System;

namespace Player
{
    [CreateAssetMenu(fileName = "MovementConfig_00_SO", menuName = "Game/MovementConfig")]
    public sealed class MovementConfig : ScriptableObject
    {
        public event Action? OnChanged;

        [Header("Ground Move")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float accel = 30f;
        [SerializeField] private float maxGroundSpeed = 7f;

        [Header("Air Control")]
        [SerializeField] private float airAccel = 8f;
        [SerializeField] private float maxAirSpeed = 5f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBuffer = 0.12f;

        [Header("Slide")]
        [SerializeField] private float slideMinSpeed = 8f;
        [SerializeField] private float slideFriction = 1.5f;

        [Header("Crawl")]
        [SerializeField] private float crawlSpeed = 2.5f;
        [SerializeField] private float standHeight = 1.8f;
        [SerializeField] private float crawlHeight = 1.0f;
        [SerializeField] private float heightLerpSpeed = 12f;

        [Header("Hover Spring")]
        [SerializeField] private float rideHeight = 0.9f;
        [SerializeField] private float springK = 1200f;
        [SerializeField] private float hoverTolerance = 0.01f;
        [SerializeField] private float maxSpringAcceleration = 60f;
        [SerializeField] private float groundCheckRadius = 0.25f;
        [SerializeField] private float maxSlopeAngle = 50f;
        [SerializeField] private float springDisableAfterJump = 0.12f;
        
        [Header("PID (yaw)")]
        [SerializeField] private float yawKp = 50f;
        [SerializeField] private float yawKd = 8f;
        [SerializeField] private float maxYawTorque = 500f;
        [SerializeField] private float idleYawFriction = 5f; 
        [SerializeField] private float maxAngularVelocity = 50f; // лимит (град/с) → сконвертим в рад/с
        
        
        [SerializeField] private float lookSensitivity = 1.5f;
        [SerializeField] private float eyeHeightStand = 1.6f;
        [SerializeField] private float eyeHeightCrawl = 1.0f;
        [SerializeField] private float cameraLerpSpeed = 12f;
        [SerializeField] private float pitchMax = 85f;
        [SerializeField] private float pitchMin = -85f;
        
        [Header("Interactor")]
        [SerializeField] private float useDistance = 3.0f;
        [SerializeField] private float holdDistance = 1.8f;
        [SerializeField] private float holdSpring = 90f;
        [SerializeField] private float holdDamper = 12f;
        [SerializeField] private float throwImpulse = 12f;

        public float MoveSpeed => moveSpeed;
        public float Accel => accel;
        public float MaxGroundSpeed => maxGroundSpeed;
        public float AirAccel => airAccel;
        public float MaxAirSpeed => maxAirSpeed;
        public float JumpHeight => jumpHeight;
        public float CoyoteTime => coyoteTime;
        public float JumpBuffer => jumpBuffer;
        public float SlideMinSpeed => slideMinSpeed;
        public float SlideFriction => slideFriction;
        public float CrawlSpeed => crawlSpeed;
        public float StandHeight => standHeight;
        public float CrawlHeight => crawlHeight;
        public float HeightLerpSpeed => heightLerpSpeed;
        public float RideHeight => rideHeight;
        public float SpringK => springK;
        public float HoverTolerance => hoverTolerance;
        public float MaxSpringAcceleration => maxSpringAcceleration;
        public float GroundCheckRadius => groundCheckRadius;
        public float MaxSlopeAngle => maxSlopeAngle;
        public float SpringDisableAfterJump => springDisableAfterJump;
        public float YawKp => yawKp;
        public float YawKd => yawKd;
        public float MaxYawTorque => maxYawTorque;
        public float IdleYawFriction => idleYawFriction;
        public float MaxAngularVelocity => maxAngularVelocity; 
        public float LookSensitivity => lookSensitivity;
        public float EyeHeightStand => eyeHeightStand;
        public float EyeHeightCrawl => eyeHeightCrawl;
        public float CameraLerpSpeed => cameraLerpSpeed;
        public float PitchMin => pitchMin;
        public float PitchMax => pitchMax;
        
        public float UseDistance => useDistance;
        public float HoldDistance => holdDistance;
        public float HoldSpring => holdSpring;
        public float HoldDamper => holdDamper;
        public float ThrowImpulse => throwImpulse;

#if UNITY_EDITOR
        void OnValidate() => OnChanged?.Invoke(); // моментально дергаем подписчиков в Editor
#endif
        /// <summary>Вызывать из кода при программном изменении параметров.</summary>
        public void NotifyChanged() => OnChanged?.Invoke();
    }
}
