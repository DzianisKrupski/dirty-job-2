using System;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class SimpleCharacterMotor : NetworkBehaviour
{
    [Header("Shape")]
    [SerializeField] private float height = 2.0f;     // Полная высота капсулы
    [SerializeField] private float radius = 0.5f;     // Радиус капсулы

    [Header("Movement")]
    [SerializeField] private float speed = 5.0f;      // м/с по XZ
    [SerializeField] private float gravity = -9.81f;  // м/с^2 (отрицательная вниз)
    [SerializeField] private LayerMask collisionMask = ~0; // что считаем препятствиями/землёй
    
    [Header("Rotation")]
    [SerializeField] private float angularSpeed = 5.0f;  
    [SerializeField] private float yawSmoothTime = 0.06f;    // сглаживание (сек)
    [SerializeField] private float maxYawSpeed = 1080f;      // макс. скорость (град/с), 0 — без лимита
      

    [Header("Tuning")]
    [SerializeField] private float skinWidth = 0.02f;     // зазор от стен/пола
    [SerializeField] private float groundProbeDistance = 0.15f; // глубина зонда вниз
    [SerializeField] private int maxSlides = 1;            // сколько раз "скользим" по поверхностям
    
    [Header("Push Rigidbodies")]
    [SerializeField] private float pushPower = 1.5f;   // множитель силы толчка
    [SerializeField] private float maxPushMass = 200f;  // толкаем слабее/не толкаем слишком тяжёлые тела
    [SerializeField] private float minPushSpeed = 0.2f; // минимальная горизонтальная скорость для толчка
    [SerializeField] private bool pushAlongGround = true; // толкать только по XZ (не «подбрасывать»)
    
    private float _yawTarget;   // куда хотим прийти
    private float _yawCurrent;  // текущий (сглаженный)
    private float _yawVel;  // внутренняя «скорость» SmoothDamp
    
    public Vector3 Velocity { get; private set; } // м/с
    
    public bool OnGround { get; private set; }

    /// <summary>Ожидаем нормализованное XZ (можно и не нормализовать — мы нормализуем сами).</summary>
    public Vector2 MoveInput { get; set; } // (x,z) в плоскости
    
    public Vector2 LookInput { get; set; } // (x,z) в плоскости

    // --- FixedUpdate loop ---
    private void FixedUpdate()
    {
        if(!IsOwner) return;
        
        float dt = Time.fixedDeltaTime;

        // 1) Обновляем флаг земли
        OnGround = GroundCheck();

        // 3) Горизонтальная скорость задаётся входом относительно поворота персонажа
        Vector3 wishDir = (transform.forward * MoveInput.y + transform.right * MoveInput.x);
        if (wishDir.sqrMagnitude > 1e-6f)
            wishDir.Normalize();

        Vector3 horizVel = wishDir * speed; // м/с
        
        // 4) Вертикаль: гравитация
        float vy = Velocity.y;
        if (OnGround && vy < 0f) vy = -2f; // небольшое прижатие к земле
        vy += gravity * dt;

        Velocity = new Vector3(horizVel.x, vy, horizVel.z);

        // 5) Перемещение: sweep капсулой и остановка у препятствий
        Vector3 delta = Velocity * dt;
        MoveWithCapsule(delta);
    }

    void Update()
    {
        if(!IsOwner) return;
        
        _yawTarget += LookInput.x * angularSpeed; 

        // 2) сглаживаем к целевому углу
        _yawCurrent = Mathf.SmoothDampAngle(
            _yawCurrent, 
            _yawTarget, 
            ref _yawVel, 
            yawSmoothTime, 
            maxYawSpeed <= 0f ? Mathf.Infinity : maxYawSpeed, 
            Time.deltaTime
        );

        // 3) применяем только yaw (по Y)
        var rot = transform.rotation.eulerAngles;
        rot.y = _yawCurrent;
        transform.rotation = Quaternion.Euler(rot);
    }

    // --- Collision / sweep ---

    private void MoveWithCapsule(Vector3 delta)
    {
        Vector3 pos = transform.position;
        Vector3 remaining = delta;

        for (int i = 0; i <= maxSlides; i++)
        {
            if (remaining.sqrMagnitude <= 1e-8f) break;

            Capsule(pos, out Vector3 p1, out Vector3 p2);
            Vector3 dir = remaining.normalized;
            float dist = remaining.magnitude + skinWidth;

            if (!Physics.CapsuleCast(p1, p2, radius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // свободно — двигаемся полностью
                pos += remaining;
                break;
            }
            
            TryPushRigidbody(hit, dir, Time.fixedDeltaTime);

            // Подходим максимально близко к препятствию
            float travel = Mathf.Max(hit.distance - skinWidth, 0f);
            pos += dir * travel;

            // Если это нижняя поверхность (пол) — разрешим "скольжение" дальше только по XZ
            bool isFloor = hit.normal.y > 0.5f;

            // Проецируем оставшееся смещение на плоскость касательной, чтобы "скользить" по поверхности
            Vector3 left = remaining - dir * travel;
            Vector3 slide = Vector3.ProjectOnPlane(left, hit.normal);

            // Для простоты ограничим вертикальное скольжение на стенах
            if (!isFloor) slide.y = Mathf.Min(slide.y, 0f);

            remaining = slide;

            // Маленький выход из препятствия во избежание залипания
            pos += hit.normal * 0.0005f;
        }

        transform.position = pos;
    }

    private bool GroundCheck()
    {
        // Берём капсулу и зондируем немного вниз
        Vector3 origin = transform.position + Vector3.up * 0.01f; // лёгкий подъём, чтобы CheckCapsule не застревал
        Capsule(origin, out Vector3 p1, out Vector3 p2);

        float probe = groundProbeDistance;
        // Смещаем обе точки вниз на probe и проверяем пересечение
        Vector3 p1Down = p1 + Vector3.down * probe;
        Vector3 p2Down = p2 + Vector3.down * probe;

        bool grounded = Physics.CheckCapsule(p1Down, p2Down, radius + skinWidth, collisionMask, QueryTriggerInteraction.Ignore);
        return grounded;
    }

    private void Capsule(Vector3 position, out Vector3 p1, out Vector3 p2)
    {
        float h = Mathf.Max(height, radius * 2f);
        float half = (h * 0.5f) - radius;

        // Капсула стоит на земле так, что её нижняя точка на поверхности Y=position.y
        // Центр — position, ось — по Y
        Vector3 center = position + Vector3.up * (radius + half);
        p1 = center + Vector3.up * half;   // верхняя сфера
        p2 = center - Vector3.up * half;   // нижняя сфера
    }

    [ServerRpc]
    private void TryPushRigidbodyServerRpc(NetworkObjectReference targetRef, Vector3 pushDir, Vector3 hitPoint, float impulseMag)
    {
        if (!targetRef.TryGet(out NetworkObject target))
            return;

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null || rb.isKinematic)
            return;

        rb.WakeUp();
        rb.AddForceAtPosition(pushDir * impulseMag, hitPoint, ForceMode.Impulse);
    }

    private void TryPushRigidbody(RaycastHit hit, Vector3 sweepDir, float dt)
    {
        Rigidbody rb = hit.rigidbody;
        if (rb == null || rb.isKinematic) return;

        // Не толкаем «пол»
        if (hit.normal.y > 0.5f) return;

        Vector3 pushDir = pushAlongGround
            ? Vector3.ProjectOnPlane(sweepDir, Vector3.up)
            : sweepDir;

        if (pushAlongGround && pushDir.y > 0f) pushDir.y = 0f;
        if (pushDir.sqrMagnitude < 1e-6f) return;
        pushDir.Normalize();

        float horizSpeed = new Vector3(Velocity.x, 0f, Velocity.z).magnitude;
        if (horizSpeed < minPushSpeed) return;

        float massScale = (maxPushMass <= 0f) ? 1f : Mathf.Clamp01(maxPushMass / rb.mass);
        float impulseMag = pushPower * horizSpeed * massScale;

        // Только если у объекта есть NetworkObject — иначе сервер о нём не узнает
        NetworkObject netObj = rb.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            var netRef = new NetworkObjectReference(netObj);
            TryPushRigidbodyServerRpc(netRef, pushDir, hit.point, impulseMag);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Визуализация капсулы и ground probe
        Gizmos.color = Color.cyan;
        Capsule(transform.position, out var p1, out var p2);
        DrawWireCapsule(p1, p2, radius);

        Gizmos.color = Color.green;
        Vector3 offset = Vector3.down * groundProbeDistance;
        DrawWireCapsule(p1 + offset, p2 + offset, radius + skinWidth);
    }

    private void DrawWireCapsule(Vector3 p1, Vector3 p2, float r)
    {
        // упрощённая отрисовка: цилиндрическая часть
        Gizmos.DrawWireSphere(p1, r);
        Gizmos.DrawWireSphere(p2, r);
        // рёбра
        Vector3 up = (p1 - p2).normalized;
        Vector3 right = Vector3.Cross(up, Vector3.right).sqrMagnitude < 1e-4f
            ? Vector3.Cross(up, Vector3.forward).normalized
            : Vector3.Cross(up, Vector3.right).normalized;
        Vector3 fwd = Vector3.Cross(up, right).normalized;

        Gizmos.DrawLine(p1 + right * r, p2 + right * r);
        Gizmos.DrawLine(p1 - right * r, p2 - right * r);
        Gizmos.DrawLine(p1 + fwd * r,  p2 + fwd * r);
        Gizmos.DrawLine(p1 - fwd * r,  p2 - fwd * r);
    }
#endif
}
