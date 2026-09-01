using UnityEngine;
using TMPro;
using System.Collections.Generic;
public class MiscellaneousMenu : MonoBehaviour, ICardHighlightHandler, ITabVisualOwner
{
    public PauseMenu host;
    public GameObject bestiaryCardPrefab;
    public Transform bestiaryCardParent;
    public GameObject saveSlotCardPrefab;
    public Transform saveSlotCardParent;
    public int maxSaveSlots = 1;
    public SettingsMenu settingsController;
    public Color cardBorderDefault = new Color32(0x3A, 0x16, 0x16, 0xFF);
    public Color cardBorderSelected = new Color32(0xD8, 0x5A, 0x30, 0xFF);
    public Color cardBackgroundSelected = new Color32(0x24, 0x10, 0x10, 0xFF);
    public Color cardTitleDefault = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
    public Color cardTitleSelected = new Color32(0xF2, 0xF2, 0xF2, 0xFF);
    const string colorMuted = "#8A8580";
    const string colorBody = "#C9C2C2";
    const string colorBright = "#E8E4E0";
    const string colorDone = "#639922";
    const string dot = "\u00B7";
    List<GameObject> spawnedCards = new List<GameObject>();
    public void OpenTab()
    {
        host.PrepareTabSwitch();
        SetupMiniTabs();
    }
     public void HideVisuals()
    {
        if(bestiaryCardParent != null) bestiaryCardParent.gameObject.SetActive(false);
        if(saveSlotCardParent != null) saveSlotCardParent.gameObject.SetActive(false);
        if(settingsController != null) settingsController.HideVisuals();
    }
    void SetupMiniTabs()
    {
        if(host.miniTabGroup == null) return;
        host.miniTabGroup.Show();
        List<TabDefinition> tabs = new List<TabDefinition>
        {
            new TabDefinition("Bestiary", ShowBestiaryTab),
            new TabDefinition("Settings", ShowSettingsTab),
             new TabDefinition("Save", ShowSaveTab),
            new TabDefinition("Load", ShowLoadTab),
            new TabDefinition("Quit", QuitGame), 
        };
        host.miniTabGroup.SetTabs(tabs, 0);
    }
    void ShowBestiaryTab()
    {
        host.ClearMenuEntries();
        host.ClearScreenHistory();
        if(bestiaryCardParent != null) bestiaryCardParent.gameObject.SetActive(true);
        if(saveSlotCardParent != null) saveSlotCardParent.gameObject.SetActive(false);
        if(settingsController != null) settingsController.HideVisuals();
        host.ShowSplitPanel();
        host.SetBreadcrumbSuffix("Misc > Bestiary");
        host.SetCardHighlightHandler(this);
        RebuildBestiaryCards();
    }
    void ShowSettingsTab()
    {
        if(bestiaryCardParent != null) bestiaryCardParent.gameObject.SetActive(false);
        if(saveSlotCardParent != null) saveSlotCardParent.gameObject.SetActive(false);
        if(settingsController != null) settingsController.OpenSettings();
    }
    void ShowSaveTab()
    {
       if(bestiaryCardParent != null) bestiaryCardParent.gameObject.SetActive(false);
       if(saveSlotCardParent != null) saveSlotCardParent.gameObject.SetActive(true);
       if(settingsController != null) settingsController.HideVisuals();
       host.ShowSplitPanel();
       host.SetBreadcrumbSuffix("Misc > Save");
       host.SetCardHighlightHandler(this);
       RebuildSlotCards(true); 
    }
   void ShowLoadTab()
    {
       if(bestiaryCardParent != null) bestiaryCardParent.gameObject.SetActive(false);
       if(saveSlotCardParent != null) saveSlotCardParent.gameObject.SetActive(true);
       if(settingsController != null) settingsController.HideVisuals();
       host.ShowSplitPanel();
       host.SetBreadcrumbSuffix("Misc > Load");
       host.SetCardHighlightHandler(this);
       RebuildSlotCards(false); 
    }
    void RebuildSlotCards(bool saveMode)
    {
        foreach(GameObject card in spawnedCards) Destroy(card);
        spawnedCards.Clear();
        if(saveSlotCardPrefab == null || saveSlotCardParent == null || SaveManager.instance == null) return;
        for(int slot = 0; slot < maxSaveSlots; slot++)
        {
            SaveManager.SaveSlotSummary summary = SaveManager.instance.GetSlotSummary(slot);
            GameObject cardObj = Instantiate(saveSlotCardPrefab, saveSlotCardParent);
            QuestCardView view = cardObj.GetComponent<QuestCardView>();
            spawnedCards.Add(cardObj);
            if(view == null) continue;
            string title = $"Slot {slot + 1}";
            string subText = summary.exists ? $"{summary.leadCharacterName} Lv.{summary.leadCharacterLevel}" : "Empty";
            if(view.titleText != null) view.titleText.text = title;
            if(view.subText != null) view.subText.text = subText;
            int capturedSlot = slot;
            MenuOption option = new MenuOption(title, () => { }) {description = BuildSlotDetail(summary, slot) };
            host.RegisterEntry(cardObj, option);
            bool clickable = saveMode || summary.exists;
            if(view.button != null)
            {
                view.button.interactable = clickable;
                view.button.onClick.RemoveAllListeners();
                GameObject capturedCard = cardObj;
                view.button.onClick.AddListener(() =>
                {
                    host.EntryHighlight(capturedCard);
                    if(saveMode) ConfirmSaveSlot(capturedSlot);
                    else ConfirmLoadSlot(capturedSlot);
                });
            }
            SetCardVisual(view, false);
        }
        if(spawnedCards.Count > 0) host.EntryHighlight(spawnedCards[0]);
     }
    string BuildSlotDetail(SaveManager.SaveSlotSummary summary, int slot)
    {
        string title = $"Slot {slot + 1}";
        if(!summary.exists)
        {
            return $"<size=140%><color=#F2F2F2>{title}</color></size>\n<size=80%><color={colorMuted}>empty</color></size>\n\n<color={colorBody}>No save data here yet.</color>";
        }
        string result = $"<size=140%><color=#F2F2F2>{title}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>saved {summary.savedAt}</color></size>\n\n";
        result += $"<color={colorBody}>{summary.leadCharacterName} Lv.{summary.leadCharacterLevel} {dot} {summary.gold} gold</color>\n";
        result += $"<color={colorMuted}>{summary.sceneName}</color>";
        return result;
    }
    void ConfirmSaveSlot(int slot)
    {
        SaveManager.instance.SaveGame(slot);
        RebuildSlotCards(true);
    }
    void ConfirmLoadSlot(int slot)
    {
        if(!SaveManager.instance.SaveExists(slot)) return;
        host.Close();
        SaveManager.instance.LoadGame(slot);
    }
    void RebuildBestiaryCards()
    {
        foreach(GameObject card in spawnedCards) Destroy(card);
        spawnedCards.Clear();
        if(bestiaryCardPrefab == null || bestiaryCardParent == null || BestiaryManager.instance == null) return;
        foreach(EnemyStats enemy in BestiaryManager.instance.allEnemies)
        {
            if(enemy == null) continue;
            GameObject cardObj = Instantiate(bestiaryCardPrefab, bestiaryCardParent);
            QuestCardView view = cardObj.GetComponent<QuestCardView>();
            spawnedCards.Add(cardObj);
            if(view == null) continue;
            bool discovered = BestiaryManager.instance.IsDiscovered(enemy);
            string title = discovered ? enemy.enemyName : "? ? ?";
            if(view.titleText != null) view.titleText.text = title;
            if(view.subText != null) view.subText.text = "";
            MenuOption option = new MenuOption(title, () => { }) {description = BuildBestiaryDetail(enemy, discovered)};
            host.RegisterEntry(cardObj, option);
            if(view.button != null)
            {
                view.button.onClick.RemoveAllListeners();
                GameObject capturedCard = cardObj;
                view.button.onClick.AddListener(() => host.EntryHighlight(capturedCard));
            }
            SetCardVisual(view, false);
        }
        if(spawnedCards.Count > 0) host.EntryHighlight(spawnedCards[0]);
        else if(host.detailText != null) host.detailText.text = "No enemies known yet";
    }
    string BuildBestiaryDetail(EnemyStats enemy, bool discovered)
    {
        if(!discovered)
        {
            return $"<size=140%><color=#F2F2F2>? ? ?</color></size>\n<size=80%><color={colorMuted}>not yet encountered</color></size>";
        }
        string result = $"<size=140%><color=#F2F2F2>{enemy.enemyName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>Lv. {enemy.level}</color></size>\n";
        result += $"<color={colorBody}>{enemy.dexEntry}</color>\n\n";
        result += $"<size=75%><color={colorMuted}>STATS</color></size>\n";
        result += $"<color={colorBright}>HP {enemy.hp}{dot} MP {enemy.mp} {dot} STR {enemy.strength} {dot} MAG {enemy.magic} {dot} DEF{enemy.defense} {dot} WIS {enemy.wisdom} {dot} TEC {enemy.tech} {dot} AFF {enemy.affinity} {dot} SPD {enemy.speed} {dot} LCK {enemy.luck}</color>";
        if(enemy.magicAffinity != Element.None)
        {
            result += $"<size=75%><color={colorMuted}>AFFINITY</color></size>\n";
            result += $"<color={colorBright}>{enemy.magicAffinity}</color>\n\n";
        }
        if(enemy.immunities != null && enemy.immunities.Count > 0)
        {
            List<string> immuneNames = new List<string>();
            foreach(StatusEffect status in enemy.immunities) if(status != null) immuneNames.Add(status.name);
            result += $"<size=75%><color={colorMuted}>IMMUNE TO</color></size>\n";
            result += $"<color={colorBright}>{string.Join(", ", immuneNames)}</color>\n\n";
        }
        if(enemy.lootTable != null && enemy.lootTable.Count > 0)
        {
             result += $"<size=75%><color={colorMuted}>DROPS</color></size>\n";
             foreach(EnemyStats.LootDrop drop in enemy.lootTable)
            {
                if(drop == null || drop.item == null) continue;
                result += $"<color={colorBright}>{drop.item.itemName}</color> <color={colorDone}>{drop.dropChance}%</color>\n";
            }
            result += "\n";
        }
        if(enemy.stealTable != null && enemy.lootTable.Count > 0)
        {
             result += $"<size=75%><color={colorMuted}>STEALABLE</color></size>\n";
             foreach(EnemyStats.StealDrop steal in enemy.stealTable)
            {
                if(steal == null || steal.item == null) continue;
                result += $"<color={colorBright}>{steal.item.itemName}</color> <color={colorDone}>{steal.stealChance}%</color>\n";
            }
        }
        return result;
    }
    public void OnCardHighlighted(GameObject entry)
    {
        for(int i = 0; i < spawnedCards.Count; i++)
        {
            QuestCardView view = spawnedCards[i].GetComponent<QuestCardView>();
            if(view == null) continue;
            SetCardVisual(view, spawnedCards[i] == entry);
        }
    }
    void SetCardVisual(QuestCardView view, bool selected)
    {
        if(view.borderImage != null) view.borderImage.color = selected ? cardBorderSelected : cardBorderDefault;
        if(view.backgroundImage != null)
        {
            Color bg = cardBackgroundSelected;
            bg.a = selected ? 1f : 0f;
            view.backgroundImage.color = bg;
        }
        if(view.titleText != null) view.titleText.color = selected ? cardTitleSelected : cardTitleDefault;
    }
    List<MenuOption> SettingsMenuList()
    {
        List<MenuOption> options = new List<MenuOption>();
        if(SettingsManager.instance == null)
        {
            options.Add(new MenuOption("Settings unavailable", () => { }) {enabled = false});
            return options;
        }
        SettingsManager setting = SettingsManager.instance;
        options.Add(new MenuOption($"Music Volume: {Mathf.RoundToInt(setting.musicVolume * 100)}%", CycleMusicVolume));
        options.Add(new MenuOption($"SFX Volume: {Mathf.RoundToInt(setting.sfxVolume * 100)}%", CycleSfxVolume));
        options.Add(new MenuOption($"Dialogue Text Speed: {setting.dialogueTextSpeed:0}", CycleDialogueTextSpeed));
        options.Add(new MenuOption($" Battle Text Speed: {setting.battleTextSpeed:0}", CycleBattleTextSpeed));
        options.Add(new MenuOption($"Battle Speed: {setting.battleSpeedMultiplier: 0.0}x", CycleBattleSpeed));
        options.Add(new MenuOption($"Text Color R: {Mathf.RoundToInt(setting.uiTextColor.r * 100)}%", () => CycleTextColorChannel(0)));
        options.Add(new MenuOption($"Text Color G: {Mathf.RoundToInt(setting.uiTextColor.g * 100)}%", () => CycleTextColorChannel(1)));
        options.Add(new MenuOption($"Text Color B: {Mathf.RoundToInt(setting.uiTextColor.b * 100)}%", () => CycleTextColorChannel(2)));
        options.Add(new MenuOption($"Panel Color R: {Mathf.RoundToInt(setting.menuPanelColor.r * 100)}%", () => CyclePanelColorChannel(0)));
        options.Add(new MenuOption($"Panel Color G: {Mathf.RoundToInt(setting.menuPanelColor.g * 100)}%", () => CyclePanelColorChannel(1)));
        options.Add(new MenuOption($"Panel Color B: {Mathf.RoundToInt(setting.menuPanelColor.b * 100)}%", () => CyclePanelColorChannel(2)));
        options.Add(new MenuOption($"Panel Opacity: {Mathf.RoundToInt(setting.menuPanelColor.a * 100)}%", CyclePanelOpacity));
        options.Add(new MenuOption($"Border Color R: {Mathf.RoundToInt(setting.menuBorderColor.r * 100)}%", () => CycleBorderColorChannel(0)));
        options.Add(new MenuOption($"Border Color G: {Mathf.RoundToInt(setting.menuBorderColor.g * 100)}%", () => CycleBorderColorChannel(1)));
        options.Add(new MenuOption($"Border Color B: {Mathf.RoundToInt(setting.menuBorderColor.b * 100)}%", () => CycleBorderColorChannel(2)));
        options.Add(new MenuOption($"Border Thickness: {setting.menuBorderThickness:0}px", CycleBorderThickness));
        return options;
    }
    void SaveGame()
    {
        if(SaveManager.instance != null) SaveManager.instance.SaveGame();
       else Debug.LogWarning("SaveManager missing Saving failed."); 
    }
    void LoadGame()
    {
        if(SaveManager.instance != null) SaveManager.instance.LoadGame();
       else Debug.LogWarning("SaveManager missing Loading failed."); 
    }
     void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else 
        Application.Quit();
        #endif
    }
    void RefreshSettingsScreen()
    {
        host.RefreshScreen(SettingsMenuList());
    }
    void CycleMusicVolume()
    {
        SettingsManager.instance.SetMusicVolume(NextVolumeStep(SettingsManager.instance.musicVolume));
        RefreshSettingsScreen();
    }
    void CycleSfxVolume()
    {
        SettingsManager.instance.SetSfxVolume(NextVolumeStep(SettingsManager.instance.sfxVolume));
        RefreshSettingsScreen();
    }
    void CycleDialogueTextSpeed()
    {
        SettingsManager.instance.dialogueTextSpeed = NextTextSpeedStep(SettingsManager.instance.dialogueTextSpeed);
       RefreshSettingsScreen(); 
    }
    void CycleBattleTextSpeed()
    {
        SettingsManager.instance.battleTextSpeed = NextTextSpeedStep(SettingsManager.instance.battleTextSpeed);
        RefreshSettingsScreen();
    }
    void CycleBattleSpeed()
    {
        SettingsManager.instance.battleSpeedMultiplier = NextBattleSpeedStep(SettingsManager.instance.battleSpeedMultiplier);
        RefreshSettingsScreen();
    }
    void CycleTextColorChannel(int channel)
    {
        Color c = SetChannel(SettingsManager.instance.uiTextColor, channel, NextVolumeStep(GetChannel(SettingsManager.instance.uiTextColor, channel)));
        SettingsManager.instance.SetTextColor(c);
        RefreshSettingsScreen();
    }
    void CyclePanelColorChannel(int channel)
    {
        Color c = SetChannel(SettingsManager.instance.menuPanelColor, channel, NextVolumeStep(GetChannel(SettingsManager.instance.menuPanelColor, channel)));
        SettingsManager.instance.SetMenuPanelColor(c);
        RefreshSettingsScreen();
    }
    void CyclePanelOpacity()
    {
        Color c = SettingsManager.instance.menuPanelColor;
        c.a = NextVolumeStep(c.a);
        SettingsManager.instance.SetMenuPanelColor(c);
        RefreshSettingsScreen();
    }
    void CycleBorderColorChannel(int channel)
    {
        Color c = SetChannel(SettingsManager.instance.menuBorderColor, channel, NextVolumeStep(GetChannel(SettingsManager.instance.menuBorderColor, channel)));
        SettingsManager.instance.SetMenuBorderColor(c);
        RefreshSettingsScreen();
    }
    void CycleBorderThickness()
    {
        float next = SettingsManager.instance.menuBorderThickness + 1f;
        if(next > 10f) next = 1f;
        SettingsManager.instance.SetMenuBorderThickness(next);
        RefreshSettingsScreen();
    }
    float GetChannel(Color c, int channel) => channel == 0 ? c.r : channel == 1 ? c.g : c.b;
    Color SetChannel(Color c, int channel, float value)
    {
        if(channel == 0) c.r = value; else if(channel == 1) c.g = value; else c.b = value;
        return c;
    }
    float NextVolumeStep(float current)
    {
        float next = current + 0.1f;
        return next > 1.001f ? 0f : Mathf.Clamp01(next);
    }
    float NextTextSpeedStep(float current)
    {
        float next = current + 10f;
        return next > 100f ? 10f : next;
    }
    float NextBattleSpeedStep(float current)
    {
        float next = current + 0.5f;
        return next > 2.5f ? 0.5f : next;
    }
}
