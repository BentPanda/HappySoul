using UnityEngine;
using System.Collections.Generic;

/// Trigger is active for the whole duration of *any* state tagged “Attack”.
[RequireComponent(typeof(Collider))]
public class WeaponDamage : MonoBehaviour
{
    [Header("References")]
    public Animator playerAnim;   // drag the same Animator the player uses
    public PlayerCombat owner;        // drag Player root (for combo stage if you need it)

    [Header("Damage")]
    public int damagePerHit = 10;

    Collider hitbox;
    HashSet<EnemyHealth> alreadyHit = new HashSet<EnemyHealth>();

    void Awake()
    {
        hitbox = GetComponent<Collider>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;     // off until first swing
    }

    void Update()
    {
        bool inAttack =
            playerAnim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") ||
           (playerAnim.IsInTransition(0) && playerAnim.GetNextAnimatorStateInfo(0).IsTag("Attack"));

        /* Toggle the trigger when we enter/leave an Attack state */
        if (inAttack && !hitbox.enabled)
        {
            hitbox.enabled = true;
            alreadyHit.Clear();             // new list for this swing
        }
        else if (!inAttack && hitbox.enabled)
        {
            hitbox.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hitbox.enabled) return;

        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh == null || alreadyHit.Contains(eh)) return;

        eh.TakeHit(damagePerHit, false, transform.position);   // ‘false’ = light hit
        alreadyHit.Add(eh);
    }
}
