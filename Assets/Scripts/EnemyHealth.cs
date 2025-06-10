using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 30;

    [Header("Knock-back")]
    public float knockLight = 0.40f;
    public float knockHeavy = 1.00f;

    /* ───────── private ───────── */
    int hp;
    bool dead;
    Animator anim;
    Collider[] colliders;
    Rigidbody rb;            // optional, only if your enemy uses one

    void Awake()
    {
        hp = maxHP;
        anim = GetComponent<Animator>();
        colliders = GetComponentsInChildren<Collider>();
        rb = GetComponent<Rigidbody>();   // may be null
    }

    /// <summary>
    /// Called by the player's WeaponDamage script.
    /// </summary>
    public void TakeHit(int dmg, bool heavy, Vector3 hitOrigin)
    {
        if (dead) return;

        hp = Mathf.Max(0, hp - dmg);
        Debug.Log($"{name}  –  took {dmg} dmg  →  HP = {hp}");

        /* ── 1.  Already dead? trigger Die and EXIT ── */
        if (hp == 0)
        {
            dead = true;
            Debug.Log($"{name}  –  HP reached 0, triggering Die");

            // make sure no stagger triggers are pending this frame
            anim.ResetTrigger("HurtTrig");
            anim.ResetTrigger("HeavyHurtTrig");
            anim.SetTrigger("DieTrig");

            StartCoroutine(DieSequence());
            return;                         // <-- skip Hurt completely
        }

        /* ── 2.  Still alive → play stagger animation ── */
        anim.SetTrigger(heavy ? "HeavyHurtTrig" : "HurtTrig");

        // optional positional knock-back
        Vector3 dir = (transform.position - hitOrigin); dir.y = 0;
        float dist = heavy ? knockHeavy : knockLight;
        if (dir.sqrMagnitude > 0.001f)
            transform.position += dir.normalized * dist;
    }

    /* freezes the corpse and disables hitboxes */
    IEnumerator DieSequence()
    {
        /* wait until Animator has actually entered the state tagged "Dead" */
        yield return new WaitUntil(() =>
            anim.GetCurrentAnimatorStateInfo(0).IsTag("Dead"));

        Debug.Log($"{name}  –  Die state entered, disabling colliders");

        foreach (var c in colliders) c.enabled = false;
        if (rb) rb.isKinematic = true;

        /* No need to stop Animator – Die clip has Loop Time off and
           the state has NO outgoing transitions, so it will stay on
           its last frame forever. */
    }
}
