using UnityEngine;
using TMPro;
using System.Collections.Generic;
public class MiscellaneousMenu : MonoBehaviour, ICardHighlightHandler, ITabVisualOwner, IPageableTab
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
    GridCardPager bestiaryPager;
    GridCardPager savePager;
    GridCardPager activePager;
    public void OpenTab()
    {
        host.PrepareTabSwitch();
        if(bestiaryPager == null) bestiaryPager = new GridCardPager(bestiaryCardPrefab, bestiaryCardParent, host, 3, 3);
        if(savePager == null) savePager = new GridCardPager(saveSlotCardPrefab, saveSlotCardParent, host, 3, 3);
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
        host.SetPageableTab(this);
        RebuildBestiaryCards();
    }
    void ShowSettingsTab()
    {
        if(bestiaryCardParent != null) bestiaryCardParent.gameObject.SetActive(false);
        if(saveSlotCardParent != null) saveSlotCardParent.gameObject.SetActive(false);
        host.ClearPageableTab();
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
       activePager = savePager;
       host.SetPageableTab(this);
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
       activePager = savePager;
       host.SetPageableTab(this);
       RebuildSlotCards(false); 
    }
    public void NextPage()
    {
        activePager?.NextPage();
        if(activePager != null && activePager.SpawnedCards.Count > 0) host.EntryHighlight(activePager.SpawnedCards[0]);
    }
    public void PreviousPage()
    {
        activePager?.PreviousPage();
        if(activePager != null && activePager.SpawnedCards.Count > 0) host.EntryHighlight(activePager.SpawnedCards[0]);
    }
    void RebuildSlotCards(bool saveMode)
    {
        if(savePager == null || saveSlotCardPrefab == null || saveSlotCardParent == null || SaveManager.instance == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        for(int slot = 0; slot < maxSaveSlots; slot++)
        {
            SaveManager.SaveSlotSummary summary = SaveManager.instance.GetSlotSummary(slot);
            string title = $"Slot {slot + 1}";
            string subText = summary.exists ? $"{summary.leadCharacterName} Lv.{summary.leadCharacterLevel}" : "Empty";
            int capturedSlot = slot;
            bool clickable = saveMode || summary.exists;
            specs.Add(new CardGridSpec(title, subText, BuildSlotDetail(summary, slot),
            () => { if(saveMode) ConfirmSaveSlot(capturedSlot); else ConfirmLoadSlot(capturedSlot); }, clickable));
        }
        savePager.SetSpecs(specs);
        if(savePager.SpawnedCards.Count > 0) host.EntryHighlight(savePager.SpawnedCards[0]);
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
        if(bestiaryPager == null || bestiaryCardPrefab == null || bestiaryCardParent == null || BestiaryManager.instance == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(EnemyStats enemy in BestiaryManager.instance.allEnemies)
        {
            if(enemy == null) continue;
            bool discovered = BestiaryManager.instance.IsDiscovered(enemy);
            string title = discovered ? enemy.enemyName : "? ? ?";
            EnemyStats captured = enemy;
            specs.Add(new CardGridSpec(title, "", BuildBestiaryDetail(captured, discovered), () => { }));
        }
        bestiaryPager.SetSpecs(specs);
        if(bestiaryPager.SpawnedCards.Count > 0) host.EntryHighlight(bestiaryPager.SpawnedCards[0]);
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
        if(activePager == null) return;
        for(int i = 0; i < activePager.SpawnedCards.Count; i++)
        {
            if(activePager.SpawnedCards[i] == null) continue;
            EntryCard card = activePager.SpawnedCards[i].GetComponent<EntryCard>();
            if(card == null) continue;
            SetCardVisual(card, activePager.SpawnedCards[i] == entry);
        }
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
}
