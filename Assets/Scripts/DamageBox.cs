using UnityEngine;

/// Cycles 1 s “armed” → 1 s idle, repeating.
/// When armed it swaps to `armedMat` and damages the player on trigger-enter.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class DamageBox : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 10;
    public bool heavy = false;

    [Header("Materials")]
    public Material normalMat;   // drag your idle material here
    public Material armedMat;    // drag your glowing / red material here

    /* ───────── private ───────── */
    Renderer rend;
    float phaseTimer = 0f;
    bool armed = false;

    void Awake()
    {
        // make sure we’re a trigger
        GetComponent<Collider>().isTrigger = true;

        rend = GetComponent<Renderer>();
        SetArmed(false);                      // start idle
    }

    void Update()
    {
        phaseTimer += Time.deltaTime;

        bool shouldBeArmed = phaseTimer % 2f < 1f;   // 0-1 s armed, 1-2 s idle

        if (shouldBeArmed != armed)
            SetArmed(shouldBeArmed);
    }

    void SetArmed(bool on)
    {
        armed = on;
        if (armedMat != null && normalMat != null)
            rend.material = on ? armedMat : normalMat;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!armed) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damage, heavy);
    }
}
