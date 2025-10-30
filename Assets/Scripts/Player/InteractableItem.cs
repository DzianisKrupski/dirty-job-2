#nullable enable
using UnityEngine;

namespace Player
{
    [DisallowMultipleComponent]
    public sealed class InteractableItem : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb = default!;
        void Reset()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
}