using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class BattleTextBox : MonoBehaviour
{
public static BattleTextBox instance;
public GameObject messagePrefab;
public Transform contentParent;
public GameObject boxPanel;
public float charactersPerSecond = 40f;
public float messageDuration = 1.5f;
public KeyCode advanceKey = KeyCode.Space;
public float disappearDistance = 60f;
public float disappearDuration = 0.5f;
List<Coroutine> activeFades = new List<Coroutine>();
void Awake()
    {
        if(instance == null) instance = this; else Destroy(gameObject);
    if(contentParent != null)
        {
            RectMask2D mask = contentParent.GetComponent<RectMask2D>();
            if(mask == null) contentParent.gameObject.AddComponent<RectMask2D>();
        }
    }
    public IEnumerator ShowMessage(string message)
    {
        if (boxPanel != null) boxPanel.SetActive(true);
        GameObject entry = Instantiate(messagePrefab, contentParent);
        RectTransform rect = entry.GetComponent<RectTransform>();
        if(rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
        }
        TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
        if(text == null) text = entry.GetComponentInChildren<TextMeshProUGUI>();
        text.text = "";
        float perSecond = SettingsManager.instance != null ? SettingsManager.instance.battleTextSpeed : charactersPerSecond;
        for(int i = 0; i < message.Length; i++)
        {
            if(Input.GetKeyDown(advanceKey)) break;
            text.text += message[i];
            yield return new WaitForSeconds(1f / Mathf.Max(1f, perSecond));
        }
        text.text = message;
        float speed = SettingsManager.instance != null ? SettingsManager.instance.battleSpeedMultiplier : 1f;
        float duration = messageDuration / Mathf.Max(0.1f, speed);
        float elapsed = 0f;
        while(elapsed < duration)
        {
            if(Input.GetKeyDown(advanceKey)) break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Coroutine fade = StartCoroutine(DisappearAndFadeOut(entry));
    activeFades.Add(fade);
    }
    IEnumerator DisappearAndFadeOut(GameObject entry)
    {
        if(entry == null) yield break;
        RectTransform rect = entry.GetComponent<RectTransform>();
        TextMeshProUGUI text = entry.GetComponent<TextMeshProUGUI>();
        if(text == null) text = entry.GetComponentInChildren<TextMeshProUGUI>();
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * disappearDistance;
        Color startColor = text.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        float elapsed = 0f;
        while(elapsed < disappearDuration)
        {
            float timing = elapsed / disappearDuration;
            float eased = 1f -Mathf.Pow(1f - timing, 3f);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            text.color = Color.Lerp(startColor, endColor, eased);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(entry);
    }
    public void Clear()
    {
        foreach(var fade in activeFades) if(fade != null) StopCoroutine(fade);
        activeFades.Clear();
        if(contentParent != null)
        {
            foreach (Transform child in contentParent)
            Destroy(child.gameObject);
        }
        if(boxPanel != null) boxPanel.SetActive(false);
    }
}
