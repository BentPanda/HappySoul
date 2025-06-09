using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StaminaBar : MonoBehaviour
{
    [Header("UI References")]
    public Image staminaFill;    // the green bar (on top)
    public Image staminaUsed;    // the orange bar (behind)

    [Header("Lag Settings")]
    [Tooltip("How fast (in fill/sec) the orange bar shrinks once it starts.")]
    public float usedShrinkSpeed = 0.5f;

    [Tooltip("Delay (seconds) after stamina use before orange bar begins shrinking.")]
    public float usedShrinkDelay = 0.5f;

    Coroutine shrinkCoroutine;

    /// <summary>
    /// Call every frame with current / max.
    /// </summary>
    public void UpdateStamina(float current, float max)
    {
        float pct = Mathf.Clamp01(current / max);

        // 1) update green instantly
        staminaFill.fillAmount = pct;

        // 2) if orange is ahead, schedule a delayed shrink
        if (staminaUsed.fillAmount > pct)
        {
            if (shrinkCoroutine != null)
                StopCoroutine(shrinkCoroutine);
            shrinkCoroutine = StartCoroutine(DelayedShrink(pct));
        }
        else
        {
            // cancel any pending shrink and snap if regen overshoots
            if (shrinkCoroutine != null)
            {
                StopCoroutine(shrinkCoroutine);
                shrinkCoroutine = null;
            }
            staminaUsed.fillAmount = pct;
        }
    }

    IEnumerator DelayedShrink(float target)
    {
        // wait for the configured delay
        yield return new WaitForSeconds(usedShrinkDelay);

        // then shrink the orange bar down to match the green
        while (staminaUsed.fillAmount > target + 0.001f)
        {
            staminaUsed.fillAmount -= usedShrinkSpeed * Time.deltaTime;
            yield return null;
        }
        staminaUsed.fillAmount = target;
        shrinkCoroutine = null;
    }
}
