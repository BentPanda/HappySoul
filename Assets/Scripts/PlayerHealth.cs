using UnityEngine;

/// 100-HP health component with light / heavy hit-stun.
/// Assumes: • Animator has HurtTrig & HeavyHurtTrig
///          • Hurt / HeavyHurt states are tagged "Hurt"
[RequireComponent(typeof(Animator))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Config")]
    public int maxHP = 100;

    [Header("UI")]
    public HealthBar healthBar;

    [Header("Refs")]
    public PlayerMovement movement;      // drag in Inspector

    int currentHP;
    bool lockActive = false;

    Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
        currentHP = maxHP;
        healthBar.UpdateHealth(currentHP, maxHP);
    }

    /* ───── public API ───── */
    public void TakeDamage(int amount, bool heavy)
    {
        if (currentHP <= 0) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        healthBar.UpdateHealth(currentHP, maxHP);

        anim.SetTrigger(heavy ? "HeavyHurtTrig" : "HurtTrig");

        movement?.LockMovement(true);
        lockActive = true;

        if (currentHP == 0)
            Die();
    }

    /* ───── unlock when reaction clip ends ───── */
    void Update()
    {
        if (lockActive && !IsInHurtState())
        {
            lockActive = false;
            movement?.LockMovement(false);
        }
    }

    bool IsInHurtState()
    {
        return anim.GetCurrentAnimatorStateInfo(0).IsTag("Hurt") ||
              (anim.IsInTransition(0) && anim.GetNextAnimatorStateInfo(0).IsTag("Hurt"));
    }

    void Die()
    {
        // TODO: play death, respawn, etc.
        Debug.Log("Player is dead");
    }
}
