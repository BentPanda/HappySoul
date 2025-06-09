using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Image healthFill;   // bright red
    public Image healthUsed;   // dark red

    [Header("Lag Settings")]
    public float usedShrinkSpeed = 0.5f;
    public float usedShrinkDelay = 0.5f;

    Coroutine shrinkCo;

    public void UpdateHealth(int current, int max)
    {
        float pct = Mathf.Clamp01((float)current / max);

        healthFill.fillAmount = pct;

        if (healthUsed.fillAmount > pct)
        {
            if (shrinkCo != null) StopCoroutine(shrinkCo);
            shrinkCo = StartCoroutine(DelayedShrink(pct));
        }
        else
        {
            if (shrinkCo != null) { StopCoroutine(shrinkCo); shrinkCo = null; }
            healthUsed.fillAmount = pct;
        }
    }

    IEnumerator DelayedShrink(float target)
    {
        yield return new WaitForSeconds(usedShrinkDelay);
        while (healthUsed.fillAmount > target + 0.001f)
        {
            healthUsed.fillAmount -= usedShrinkSpeed * Time.deltaTime;
            yield return null;
        }
        healthUsed.fillAmount = target;
        shrinkCo = null;
    }
}
