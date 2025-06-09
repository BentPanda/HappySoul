using UnityEngine;

/// Simple damage trigger.  Add a collider, tick "Is Trigger".
public class DamageBox : MonoBehaviour
{
    public int damage = 10;
    public bool heavy = false;   // tick this on the heavy-damage cube

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.TakeDamage(damage, heavy);
    }
}
