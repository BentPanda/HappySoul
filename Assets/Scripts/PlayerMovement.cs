using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    /* ───────── Inspector ───────── */
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
    public float sprintDrainRate = 15f;
    public float dodgeCost = 20f;
    public float staminaRegenRate = 10f;
    public float regenDelay = 1f;
    public StaminaBar staminaBar;

    /* ───────── Runtime (exposed) ───────── */
    public Vector2 InputVector { get; private set; }
    public Vector3 Velocity { get; private set; }
    public bool IsLockedOn => cameraController != null && cameraController.IsLocked;

    /* ───────── internal state ───────── */
    CharacterController controller;
    Animator animator;

    float verticalVelocity;
    bool isSprinting;

    /* stamina state */
    float currentStamina;
    float regenTimer;

    /* movement lock set by Dodge / Combat / Health */
    bool movementLocked = false;

    /* Input-System */
    InputSystem_Actions controls;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction dodgeAction;

    /* ───────── Mono ───────── */
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
        /* ─ 1) current animator state ─ */
        var state = animator.GetCurrentAnimatorStateInfo(0);
        bool inDodge = state.IsName("Dodge");
        bool inHurt = state.IsTag("Hurt");             // NEW

        /* Dodge OR Hurt clips supply root-motion */
        animator.applyRootMotion = inDodge || inHurt;

        /* ─ 2) gravity always ticks ─ */
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        /* ─ 3) stamina regen ─ */
        regenTimer += Time.deltaTime;
        if (regenTimer >= regenDelay && currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            staminaBar.UpdateStamina(currentStamina, maxStamina);
        }

        /* ─ 4) let Dodge / Hurt root-motion drive us ─ */
        if (inDodge || inHurt)
            return;

        /* ─ 5) freeze during lock (combo, hurt, cut-scene, etc.) ─ */
        if (movementLocked)
            return;

        /* ─ 6) normal locomotion logic (unchanged) ─ */
        InputVector = moveAction.ReadValue<Vector2>();

        /* Dodge */
        if (dodgeAction.triggered && controller.isGrounded && currentStamina >= dodgeCost)
        {
            currentStamina -= dodgeCost;
            regenTimer = 0f;
            staminaBar.UpdateStamina(currentStamina, maxStamina);

            animator.SetTrigger("Dodge");
            return;
        }

        /* Sprint */
        if (sprintAction.triggered) isSprinting = !isSprinting;
        if (InputVector.magnitude < 0.1f) isSprinting = false;

        if (isSprinting && currentStamina > 0f)
        {
            float drain = sprintDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina - drain);
            regenTimer = 0f;
            staminaBar.UpdateStamina(currentStamina, maxStamina);

            if (currentStamina <= 0f) isSprinting = false;
        }

        /* Jump */
        if (controller.isGrounded && jumpAction.triggered)
        {
            animator.SetTrigger("JumpTrigger");
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        /* Move & rotate */
        Move(isSprinting);
        Rotate();
    }

    /* ───────── Helpers ───────── */
    public void LockMovement(bool locked)
    {
        movementLocked = locked;
        if (locked) Velocity = Vector3.zero;
    }

    void Move(bool sprinting)
    {
        Vector3 f = cameraTransform.forward; f.y = 0; f.Normalize();
        Vector3 r = cameraTransform.right; r.y = 0; r.Normalize();
        Vector3 h = f * InputVector.y + r * InputVector.x;
        if (h.sqrMagnitude > 1f) h.Normalize();

        float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);
        Vector3 vel = h * speed; vel.y = verticalVelocity;
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
                Quaternion tgt = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, tgt, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            Vector3 horiz = new Vector3(Velocity.x, 0, Velocity.z);
            if (horiz.sqrMagnitude > 0.001f)
            {
                Quaternion tgt = Quaternion.LookRotation(horiz);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, tgt, rotationSpeed * Time.deltaTime);
            }
        }
    }

    /* ───────── Root-motion hooks ───────── */
    void OnAnimatorMove()
    {
        if (!animator.applyRootMotion)
            return;

        var info = animator.GetCurrentAnimatorStateInfo(0);
        bool inDodge = info.IsName("Dodge");
        bool inHurt = info.IsTag("Hurt");

        if (inDodge)
        {
            /* Dodge root-motion (unchanged) */
            Vector3 f = cameraTransform.forward; f.y = 0; f.Normalize();
            Vector3 r = cameraTransform.right; r.y = 0; r.Normalize();
            Vector3 h = f * InputVector.y + r * InputVector.x;
            if (h.sqrMagnitude > 1f) h.Normalize();

            if (h.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(h);

            Vector3 motion = animator.deltaPosition
                           + Vector3.up * (verticalVelocity * Time.deltaTime);
            controller.Move(motion);
        }
        else if (inHurt)
        {
            /* Hurt / HeavyHurt root-motion */
            Vector3 motion = animator.deltaPosition
                           + Vector3.up * (verticalVelocity * Time.deltaTime);
            controller.Move(motion);

            transform.rotation *= animator.deltaRotation;
        }
        else
        {
            /* fallback */
            controller.Move(animator.deltaPosition);
            transform.rotation *= animator.deltaRotation;
        }
    }
}
