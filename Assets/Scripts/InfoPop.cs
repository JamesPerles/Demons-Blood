using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
public class InfoPop : MonoBehaviour
{
public CanvasGroup canvasGroup;
public Image borderImage;
public Image backgroundImage;
public Image icon;
public TextMeshProUGUI titleText;
public float fadeDuration = 0.25f;
public void Setup(string title, Sprite iconSprite)
    {
        if(titleText != null) titleText.text = title;
        if(icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
        }
    }
public IEnumerator FadeIn()
    {
        if(canvasGroup == null) yield break;
        canvasGroup.alpha = 0f;
        float durationTime = 0f;
        while(durationTime < fadeDuration)
        {
            durationTime += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, durationTime / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
    public IEnumerator FadeOutAndDestroy()
    {
        if(canvasGroup == null)
        {
            Destroy(gameObject);
            yield break;
        }
        float startAlpha = canvasGroup.alpha;
        float durationTime = 0f;
        while(durationTime < fadeDuration)
        {
            durationTime += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, durationTime / fadeDuration);
            yield return null;
        }
        Destroy(gameObject);
    }
}
