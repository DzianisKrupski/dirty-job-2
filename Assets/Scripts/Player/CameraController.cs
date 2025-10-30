#nullable enable
using System;
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private MovementConfig config = default!;
        
        private Vector3 _characterPosition;
        private bool _isCrawl;
        private LookPhysicsState? _lookPhysicsState;

        public void Initialize(LookPhysicsState lookPhysicsState)
        {
            _lookPhysicsState = lookPhysicsState;
        }
        
        public void SetCharacterPosition(Vector3 characterPosition, bool isCrawl)
        {
            _characterPosition = characterPosition;
            _isCrawl = isCrawl;
        }
        
        private void LateUpdate()
        {
            UpdatePosition();
            UpdateRotation();
        }
        private void UpdatePosition()
        {
            float characterHeight = _isCrawl ? config.EyeHeightCrawl : config.EyeHeightStand;
            transform.position = _characterPosition + Vector3.up * characterHeight;
        }

        private void UpdateRotation()
        {
            if (_lookPhysicsState != null)
            {
                float a = _lookPhysicsState.GetAlpha();

                // Интерполяция yaw (по кругу) и pitch
                float yaw   = Mathf.LerpAngle(_lookPhysicsState.Prev.Yaw,   _lookPhysicsState.Curr.Yaw,   a);
                float pitch = Mathf.LerpAngle(_lookPhysicsState.Prev.Pitch, _lookPhysicsState.Curr.Pitch, a);
                // Камера — pitch
                transform.localRotation    = Quaternion.Euler(pitch, yaw, 0f);
            }
        }
    }
}