using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Three-hit light combo, pure-code timing, plays each swing to the end.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public PlayerMovement movement;          // assign in Inspector

    /* ───── configurable combo window (as % of each clip) ───── */
    [Range(0, 1)] public float windowStart = 0.25f;
    [Range(0, 1)] public float windowEnd = 0.6f;

    /* ───── private state ───── */
    Animator anim;
    InputAction attackAction;                // R1 / rightShoulder

    int comboIndex = 0;                   // 0 = idle, 1-3 = swing #
    bool lockActive = false;               // true while movement frozen
    bool queuedPress = false;               // buffered R1

    /* ───── setup ───── */
    void Awake()
    {
        anim = GetComponent<Animator>();

        attackAction = new InputAction(
            name: "AttackR1",
            type: InputActionType.Button,
            binding: "<Gamepad>/rightShoulder"
        );
        attackAction.Enable();
    }
    void OnDisable() => attackAction.Disable();

    /* ───── main loop ───── */
    void Update()
    {
        /* 1) buffer any press */
        if (attackAction.triggered)
            queuedPress = true;

        /* 2) skip while dodging */
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("Dodge"))
            return;

        /* 3) auto-unlock when we’re out of the Attack tag */
        if (lockActive && !IsInAttackState())
        {
            lockActive = false;
            comboIndex = 0;
            queuedPress = false;

            anim.SetInteger("ComboIndex", 0);
            movement?.LockMovement(false);
        }

        /* 4) consume the buffered press if possible */
        if (queuedPress)
            TryConsumePress();
    }

    /* ───── helpers ───── */
    bool IsInAttackState()
    {
        return anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") ||
              (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsTag("Attack"));
    }

    bool IsWithinComboWindow()
    {
        float t = anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
        return t >= windowStart && t <= windowEnd;
    }

    void TryConsumePress()
    {
        /* A) idle  →  start combo */
        if (comboIndex == 0 && !anim.IsInTransition(0))
        {
            queuedPress = false;
            comboIndex = 1;
            anim.SetInteger("ComboIndex", comboIndex);
            anim.SetTrigger("AttackTrig");          // ← only here!

            movement?.LockMovement(true);
            lockActive = true;
            return;
        }

        /* B) mid-combo  →  queue next swing */
        if (IsInAttackState() && IsWithinComboWindow() && comboIndex < 3)
        {
            queuedPress = false;
            comboIndex += 1;                        // 2 or 3
            anim.SetInteger("ComboIndex", comboIndex);
            /* no trigger — FSM will switch at clip's exit time */
        }
    }
}
