using UnityEngine;
using UnityEngine.UI;
public class MenuPanelTheme : MonoBehaviour
{
public Image borderImage;
public RectTransform fillRect;
public Image fillImage;
void Start()
    {
        if(SettingsManager.instance != null) SettingsManager.instance.OnMenuThemeChanged += Apply;
        Apply();
    }
    void OnEnable()
    {
    Apply();        
    }
    void OnDestroy()
    {
    if(SettingsManager.instance != null) SettingsManager.instance.OnMenuThemeChanged -= Apply;        
    }
    public void Apply()
    {
        if(SettingsManager.instance == null) return;
        if(borderImage != null) borderImage.color = SettingsManager.instance.menuBorderColor;
        if(fillImage != null) fillImage.color = SettingsManager.instance.menuPanelColor;
        if(fillRect != null)
        {
            float thickness = SettingsManager.instance.menuBorderThickness;
            fillRect.offsetMin = new Vector2(thickness, thickness);
            fillRect.offsetMax = new Vector2(-thickness, -thickness);
        }
    }
}
