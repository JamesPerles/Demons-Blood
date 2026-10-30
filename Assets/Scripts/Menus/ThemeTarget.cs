using UnityEngine;
using UnityEngine.UI;
public enum ThemeRole { Panel, Border, Text }
public class ThemeTarget : MonoBehaviour
{
    public ThemeRole role = ThemeRole.Panel;
    Graphic graphic;
    RectTransform rect;
    bool subscribed;
void Awake()
    {
    graphic = GetComponent<Graphic>();
    rect = transform as RectTransform;
    }
    void OnEnable()
    {
       TryHook();
    }
    void Start()
    {
        TryHook();
    }
    void TryHook()
    {
        if(SettingsManager.instance == null) return;
        Apply();
        if(subscribed) return;
        if(role == ThemeRole.Text) SettingsManager.instance.OnTextColorChanged += OnTextColorChanged;
        else SettingsManager.instance.OnMenuThemeChanged += Apply;
        subscribed = true;
    }
    void OnDisable()
    {
        Unhook();
    }
    void OnDestroy()
    {
    Unhook();
    }
    void Unhook()
    {
        if(!subscribed) return;
        subscribed = false;
        if(SettingsManager.instance == null) return;
        if(role == ThemeRole.Text) SettingsManager.instance.OnTextColorChanged -= OnTextColorChanged;
        else SettingsManager.instance.OnMenuThemeChanged -= Apply;
    }
    void OnTextColorChanged(Color color)
    {
        Apply();        
    }
    public void Apply()
    {
        if(SettingsManager.instance == null || graphic == null) return;
        switch(role)
        {
            case ThemeRole.Text:
            graphic.color = SettingsManager.instance.uiTextColor;
            break;
            case ThemeRole.Border:
            graphic.color = SettingsManager.instance.menuBorderColor;
            break;
            case ThemeRole.Panel:
            graphic.color = SettingsManager.instance.menuPanelColor;
            break;
        }
    }
}
