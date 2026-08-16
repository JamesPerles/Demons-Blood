using UnityEngine;
using System.Collections;
public class HitFlicker : MonoBehaviour
{
public SpriteRenderer[] spriteRenderers;
public int flickerCount = 6;
public float flickerInterval = 0.05f;
Coroutine activeFlicker;
void Awake()
    {
        if(spriteRenderers == null || spriteRenderers.Length == 0)
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }
    public void Flicker()
    {
        if(activeFlicker != null) StopCoroutine(activeFlicker);
        activeFlicker = StartCoroutine(FlickerRoutine());
    }
    IEnumerator FlickerRoutine()
    {
        for (int i = 0; i < flickerCount; i++)
        {
            SetVisible(false);
            yield return new WaitForSeconds(flickerInterval);
            SetVisible(true);
            yield return new WaitForSeconds(flickerInterval);
        }
        activeFlicker = null;
    }
    void SetVisible(bool visible)
    {
        foreach (var sprite in spriteRenderers) if(sprite != null) sprite.enabled = visible;
    }
}
