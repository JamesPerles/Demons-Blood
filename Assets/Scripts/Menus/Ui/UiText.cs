using UnityEngine;
using TMPro;
[RequireComponent(typeof(TextMeshProUGUI))]
public class UIText : MonoBehaviour
{
TextMeshProUGUI text;
void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }
    void OnEnable()
    {
        if(SettingsManager.instance != null)
        {
            ApplyColor(SettingsManager.instance.uiTextColor);
            SettingsManager.instance.OnTextColorChanged += ApplyColor;
        }
    }
    void OnDisable()
    {
        if(SettingsManager.instance != null) SettingsManager.instance.OnTextColorChanged -= ApplyColor;
    }
    void ApplyColor(Color color)
    {
        if(text != null) text.color = color;
    }
}
