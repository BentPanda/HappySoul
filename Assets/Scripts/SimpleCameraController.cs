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
        // 1) Instantiate & enable your generated InputSystem_Actions
        controls = new InputSystem_Actions();
        controls.Player.Enable();

        // 2) Cache the actions
        lookAction = controls.Player.Look;    // Vector2
        lockOnAction = controls.Player.LockOn;  // Button (Stick press)
    }

    void OnDestroy()
    {
        controls.Player.Disable();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- 1) Toggle lock on button‐press ---
        if (lockOnAction.triggered)
        {
            if (isLocked) ReleaseLock();
            else SelectLockTarget();
        }

        // --- 2) Read orbit input & update yaw/pitch ---
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        if (lookInput.magnitude > 0.1f)
        {
            yaw += lookInput.x * lookSpeed * Time.deltaTime;
            pitch -= lookInput.y * lookSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // --- 3) Compute desired camera position ---
        Quaternion orbitRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + orbitRot * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        // --- 4) Decide where to look ---
        if (isLocked && lockTarget != null)
        {
            // Aim at the target’s head (offset up by 1.5m)
            Vector3 aimPoint = lockTarget.position + Vector3.up * 1.5f;
            transform.LookAt(aimPoint);
        }
        else
        {
            transform.rotation = orbitRot;
        }
    }

    private void SelectLockTarget()
    {
        // Find all colliders in radius on your targetLayer
        var hits = Physics.OverlapSphere(target.position, lockRadius, targetLayer);

        // Player's forward direction (on XZ plane)
        Vector3 forward = target.forward;
        forward.y = 0;
        forward.Normalize();

        Transform best = null;
        float bestDot = Mathf.Cos(lockAngle * Mathf.Deg2Rad);

        foreach (var c in hits)
        {
            // Direction to candidate
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
            // no valid target found
            isLocked = false;
            lockTarget = null;
        }
    }

    private void ReleaseLock()
    {
        isLocked = false;
        lockTarget = null;
    }

    // Optional: visualize your lock-on radius & angle in the editor
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
