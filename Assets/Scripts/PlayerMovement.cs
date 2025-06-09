// PlayerMovement.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float rotationSpeed = 720f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cameraTransform;
    public SimpleCameraController cameraController;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float sprintDrainRate = 15f;   // per second
    public float dodgeCost = 20f;   // per roll
    public float staminaRegenRate = 10f;   // per second
    public float regenDelay = 1f;    // seconds after last use
    public StaminaBar staminaBar;          // assign your StaminaBG here

    // exposed to Animator
    public Vector2 InputVector { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsLockedOn => cameraController != null && cameraController.IsLocked;

    // internal
    CharacterController controller;
    Animator animator;
    float verticalVelocity;
    bool isSprinting;

    // stamina state
    float currentStamina;
    float regenTimer;

    // InputSystem
    InputSystem_Actions controls;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction dodgeAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;

        controls = new InputSystem_Actions();
        controls.Player.Enable();
        moveAction = controls.Player.Move;
        jumpAction = controls.Player.Jump;
        sprintAction = controls.Player.Sprint;

        dodgeAction = new InputAction(
            name: "EastCircle",
            type: InputActionType.Button,
            binding: "<Gamepad>/buttonEast"
        );
        dodgeAction.Enable();

        // init stamina
        currentStamina = maxStamina;
        staminaBar.UpdateStamina(currentStamina, maxStamina);
    }

    void OnDisable()
    {
        dodgeAction.Disable();
        controls.Player.Disable();
    }

    void Update()
    {
        // 1) Check dodge state
        var state = animator.GetCurrentAnimatorStateInfo(0);
        bool inDodge = state.IsName("Dodge");
        animator.applyRootMotion = inDodge;

        // 2) Always tick gravity
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        // 3) TICK STAMINA REGEN
        regenTimer += Time.deltaTime;
        if (regenTimer >= regenDelay && currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            staminaBar.UpdateStamina(currentStamina, maxStamina);
        }

        // 4) If in dodge animation, let OnAnimatorMove handle motion & skip the rest
        if (inDodge)
            return;

        // 5) Read movement input
        InputVector = moveAction.ReadValue<Vector2>();

        // 6) Dodge input
        if (dodgeAction.triggered && controller.isGrounded && currentStamina >= dodgeCost)
        {
            // spend stamina
            currentStamina -= dodgeCost;
            regenTimer = 0f;
            staminaBar.UpdateStamina(currentStamina, maxStamina);

            animator.SetTrigger("Dodge");
            return;
        }

        // 7) Sprint toggle & drain
        if (sprintAction.triggered)
            isSprinting = !isSprinting;
        if (InputVector.magnitude < 0.1f)
            isSprinting = false;

        if (isSprinting && currentStamina > 0f)
        {
            float drain = sprintDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina - drain);
            regenTimer = 0f;
            staminaBar.UpdateStamina(currentStamina, maxStamina);

            if (currentStamina <= 0f)
                isSprinting = false;
        }

        // 8) Jump
        if (controller.isGrounded && jumpAction.triggered)
        {
            animator.SetTrigger("JumpTrigger");
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 9) Normal move & rotate
        Move(isSprinting);
        Rotate();
    }

    void Move(bool sprinting)
    {
        Vector3 f = cameraTransform.forward; f.y = 0; f.Normalize();
        Vector3 r = cameraTransform.right; r.y = 0; r.Normalize();
        Vector3 h = f * InputVector.y + r * InputVector.x;
        if (h.sqrMagnitude > 1f) h.Normalize();

        float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);
        Vector3 vel = h * speed;
        vel.y = verticalVelocity;
        Velocity = vel;

        controller.Move(vel * Time.deltaTime);
    }

    void Rotate()
    {
        if (IsLockedOn && cameraController.LockTarget != null)
        {
            Vector3 dir = cameraController.LockTarget.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                var tgt = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, tgt, rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            Vector3 horiz = new Vector3(Velocity.x, 0, Velocity.z);
            if (horiz.sqrMagnitude > 0.001f)
            {
                var tgt = Quaternion.LookRotation(horiz);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, tgt, rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    // feeds the Dodge animation’s root motion + gravity, while smoothing rotation
    void OnAnimatorMove()
    {
        if (!animator.applyRootMotion)
            return;

        // 1) get input-based direction
        Vector3 f = cameraTransform.forward; f.y = 0; f.Normalize();
        Vector3 r = cameraTransform.right; r.y = 0; r.Normalize();
        Vector3 h = f * InputVector.y + r * InputVector.x;
        if (h.sqrMagnitude > 1f) h.Normalize();

        // 2) snap facing instantly toward dodge
        if (h.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(h);

        // 3) apply horizontal root motion + vertical gravity
        Vector3 motion = animator.deltaPosition
                       + Vector3.up * (verticalVelocity * Time.deltaTime);

        controller.Move(motion);
    }
}
