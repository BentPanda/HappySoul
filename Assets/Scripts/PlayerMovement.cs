using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 720f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;
    public SimpleCameraController cameraController;

    // Exposed for animator
    public Vector2 InputVector { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsLockedOn => cameraController != null && cameraController.IsLocked;

    private CharacterController controller;
    private float verticalVelocity;
    private InputSystem_Actions controls;

    void Awake()
    {
        controls = new InputSystem_Actions();
        controls.Player.Enable();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // 1) Read move input
        InputVector = controls.Player.Move.ReadValue<Vector2>();

        HandleGravityAndJump();
        Move();
        Rotate();
    }

    void HandleGravityAndJump()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (isGrounded && controls.Player.Jump.triggered)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;
    }

    void Move()
    {
        // camera‐relative horizontal movement
        Vector3 camF = cameraTransform.forward; camF.y = 0; camF.Normalize();
        Vector3 camR = cameraTransform.right; camR.y = 0; camR.Normalize();

        Vector3 hmove = camF * InputVector.y + camR * InputVector.x;
        if (hmove.sqrMagnitude > 1f) hmove.Normalize();

        // Build a local velocity vector, then assign it in one go:
        Vector3 vel = hmove * moveSpeed;
        vel.y = verticalVelocity;
        Velocity = vel;

        controller.Move(Velocity * Time.deltaTime);
    }

    void Rotate()
    {
        if (IsLockedOn && cameraController.LockTarget != null)
        {
            // Rotate towards lock target
            Vector3 dir = cameraController.LockTarget.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion tgt = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    tgt,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // Rotate towards movement direction
            Vector3 horiz = new Vector3(Velocity.x, 0, Velocity.z);
            if (horiz.sqrMagnitude > 0.001f)
            {
                Quaternion tgt = Quaternion.LookRotation(horiz);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    tgt,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }
}
