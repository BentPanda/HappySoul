using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class SimpleCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your player (or whatever the camera follows)")]
    public Transform target;

    [Tooltip("Which layers count as lockable targets (e.g. your Enemy layer)")]
    public LayerMask targetLayer;

    [Header("Lock-On Settings")]
    [Tooltip("Max distance to search for targets")]
    public float lockRadius = 10f;
    [Tooltip("Max angle in front of player to consider (in degrees)")]
    public float lockAngle = 60f;

    [Header("Orbit & Follow")]
    public Vector3 offset = new Vector3(0f, 2f, -4f);
    public float followSpeed = 10f;
    public float lookSpeed = 120f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Lock-On Camera")]
    [Tooltip("Horizontal distance behind the player when locked on")]
    public float lockDistance = 4f;
    [Tooltip("Vertical height above the player when locked on")]
    public float lockHeight = 2f;

    // Private state
    private InputSystem_Actions controls;
    private InputAction lookAction;
    private InputAction lockOnAction;

    private bool isLocked;
    private Transform lockTarget;

    public bool IsLocked => isLocked;
    public Transform LockTarget => lockTarget;

    private float yaw;
    private float pitch;

    void Awake()
    {
        controls = new InputSystem_Actions();
        controls.Player.Enable();

        lookAction = controls.Player.Look;
        lockOnAction = controls.Player.LockOn;
    }

    void OnDestroy()
    {
        controls.Player.Disable();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1) Toggle lock on button-press
        if (lockOnAction.triggered)
        {
            if (isLocked) ReleaseLock();
            else SelectLockTarget();
        }

        // 2) Read orbit input & update yaw/pitch (only when not locked)
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        if (!isLocked && lookInput.magnitude > 0.1f)
        {
            yaw += lookInput.x * lookSpeed * Time.deltaTime;
            pitch -= lookInput.y * lookSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // 3) Compute desired camera position & look
        if (isLocked && lockTarget != null)
        {
            // Direction from player to target (flat)
            Vector3 toTarget = lockTarget.position - target.position;
            toTarget.y = 0;
            Vector3 forwardDir = toTarget.normalized;

            // Desired position: behind the player relative to the target
            Vector3 desiredPos = target.position
                               - forwardDir * lockDistance
                               + Vector3.up * lockHeight;

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

            // Always look at the target's head
            Vector3 aimPoint = lockTarget.position + Vector3.up * 1.5f;
            transform.LookAt(aimPoint);
        }
        else
        {
            // Free-orbit mode
            Quaternion orbitRot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPos = target.position + orbitRot * offset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
            transform.rotation = orbitRot;
        }
    }

    private void SelectLockTarget()
    {
        var hits = Physics.OverlapSphere(target.position, lockRadius, targetLayer);

        Vector3 forward = target.forward;
        forward.y = 0;
        forward.Normalize();

        Transform best = null;
        float bestDot = Mathf.Cos(lockAngle * Mathf.Deg2Rad);

        foreach (var c in hits)
        {
            Vector3 dir = (c.transform.position - target.position).normalized;
            dir.y = 0;

            float dot = Vector3.Dot(forward, dir);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = c.transform;
            }
        }

        if (best != null)
        {
            isLocked = true;
            lockTarget = best;
        }
        else
        {
            isLocked = false;
            lockTarget = null;
        }
    }

    private void ReleaseLock()
    {
        // 1) Read current camera rotation
        Vector3 camEuler = transform.rotation.eulerAngles;

        // 2) Sync yaw/pitch so free-orbit picks up where we're looking now
        yaw = camEuler.y;
        pitch = camEuler.x;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 3) Turn off lock-on
        isLocked = false;
        lockTarget = null;
    }

    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, lockRadius);

        Vector3 fwd = target.forward;
        fwd.y = 0;
        fwd.Normalize();

        float halfAng = lockAngle * 0.5f;
        Quaternion leftRot = Quaternion.AngleAxis(-halfAng, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAng, Vector3.up);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(target.position, leftRot * fwd * lockRadius);
        Gizmos.DrawRay(target.position, rightRot * fwd * lockRadius);
    }
}
