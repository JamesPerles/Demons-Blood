using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class SettingsMenu : MonoBehaviour, ICardHighlightHandler, ITabVisualOwner
{
public PauseMenu host;
public GameObject categoryCardPrefab;
public Transform categoryCardParent;
public GameObject sliderRowPrefab;
public GameObject colorPreviewPrefab;
public Color cardBorderDefault = new Color32(0x3A, 0x16, 0x16, 0xFF);
public Color cardBorderSelected = new Color32(0xD8, 0x5A, 0x30, 0xFF);
public Color cardBackgroundSelected = new Color32(0x24, 0x10, 0x10, 0xFF);
public Color cardTitleDefault = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
public Color cardTitleSelected = new Color32(0xF2, 0xF2, 0xF2, 0xFF);

public Transform speedGroup;
public Transform soundsGroup;
public Transform menusGroup;
public Transform textGroup;
enum Category {Speed, Sounds, Menus, Text};
List<GameObject> spawnedCards = new List<GameObject>();
List<GameObject> spawnedRows = new List<GameObject>();
Image panelColorPreviewImage;
Image borderColorPreviewImage;
Image textColorPreviewImage;
public void OpenSettings()
    {
        if(categoryCardParent != null) categoryCardParent.gameObject.SetActive(true);
        host.ShowSplitPanel();
        host.SetBreadcrumbSuffix("Misc > Settings");
        host.SetCardHighlightHandler(this);
        if(host.detailText != null) host.detailText.gameObject.SetActive(false);
        BuildCategoryCards();
    }
    public void HideVisuals()
    {
        if(categoryCardParent != null) categoryCardParent.gameObject.SetActive(false);
        HideAllGroups();
        if(host.detailText != null) host.detailText.gameObject.SetActive(true);
    }
    void BuildCategoryCards()
    {
        foreach(GameObject card in spawnedCards) Destroy(card);
        spawnedCards.Clear();
        if(categoryCardPrefab == null || categoryCardParent == null) return;
        AddCategoryCard("Speed", "Text & Battle Speed");
        AddCategoryCard("Sounds", "Music & SFX Volume");
        AddCategoryCard("Menus", "Panel and Border Style");
        AddCategoryCard("Text", "UI Text Color");
        if(spawnedCards.Count > 0) host.EntryHighlight(spawnedCards[0]);
    }
    void AddCategoryCard(string title, string subtitle)
    {
        GameObject cardObj = Instantiate(categoryCardPrefab, categoryCardParent);
        EntryCard card = cardObj.GetComponent<EntryCard>();
        spawnedCards.Add(cardObj);
        if(card == null) return;
        if(card.titleText != null) card.titleText.text = title;
        if(card.subText != null) card.subText.text = subtitle;
        MenuOption option = new MenuOption(title, () => { });
        host.RegisterEntry(cardObj, option);
        if(card.button != null)
        {
            card.button.onClick.RemoveAllListeners();
            GameObject capturedCard = cardObj;
            card.button.onClick.AddListener(() => host.EntryHighlight(capturedCard));
        } 
    }
    public void OnCardHighlighted(GameObject entry)
    {
        int index = spawnedCards.IndexOf(entry);
        for(int i = 0; i < spawnedCards.Count; i++)
        {
            EntryCard card = spawnedCards[i].GetComponent<EntryCard>();
            if(card == null) continue;
            SetCardVisual(card, spawnedCards[i] == entry);
        }
        if(index >= 0) SelectCategory((Category)index);
    }
    void SetCardVisual(EntryCard card, bool selected)
    {
        if(card.borderImage != null) card.borderImage.color = selected ? cardBorderSelected : cardBorderDefault;
        if(card.backgroundImage != null)
        {
            Color bg = cardBackgroundSelected;
            bg.a = selected ? 1f : 0f;
            card.backgroundImage.color = bg;
        }
        if(card.titleText != null) card.titleText.color = selected ? cardTitleSelected : cardTitleDefault;
    }
    void SelectCategory(Category category)
    {
        HideAllGroups();
        if(SettingsManager.instance == null) return;
        switch(category)
        {
            case Category.Speed: ShowSpeedGroup(); break;
            case Category.Sounds: ShowSoundsGroup(); break;
            case Category.Menus: ShowMenusGroup(); break;
            case Category.Text: ShowTextGroup(); break;
        }
    }
    void HideAllGroups()
    {
        ClearRows();
        if(speedGroup != null) speedGroup.gameObject.SetActive(false);
        if(soundsGroup != null) soundsGroup.gameObject.SetActive(false);
        if(menusGroup != null) menusGroup.gameObject.SetActive(false);
        if(textGroup != null) textGroup.gameObject.SetActive(false);
    }
    void ClearRows()
    {
        foreach(GameObject row in spawnedRows) Destroy(row);
        spawnedRows.Clear();
    }
    void ShowSpeedGroup()
    {
        if(speedGroup == null) return;
        speedGroup.gameObject.SetActive(true);
        SettingsManager settings = SettingsManager.instance;
        SpawnSliderRow(speedGroup, "Dialogue Speed", 10f, 100f, settings.dialogueTextSpeed, variable => settings.dialogueTextSpeed = variable, variable => variable.ToString("0"));
        SpawnSliderRow(speedGroup, "Battle Text Speed", 10f, 100f, settings.battleTextSpeed, variable => settings.battleTextSpeed = variable, variable => variable.ToString("0"));
        SpawnSliderRow(speedGroup, "Battle Speed", 0.5f, 2.5f, settings.battleSpeedMultiplier, variable => settings.battleSpeedMultiplier = variable, variable => $"{variable:0.0}x");
    }
    void ShowSoundsGroup()
    {
        if(soundsGroup == null) return;
        soundsGroup.gameObject.SetActive(true);
        SettingsManager settings = SettingsManager.instance;
        SpawnSliderRow(soundsGroup, "Music Volume", 0f, 1f, settings.musicVolume, variable => settings.SetMusicVolume(variable), variable => $"{Mathf.RoundToInt(variable * 100)}%");
        SpawnSliderRow(soundsGroup, "SFX Volume", 0f, 1f, settings.sfxVolume, variable => settings.SetSfxVolume(variable), variable => $"{Mathf.RoundToInt(variable * 100)}%");
    }
void ShowMenusGroup()
    {
        if(menusGroup == null) return;
        menusGroup.gameObject.SetActive(true);
        SettingsManager settings = SettingsManager.instance;
        Color panel = settings.menuPanelColor;
        panelColorPreviewImage = SpawnColorPreview(menusGroup, panel);
        SpawnSliderRow(menusGroup, "Panel Red", 0f, 1f, panel.r, variable => SetPanelChannel(0, variable), null);
        SpawnSliderRow(menusGroup, "Panel Green", 0f, 1f, panel.g, variable => SetPanelChannel(1, variable), null);
        SpawnSliderRow(menusGroup, "Panel Blue", 0f, 1f, panel.b, variable => SetPanelChannel(2, variable), null);
        SpawnSliderRow(menusGroup, "Panel Opacity", 0f, 1f, panel.a, variable => SetPanelChannel(3, variable), null);
        Color border = settings.menuBorderColor;
        borderColorPreviewImage = SpawnColorPreview(menusGroup, border);
        SpawnSliderRow(menusGroup, "Border Red", 0f, 1f, border.r, variable => SetBorderChannel(0, variable), null);
        SpawnSliderRow(menusGroup, "Border Green", 0f, 1f, border.g, variable => SetBorderChannel(1, variable), null);
        SpawnSliderRow(menusGroup, "Border Blue", 0f, 1f, border.b, variable => SetBorderChannel(2, variable), null);
        SpawnSliderRow(menusGroup, "Border Thickness", 1f, 10f, settings.menuBorderThickness, variable => settings.SetMenuBorderThickness(variable), variable =>$"{variable:0}px");
    }
    void ShowTextGroup()
    {
        if(textGroup == null) return;
        textGroup.gameObject.SetActive(true);
        SettingsManager settings = SettingsManager.instance;
        Color text = settings.uiTextColor;
        textColorPreviewImage = SpawnColorPreview(textGroup, text);
        SpawnSliderRow(textGroup, "Text Red", 0f, 1f, text.r, variable => SetTextChannel(0, variable), null);
        SpawnSliderRow(textGroup, "Text Green", 0f, 1f, text.g, variable => SetTextChannel(1, variable), null);
        SpawnSliderRow(textGroup, "Text Blue", 0f, 1f, text.b, variable => SetTextChannel(2, variable), null);
    }
    void SetPanelChannel(int channel, float value)
    {
        Color color = SettingsManager.instance.menuPanelColor;
        if(channel == 0) color.r = value; else if(channel == 1) color.g = value; else if(channel == 2) color.b = value; else color.a = value;
        SettingsManager.instance.SetMenuPanelColor(color);
        if(panelColorPreviewImage != null) panelColorPreviewImage.color = color;
    }
    void SetBorderChannel(int channel, float value)
    {
        Color color = SettingsManager.instance.menuBorderColor;
        if(channel == 0) color.r = value; else if(channel == 1) color.g = value; else color.b = value;
        SettingsManager.instance.SetMenuBorderColor(color);
        if(borderColorPreviewImage != null) borderColorPreviewImage.color = color;
    }
    void SetTextChannel(int channel, float value)
    {
        Color color = SettingsManager.instance.uiTextColor;
        if(channel == 0) color.r = value; else if(channel == 1) color.g = value; else color.b = value;
        SettingsManager.instance.SetTextColor(color);
        if(textColorPreviewImage != null) textColorPreviewImage.color = color;
    }
    void SpawnSliderRow(Transform parent, string label, float min, float max, float initial, System.Action<float> onChanged, System.Func<float, string> formatter)
    {
        if(sliderRowPrefab == null || parent == null) return;
        GameObject rowObj = Instantiate(sliderRowPrefab, parent);
        spawnedRows.Add(rowObj);
        SettingRowView view = rowObj.GetComponent<SettingRowView>();
        if(view == null) return;
        if(view.labelText != null) view.labelText.text = label;
        WireSlider(view.slider, min, max, initial, onChanged, view.valueText, formatter);
    }
    Image SpawnColorPreview(Transform parent, Color initial)
    {
        if(colorPreviewPrefab == null || parent == null) return null;
        GameObject obj = Instantiate(colorPreviewPrefab, parent);
        spawnedRows.Add(obj);
        Image img = obj.GetComponent<Image>();
        if(img != null) img.color = initial;
        return img;
    }
    void WireSlider(Slider slider, float min, float max, float initial, System.Action<float> onChanged, TextMeshProUGUI valueText, System.Func<float, string> formatter)
    {
        if(slider == null) return;
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(initial);
        bool showValue = valueText != null && formatter != null;
        if(valueText != null) valueText.gameObject.SetActive(showValue);
        if(showValue) valueText.text = formatter(initial);
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(variable => {onChanged(variable); if(valueText != null && formatter != null) valueText.text = formatter(variable);
        });
    }
}