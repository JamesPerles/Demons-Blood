using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class LoadMainGame : MenuBase
{
    public GameObject saveSlotCardPrefab;
    public Transform saveSlotCardParent;
    public int maxSaveSlots = 1;
    public Color cardBorderDefault = new Color32(0x3A, 0x16, 0x16, 0xFF);
    public Color cardBorderSelected = new Color32(0xD8, 0x5A, 0x30, 0xFF);
    public Color cardBackgroundSelected = new Color32(0x24, 0x10, 0x10, 0xFF);
    public Color cardTitleDefault = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
    public Color cardTitleSelected = new Color32(0xF2, 0xF2, 0xF2, 0xFF);
    const string colorMuted = "#8A8580";
    const string colorBody = "#C9C2C2";
    const string dot = "\u00B7";
    GridCardPager pager;
    public GameObject rootOptionsPanel;
    public Transform rootOptionsParent;
    GridCardPager startPager;
    public string newGameSceneName;
    bool onSaveScreen;
    void Start()
    {
        pager = new GridCardPager(saveSlotCardPrefab, saveSlotCardParent, this, 2, 3);
        startPager = new GridCardPager(saveSlotCardPrefab, rootOptionsParent, this, 1, 2);
        RebuildSlotCards();
        ShowOptions();
    }
    void Update()
    {
        if(onSaveScreen && Input.GetKeyDown(KeyCode.Backspace)) ShowOptions();
    }
    void ShowOptions()
    {
        onSaveScreen = false;
        if(rootOptionsPanel != null) rootOptionsPanel.SetActive(true);
        SetDisplayActive(false);
        StartOptions();
    }
    void StartOptions()
    {
        if(startPager == null) return;
        List<CardGridSpec> specs = new List<CardGridSpec>
        {
            new CardGridSpec("New Game", "", "Start a new game", StartNewGame),
            new CardGridSpec("Load Game", "", "Choose a save slot to load", OpenSavesScreen),
        };
        startPager.SetSpecs(specs);
        if(startPager.SpawnedCards.Count > 0) EntryHighlight(startPager.SpawnedCards[0]);
    }
    void StartNewGame()
    {
        if(string.IsNullOrEmpty(newGameSceneName))
        {
            Debug.LogWarning("LoadMainGame: scene not set");
            return;
        }
        SceneManager.LoadScene(newGameSceneName);
    }
    void OpenSavesScreen()
    {
        onSaveScreen = true;
        startPager?.Clear();
        if(rootOptionsPanel != null) rootOptionsPanel.SetActive(false);
        SetDisplayActive(true);
        RebuildSlotCards();
    }
    void RebuildSlotCards()
    {
        if(pager == null || saveSlotCardPrefab == null || saveSlotCardParent == null) return;
        if(SaveManager.instance == null)
        {
            Debug.LogError("TitleScreenLoadMenu: Save Instance NULL! Place a savemanager in title screen");
            return;
        }
        List<CardGridSpec> specs = new List<CardGridSpec>();
        for(int slot = 0; slot < maxSaveSlots; slot++)
        {
            SaveManager.SaveSlotSummary summary = SaveManager.instance.GetSlotSummary(slot);
            string title = $"Slot {slot + 1}";
            string subText = summary.exists ? 
            $"{summary.leadCharacterName} Lv.{summary.leadCharacterLevel}" : "Empty";
            int capturedSlot = slot;
            specs.Add(new CardGridSpec(title, subText, BuildSlotDetail(summary, slot),
            () => ConfirmLoadSlot(capturedSlot), summary.exists));
        }
        pager.SetSpecs(specs);
        if(pager.SpawnedCards.Count > 0) EntryHighlight(pager.SpawnedCards[0]);
        else if(detailText != null) detailText.text = "No Save Data Found";
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
    void ConfirmLoadSlot(int slot)
    {
        if(SaveManager.instance == null || !SaveManager.instance.SaveExists(slot)) return;
        SaveManager.instance.LoadGame(slot);
    }
    public override void EntryHighlight(GameObject entry)
    {
        base.EntryHighlight(entry);
        ApplyCardHighlight(pager, entry);
        ApplyCardHighlight(startPager, entry);
    }
    void ApplyCardHighlight(GridCardPager targetPager, GameObject entry)
    {
        if(targetPager == null) return;
        foreach(GameObject card in targetPager.SpawnedCards)
        {
            if(card == null) continue;
            EntryCard entryCard = card.GetComponent<EntryCard>();
            if(entryCard == null) continue;
            bool selected = card == entry;
            if(entryCard.borderImage != null) entryCard.borderImage.color = 
            selected ? cardBorderSelected : cardBorderDefault;
            if(entryCard.backgroundImage != null)
            {
                Color bg = cardBackgroundSelected;
                bg.a = selected ? 1f : 0f;
                entryCard.backgroundImage.color = bg;
            }
            if(entryCard.titleText != null) entryCard.titleText.color = 
            selected ? cardTitleSelected : cardTitleDefault;
        }
    }
}
