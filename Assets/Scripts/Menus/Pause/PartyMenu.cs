using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
public class PartyMenu : MonoBehaviour, ITabVisualOwner, ICardHighlightHandler, IPageableTab
{
public PauseMenu host;
public PartyController partyController;
public KeyCode rosterSwapKey = KeyCode.Tab;
public TextMeshProUGUI characterHeaderText;
public TextMeshProUGUI coreStatsText;
public TextMeshProUGUI secondaryStatsText;
public TextMeshProUGUI equippedSkillsText;
public TextMeshProUGUI infoText;
public GameObject equipmentCardPrefab;
public Color cardBorderDefault = new Color32(0x3A, 0x16, 0x16, 0xFF);
public Color cardBorderSelected = new Color32(0xD8, 0x5A, 0x30, 0xFF);
public Color cardBackgroundSelected = new Color32(0x24, 0x10, 0x10, 0xFF);
public Color cardTitleDefault = new Color32(0xC9, 0xC2, 0xC2, 0xFF);
public Color cardTitleSelected = new Color32(0xF2, 0xF2, 0xF2, 0xFF);
public TextMeshProUGUI equipmentDetailText;
public Button changeEquipmentButton;
public TextMeshProUGUI changeEquipmentButtonLabel;
public Transform contentGrid;
public Transform equipmentGrid;
public int statColumnOffsetPx = 200;
public TextMeshProUGUI bondBonusText;
const string colorMuted = "#8A8580";
const string colorBody = "#C9C2C2";
const string colorBright = "#E8E4E0";
const string dot = "\u00B7";
bool inCharacterDetail;
bool inAbilitiesTab;
public bool InCharacterDetail => inCharacterDetail;
ActiveStats selectedCharacter;
GameObject selectedCharacterObject;
Equipment.EquipmentType viewingSlotType;
int selectedTreeIndex; 
int selectedSkillSlotIndex;
Baggable selectedPersonalItem;
ActiveStats selectedBondPartner;
bool viewingEquipmentDetail;
bool inEquipmentPicker;
List<GameObject> spawnedContentCards = new List<GameObject>();
Stack<System.Action> navStack = new Stack<System.Action>();
GridCardPager equipmentPager;
GridCardPager abilityPager;
GridCardPager skillTreePager;
GridCardPager activeGridPager;
List<BondData> bondCardData = new List<BondData>();
bool inBondPartnerList;
public void ResetState()
    {
        inCharacterDetail = false;
        inAbilitiesTab = false;
        HideVisuals();
       if(host.miniTabGroup != null) host.miniTabGroup.Hide();
       if(host.microTabGroup != null) host.microTabGroup.Hide();
    }
    public void HideVisuals()
    {
        ClearAllTabCards();
        navStack.Clear();
        viewingEquipmentDetail = false;
        inEquipmentPicker = false;
        if(equipmentDetailText != null) equipmentDetailText.gameObject.SetActive(false);
        if(changeEquipmentButton != null) changeEquipmentButton.gameObject.SetActive(false);
        if(bondBonusText != null) bondBonusText.gameObject.SetActive(false);
    }
public void OpenTab()
    {
        host.PrepareTabSwitch();
        inCharacterDetail = false;
        navStack.Clear();
        if(host.miniTabGroup != null) host.miniTabGroup.Hide();
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ShowRosterPanel();
        host.ClearMenuEntries();
        host.ClearScreenHistory();
        if(host.pageText != null) host.pageText.text = "";
        if(partyController != null)
        {
            partyController.Init(host);
            host.SetCardHighlightHandler(partyController);
            partyController.Refresh(OpenCharacterDetail);
        }
        host.SetBreadcrumbSuffix("Party");
    }
    void OpenCharacterDetail(GameObject characterObject)
    {
        ActiveStats character = characterObject.GetComponent<ActiveStats>();
        if(character == null) return;
        selectedCharacter = character;
        selectedCharacterObject = characterObject;
        inCharacterDetail = true;
        inAbilitiesTab = false;
        if(partyController != null) partyController.SelectedCharacter(characterObject);
        SetupCharacterTabs();
    }
    public void ExitCharacterDetail()
    {
        inCharacterDetail = false;
        inAbilitiesTab = false;
        navStack.Clear();
        ClearAllTabCards();
        if(host.miniTabGroup != null) host.miniTabGroup.Hide();
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        OpenTab();
    }
    void SetupCharacterTabs()
    {
        List<TabDefinition> tabs = new List<TabDefinition>
        {
            new TabDefinition("Stats", OpenStatsTab),
            new TabDefinition("Skills", AbilitiesTab),
            new TabDefinition("Bond", OpenBondMenu),
            new TabDefinition("Inventory", OpenInventoryTab),
            new TabDefinition("Bio", OpenInfoTab),
        };
        if(host.miniTabGroup != null)
        {
            host.miniTabGroup.Show();
            host.miniTabGroup.SetTabs(tabs, 0);
        }
    }
    public void HandleBack()
    {
        if(inEquipmentPicker) { ShowEquipmentDetail(viewingSlotType); return; }
        if(viewingEquipmentDetail) { ExitEquipmentDetail(); return; }
        if(inCharacterDetail)
        {
            if(navStack.Count > 0) { PopNav(); return; }
            ExitCharacterDetail();
            return;
        }
        if(partyController != null && partyController.HasPickedUp) {partyController.CancelSwap(); return;}
        host.Close();
    }
    public void HandleTabInput()
    {
        if(!inCharacterDetail)
        {
            if(partyController != null && Input.GetKeyDown(rosterSwapKey))
            partyController.ToggleSwapOnFocused(EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null);
            return;
        }
    }
    void PushNav(System.Action rebuildPrevious) => navStack.Push(rebuildPrevious);
    void PopNav()
    {
        if(navStack.Count > 0) navStack.Pop()?.Invoke();
    }
    void EnsurePagers()
    {
        if(equipmentPager == null) equipmentPager = new GridCardPager(equipmentCardPrefab, equipmentGrid, host, 2, 3);
        if(abilityPager == null) abilityPager = new GridCardPager(equipmentCardPrefab, contentGrid, host, 3, 3);
        if(skillTreePager == null) skillTreePager = new GridCardPager(equipmentCardPrefab, contentGrid, host, 2, 3);
    }
    public void NextPage()
    {
        activeGridPager?.NextPage();
        activeGridPager?.SelectFirstOnPage();
    }
    public void PreviousPage()
    {
        activeGridPager?.PreviousPage();
        activeGridPager?.SelectFirstOnPage();
    }
    void SpawnContentCard(string title, string subText, string detail, System.Action onSelect, bool enabled = true)
    {
        if(equipmentCardPrefab == null || contentGrid == null) return;
        GameObject cardObj = Instantiate(equipmentCardPrefab, contentGrid);
        spawnedContentCards.Add(cardObj);
        EntryCard card = cardObj.GetComponent<EntryCard>();
        if(card == null) return;
        if(card.titleText != null) card.titleText.text = title;
        if(card.subText != null) card.subText.text = subText;
        MenuOption option = new MenuOption(title, () => { }) { description = detail };
        host.RegisterEntry(cardObj, option);
        if(card.button != null)
        {
            card.button.interactable = enabled;
            card.button.onClick.RemoveAllListeners();
            GameObject capturedCard = cardObj;
            if(enabled)
            {
                card.button.onClick.AddListener(() =>
                {
                    host.EntryHighlight(capturedCard);
                    onSelect?.Invoke();
                });
            }
        }
        SetCardVisual(card, false);
    }
    void ClearContentCards()
    {
        foreach(GameObject card in spawnedContentCards) if(card != null) Destroy(card);
        spawnedContentCards.Clear();
        bondCardData.Clear();
        inBondPartnerList = false;
    }
    void ClearAllTabCards()
    {
        ClearContentCards();
        equipmentPager?.Clear();
        abilityPager?.Clear();
        skillTreePager?.Clear();
    }
    public void OnCardHighlighted(GameObject entry)
    {
        HighlightInList(spawnedContentCards, entry);
        if(equipmentPager != null) HighlightInList(equipmentPager.SpawnedCards, entry);
        if(abilityPager != null) HighlightInList(abilityPager.SpawnedCards, entry);
        if(skillTreePager != null) HighlightInList(skillTreePager.SpawnedCards, entry);
        if(inBondPartnerList)
        {
            int index = spawnedContentCards.IndexOf(entry);
            if(index >= 0 && index < bondCardData.Count) UpdateBondBonusText(bondCardData[index]);
        }
    }
    void HighlightInList(List<GameObject> list, GameObject entry)
    {
        for(int i = 0; i < list.Count; i++)
        {
            if(list[i] == null) continue;
            EntryCard card = list[i].GetComponent<EntryCard>();
            if(card == null) continue;
            SetCardVisual(card, list[i] == entry);
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
    string BuildTwoColumnLines(List<string> items, int columnOffsetPx)
    {
        int rows = Mathf.CeilToInt(items.Count / 2f);
        List<string> lines = new List<string>();
        for(int i = 0; i < rows; i++)
        {
            string left = items[i];
            int rightIndex = i + rows;
            string right = rightIndex < items.Count ? items[rightIndex] : null;
            lines.Add(right != null ? $"{left}<pos={columnOffsetPx}>{right}" : left);
        }
        return string.Join("\n", lines);
    }
    void OpenStatsTab()
    {
        inAbilitiesTab = false;
        navStack.Clear();
        viewingEquipmentDetail = false;
        inEquipmentPicker = false;
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        if(partyController != null) partyController.SelectedCharacter(selectedCharacterObject);
        host.ShowRosterPanel(true);
        UpdateStatsExtraPanel();
        SetStatsTextVisible(true);
        if(equipmentDetailText != null) equipmentDetailText.gameObject.SetActive(false);
        if(changeEquipmentButton != null) changeEquipmentButton.gameObject.SetActive(false);
        if(bondBonusText != null) bondBonusText.gameObject.SetActive(false);
        host.SetCardHighlightHandler(this);
        host.SetPageableTab(this);
        ClearAllTabCards();
        RebuildEquipmentCards();
    }
void UpdateStatsExtraPanel()
    {
        ActiveStats character = selectedCharacter;
        if(character == null) return;
        if(characterHeaderText != null)
        characterHeaderText.text = $"{character.currentName} Lv.{character.currentLevel}\nHP{character.currentHP}/{character.finalHP} MP{character.currentMP}/{character.finalMP}\n" +
        $"Exp {character.currentExperience}/{character.currentExpToNextLevel} (Total Exp{character.totalExperience})";
        if(coreStatsText != null)
        {
            List<string> coreItems = new List<string>
            {
            $"STR: {character.finalStrength}", $"MAG: {character.finalMagic}", $"DEF: {character.finalDefense}", 
            $"WIS: {character.finalWisdom}", $"TEC: {character.finalTech}", $"AFF: {character.finalAffinity}",
            $"SPD: {character.finalSpeed}", $"LUCK: {character.finalLuck}"
            };
            coreStatsText.text = BuildTwoColumnLines(coreItems, statColumnOffsetPx) + $"\n\nMagic Affinity: {character.currentMagicAffinity}";
        }
        if(secondaryStatsText != null)
        {
        List<string> secondaryItems = new List<string>
        {
        $"Accuracy: {character.Accuracy}", $"Precision: {character.Precision}", $"Evasion: {character.Evasion}",
        $"Foresight: {character.Foresight}", $"Critical: {character.Critical}", $"Dodge: {character.Dodge}"
        };
        secondaryStatsText.text = BuildTwoColumnLines(secondaryItems, statColumnOffsetPx);
        }
        if(equippedSkillsText != null)
        {
            List<string> names = character.equippedSkills.Where(skill => skill != null).Select(skill => skill.skillName).ToList();
            equippedSkillsText.text = names.Count > 0 ? "Equipped Skills:\n" + string.Join("\n", names) : "Equipped Skills:\nNone";
        }   
    }
    void RebuildEquipmentCards()
    {
        EnsurePagers();
        activeGridPager = equipmentPager;
        if(selectedCharacter == null) {equipmentPager.SetSpecs(new List<CardGridSpec>()); return; }
        List<CardGridSpec> specs = new List<CardGridSpec>
    {
        BuildEquipmentSlotSpec(Equipment.EquipmentType.Weapon, "Weapon"),
        BuildEquipmentSlotSpec(Equipment.EquipmentType.Head, "Head"),
        BuildEquipmentSlotSpec(Equipment.EquipmentType.Body, "Body"),
        BuildEquipmentSlotSpec(Equipment.EquipmentType.Shield, "Shield"),
        BuildEquipmentSlotSpec(Equipment.EquipmentType.Accessory, "Accessory"),
    };
       equipmentPager.SetSpecs(specs);
    }
    CardGridSpec BuildEquipmentSlotSpec(Equipment.EquipmentType slotType, string label)
    {
        Equipment equipped = selectedCharacter.GetEquipped(slotType);
        string title = equipped != null ? equipped.equipmentName : "Empty";
        return new CardGridSpec(title, label, BuildEquipmentDetail(slotType, equipped), () => ShowEquipmentDetail(slotType));
    }
    void ShowEquipmentDetail(Equipment.EquipmentType slotType)
    {
        inEquipmentPicker = false;
        viewingEquipmentDetail = true;
        viewingSlotType = slotType;
        equipmentPager?.Clear();
        Equipment equipped = selectedCharacter.GetEquipped(slotType);
        SetStatsTextVisible(false);
        if(equipmentDetailText != null)
        {
            equipmentDetailText.gameObject.SetActive(true);
            equipmentDetailText.text = BuildEquipmentDetail(slotType, equipped);
        }
        if(changeEquipmentButton != null)
        {
            changeEquipmentButton.gameObject.SetActive(true);
            changeEquipmentButton.interactable = true;
            if(changeEquipmentButtonLabel != null) changeEquipmentButtonLabel.text = equipped != null ? "Change" : "Equip";
            changeEquipmentButton.onClick.RemoveAllListeners();
            changeEquipmentButton.onClick.AddListener(() => ShowEquipmentPicker(slotType));
        }
    }
    void ExitEquipmentDetail()
    {
     viewingEquipmentDetail = false;
     SetStatsTextVisible(true);
     if(equipmentDetailText != null) equipmentDetailText.gameObject.SetActive(false);
     if(changeEquipmentButton != null) changeEquipmentButton.gameObject.SetActive(false);
     RebuildEquipmentCards();
    }
    void SetStatsTextVisible(bool visible)
    {
        if(characterHeaderText != null) characterHeaderText.gameObject.SetActive(visible);
        if(coreStatsText != null) coreStatsText.gameObject.SetActive(visible);
        if(secondaryStatsText != null) secondaryStatsText.gameObject.SetActive(visible);
        if(equippedSkillsText != null) equippedSkillsText.gameObject.SetActive(visible);
    }
    string BuildEquipmentDetail(Equipment.EquipmentType slotType, Equipment equipped)
    {
        if(equipped == null)
        {
            return $"<size=140%><color=#F2F2F2>{slotType}</color></size>\n<size=80%><color={colorMuted}>empty</color></size>\n\n<color={colorBody}>Nothing equipped in this slot.</color>";
        }
        string subtitle = slotType == Equipment.EquipmentType.Weapon ? $"{slotType} {dot} {equipped.weaponType}" : slotType.ToString();
        string result = $"<size=140%><color=#F2F2F2>{equipped.equipmentName}</color></size>\n";
        result += $"<size=80%><color={colorMuted}>{subtitle}</color></size>\n\n";
        result += $"<color={colorBright}>{BuildEquipmentStatLine(equipped)}</color>";
        return result;
    }
    string BuildEquipmentStatLine(Equipment equipment)
    {
        List<string> parts = new List<string>();
        if(equipment.strength != 0) parts.Add($"STR {(equipment.strength > 0 ? "+" : "")}{equipment.strength}");
        if(equipment.magic != 0) parts.Add($"MAG {(equipment.magic > 0 ? "+" : "")}{equipment.magic}");
        if(equipment.defense != 0) parts.Add($"DEF {(equipment.defense > 0 ? "+" : "")}{equipment.defense}");
        if(equipment.wisdom != 0) parts.Add($"WIS {(equipment.wisdom > 0 ? "+" : "")}{equipment.wisdom}");
        if(equipment.tech != 0) parts.Add($"TEC {(equipment.tech > 0 ? "+" : "")}{equipment.tech}");
        if(equipment.affinity != 0) parts.Add($"AFF {(equipment.affinity > 0 ? "+" : "")}{equipment.affinity}");
        if(equipment.speed != 0) parts.Add($"SPD {(equipment.speed > 0 ? "+" : "")}{equipment.speed}");
        if(equipment.luck != 0) parts.Add($"LCK {(equipment.luck > 0 ? "+" : "")}{equipment.luck}");
        return parts.Count > 0 ? string.Join(", ", parts) : "no stat bonus";
    }
    void ShowEquipmentPicker(Equipment.EquipmentType slotType)
    {
        inEquipmentPicker = true;
        SetStatsTextVisible(false);
        if(equipmentDetailText != null)
        {
            equipmentDetailText.gameObject.SetActive(true);
            equipmentDetailText.text = $"<size=140%><color=#F2F2F2>Choose {slotType}</color></size>\n<size=80%><color={colorMuted}>from personal inventory</color></size>";
        }
        if(changeEquipmentButton != null) changeEquipmentButton.gameObject.SetActive(false);
        List<CardGridSpec> specs = new List<CardGridSpec>();
        specs.Add(new CardGridSpec("Cancel", "", "Return without changing equipment", () => ShowEquipmentDetail(slotType)));
        Equipment currentlyEquipped = selectedCharacter.GetEquipped(slotType);
        if(currentlyEquipped != null) 
        {
            specs.Add(new CardGridSpec($"Unequip {currentlyEquipped.equipmentName}", "", "Remove the currently equipped item.", () =>
            {
                selectedCharacter.Unequip(slotType);
                AfterEquipmentChange(slotType);
            }));
        }
        List<Equipment.WeaponType> allowed = slotType == Equipment.EquipmentType.Weapon ? selectedCharacter.playerStats.allowedWeaponTypes : null;
        List<Equipment> matching = selectedCharacter.personalInventory.GetEquippableOfType(slotType, allowed);
        foreach(Equipment item in matching)
        {
            Equipment captured = item;
            string sub = slotType == Equipment.EquipmentType.Weapon ? captured.weaponType.ToString() : "";
            specs.Add(new CardGridSpec(captured.equipmentName, sub, BuildEquipmentDetail(slotType, captured), () =>
        {
            selectedCharacter.Equip(captured);
            AfterEquipmentChange(slotType);
        }));
    }
        equipmentPager.SetSpecs(specs);
    }
void AfterEquipmentChange(Equipment.EquipmentType slotType)
    {
        UpdateStatsExtraPanel();
        inEquipmentPicker = false;
        ShowEquipmentDetail(slotType);
    }
    void AbilitiesTab()
    {
        inAbilitiesTab = true;
        navStack.Clear();
        EnsurePagers();
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        if(partyController != null) partyController.SelectedCharacter(selectedCharacterObject);
        host.ShowRosterPanel(false);
        SetStatsTextVisible(false);
        if(bondBonusText != null) bondBonusText.gameObject.SetActive(false);
        host.SetCardHighlightHandler(this);
        host.SetPageableTab(this);
        ClearAllTabCards();
        RebuildAbilityCategoryCards();
    }
    void RebuildAbilityCategoryCards()
    {
        activeGridPager = abilityPager;
        List<CardGridSpec> specs = new List<CardGridSpec>
        {
            new CardGridSpec("Arts", $"{selectedCharacter.learnedArts.Count}", "", () => {PushNav(RebuildAbilityCategoryCards); RebuildArtsCards(); }),
            new CardGridSpec("Spells", $"{selectedCharacter.learnedSpells.Count}", "", () => {PushNav(RebuildAbilityCategoryCards); RebuildSpellCards(); }),
            new CardGridSpec("Fusions", $"{selectedCharacter.learnedFusions.Count}", "", () => {PushNav(RebuildAbilityCategoryCards); RebuildFusionCards(); }),
            new CardGridSpec("Skills", "", "", () => {PushNav(RebuildAbilityCategoryCards); RebuildSkillSlotCards(); }),
            new CardGridSpec("Skill Trees", "", "", () => {PushNav(RebuildAbilityCategoryCards); RebuildSkillTreeCards(); }),
        };
       abilityPager.SetSpecs(specs);
    }
    void RebuildArtsCards()
    {
        activeGridPager = abilityPager;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(Art art in selectedCharacter.learnedArts)
        {
            Art captured = art;
            string detail = $"DMG {captured.Damage}{(captured.isAOE ? " AOE" : "")}";
            specs.Add(new CardGridSpec(captured.artName, $"HP {captured.Cost}", detail, () => { }));
        }
        if(specs.Count == 0) specs.Add(new CardGridSpec("No arts learned.", "", "", () => { }, false));
        abilityPager.SetSpecs(specs);
    }
    void RebuildSpellCards()
    {
        activeGridPager = abilityPager;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(Spell spell in selectedCharacter.learnedSpells)
        {
            Spell captured = spell;
            string detail = $"DMG {captured.Damage} Element: {captured.element}{(captured.isAOE ? " AOE" : "")}";
            specs.Add(new CardGridSpec(captured.spellName, $"MP {captured.Cost}", detail, () => { }));
        }
        if(specs.Count == 0) specs.Add(new CardGridSpec("No spells learned.", "", "", () => { }, false));
        abilityPager.SetSpecs(specs);
    }
    void RebuildFusionCards()
    {
        activeGridPager = abilityPager;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        foreach(Fusion fusion in selectedCharacter.learnedFusions)
        {
            Fusion captured = fusion;
            string detail = $"DMG {captured.Damage} Element: {captured.element}{(captured.isAOE ? " AOE" : "")}";
            specs.Add(new CardGridSpec(captured.fusionName, $"HP {captured.HPCost}/MP {captured.MPCost}", detail, () => { }));
        }
        if(specs.Count == 0) specs.Add(new CardGridSpec("No fusions learned.", "", "", () => { }, false));
        abilityPager.SetSpecs(specs);
    }
    void RebuildSkillSlotCards()
    {
        activeGridPager = abilityPager;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        for(int i = 0; i < selectedCharacter.skillSlots.Length; i++)
        {
            int capturedIndex = i;
            Skill equipped = selectedCharacter.GetSkillSlot(i);
            string title = i == 0 ? "Personal" : $"Slot {i}";
            string sub = equipped != null ? equipped.skillName : (i == 0 ? "None" : "Empty");
            specs.Add(new CardGridSpec(title, sub, "", () => SkillSlotSelection(capturedIndex)));
        }
        abilityPager.SetSpecs(specs);
    }
    void SkillSlotSelection(int slotIndex)
    {
        selectedSkillSlotIndex = slotIndex;
        PushNav(RebuildSkillSlotCards);
        RebuildSkillPickerCards();
    }
    void RebuildSkillPickerCards()
    {
        activeGridPager = abilityPager;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        Skill current = selectedCharacter.GetSkillSlot(selectedSkillSlotIndex);
        if(current != null) specs.Add(new CardGridSpec("Clear", current.skillName, "", ClearSelectedSkillSlot));
        foreach(Skill skill in selectedCharacter.learnedSkills)
        {
            Skill captured = skill;
            specs.Add(new CardGridSpec(captured.skillName, "", "", () => AssignSkill(captured)));
        }
        abilityPager.SetSpecs(specs);
    }
    void AssignSkill(Skill skill)
    {
        selectedCharacter.SetSkillSlot(selectedSkillSlotIndex, skill);
        PopNav();
    }
    void ClearSelectedSkillSlot()
    {
        selectedCharacter.ClearSkillSlot(selectedSkillSlotIndex);
        PopNav();
    }
    void RebuildSkillTreeCards()
    {
        activeGridPager = skillTreePager;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        if(selectedCharacter.playerStats.skillTrees != null)
        {
        for(int i = 0; i < selectedCharacter.playerStats.skillTrees.trees.Count; i++)
        {
            SkillTree tree = selectedCharacter.playerStats.skillTrees.trees[i];
            int capturedIndex = i;
            int points = selectedCharacter.GetTreePoints(i);
            specs.Add(new CardGridSpec(tree.treeName, $"{points} pts", "", () => OpenTreeNodes(capturedIndex)));
        }
    }
        skillTreePager.SetSpecs(specs);
    }
    void OpenTreeNodes(int treeIndex)
    {
        selectedTreeIndex = treeIndex;
        PushNav(RebuildSkillTreeCards);
        RebuildTreeNodeCards();
    }
    void RebuildTreeNodeCards()
    {
        activeGridPager = abilityPager;
        List<CardGridSpec> specs = new List<CardGridSpec>();
        SkillTree tree = selectedCharacter.playerStats.skillTrees.trees[selectedTreeIndex];
        int points = selectedCharacter.GetTreePoints(selectedTreeIndex);
        bool canSpend = selectedCharacter.skillPoints > 0;
        specs.Add(new CardGridSpec("Spend Point", $"{selectedCharacter.skillPoints} available", "", SpendPoint, canSpend));
        foreach(SkillTreePath path in tree.paths)
        {
            bool unlocked = selectedCharacter.IsPathUnlocked(path);
            string status = unlocked ? "Unlocked" : $"{points}/{path.pointsRequired}";
            specs.Add(new CardGridSpec(path.pathName, status, "", () => { }, false));
        }
        abilityPager.SetSpecs(specs);
    }
void SpendPoint()
    {
        selectedCharacter.SpendSkillPoint(selectedTreeIndex);
        RebuildTreeNodeCards();
    }
    void OpenBondMenu()
    {
        inAbilitiesTab = false;
        navStack.Clear();
        if(partyController != null) partyController.SelectedCharacter(selectedCharacterObject);
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ShowRosterPanel(false);
        SetStatsTextVisible(false);
        host.SetCardHighlightHandler(this);
        host.ClearPageableTab();
        ClearAllTabCards();
        RebuildBondCards();
    }
    void RebuildBondCards()
    {
        ClearContentCards();
        inBondPartnerList = true;
        if(bondBonusText != null) bondBonusText.gameObject.SetActive(true);
        foreach (BondData data in selectedCharacter.playerStats.bonds)
        {
            BondRank rank = selectedCharacter.GetBondRank(data.partner);
            BondData capturedData = data;
            SpawnContentCard(data.partner.characterName, rank.ToString(), "", () => OpenConversationList(capturedData));
            bondCardData.Add(data);
        }
        if(bondCardData.Count > 0 ) UpdateBondBonusText(bondCardData[0]);
    }
    void UpdateBondBonusText(BondData data)
    {
        if(bondBonusText == null || data == null) return;
        BondRank rank = selectedCharacter.GetBondRank(data.partner);
        BondRankBonus rankBonus = data.rankBonuses != null ? data.rankBonuses.Find(bonus => bonus.rank == rank) : null;
        List<string> bonusLines = new List<string>();
        if(rankBonus != null && rankBonus.bonuses != null)
        foreach(StatBonus bonus in rankBonus.bonuses) bonusLines.Add($"{bonus.stat}: +{bonus.amount}");
        string bonusText = bonusLines.Count > 0 ? string.Join("\n", bonusLines) : "No active bonuses at this rank.";
        bondBonusText.text = $"<size=140%><color=#F2F2F2>{data.partner.characterName}</color></size>\n<size=80%><color={colorMuted}>{rank} Rank</color></size>\n\n<color={colorBody}>{bonusText}</color>";
    }
    void OpenConversationList(BondData data)
    {
        PushNav(RebuildBondCards);
        RebuildConversationCards(data);
    }
    void RebuildConversationCards(BondData data)
    {
        ClearContentCards();
        if(bondBonusText != null) bondBonusText.gameObject.SetActive(false);
        BondProgress progress = selectedCharacter.GetBondProgress(data.partner);
        BondRank currentRank = selectedCharacter.GetBondRank(data.partner);
        for(int i = 0; i < data.conversations.Count; i++)
        {
            BondConversation conversation = data.conversations[i];
            bool viewed = progress.conversationsViewed[i];
            bool available = !viewed && currentRank >= conversation.requiredRank;
            int capturedIndex = i;
            BondConversation capturedConversation = conversation;
            BondData capturedData = data;
            string title = viewed ? $"{conversation.requiredRank} Rank (Viewed)" : $"{conversation.requiredRank} Rank";
            SpawnContentCard(title, "", "", () => PlayConversation(capturedData, capturedConversation, capturedIndex), available);
        }
     }
        void PlayConversation(BondData data, BondConversation conversation, int index)
    {
        string speakerLabel = $"{selectedCharacter.currentName} & {data.partner.characterName}";
        string[] lines = conversation.dialogue.Split('\n');
        DialogueBox.instance.StartDialogue(speakerLabel, lines, () => MarkViewed(data, index));
    }
    void MarkViewed(BondData data, int index)
    {
        BondProgress progress = selectedCharacter.GetBondProgress(data.partner);
        progress.conversationsViewed[index] = true;
        RebuildConversationCards(data);
    }
    void OpenInventoryTab()
    {
        inAbilitiesTab = false;
        navStack.Clear();
        if(partyController != null) partyController.SelectedCharacter(selectedCharacterObject);
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ShowRosterPanel(false);
        SetStatsTextVisible(false);
        if(bondBonusText != null) bondBonusText.gameObject.SetActive(false);
        host.SetCardHighlightHandler(this);
        host.ClearPageableTab();
        ClearAllTabCards();
        RebuildPersonalInventoryCards();
    }
    void RebuildPersonalInventoryCards()
    {
        ClearContentCards();
        SpawnContentCard("Add from Party Inventory", $"{selectedCharacter.personalInventory.FreeSlots} free", "", OpenAddFromPartyInventory);
        foreach(Baggable item in selectedCharacter.personalInventory.items)
        {
            Baggable captured = item;
            bool isEquipped = captured is Equipment equipment && IsEquippedAnywhere(equipment);
            string sub = isEquipped ? "Equipped" : "";
            SpawnContentCard(captured.DisplayName, sub, "", () => OpenPersonalItemActions(captured));
        }
    }
    bool IsEquippedAnywhere(Equipment equipment)
    {
        return selectedCharacter.weaponSlot == equipment || selectedCharacter.headSlot == equipment ||
        selectedCharacter.bodySlot == equipment || selectedCharacter.shieldSlot == equipment ||
        selectedCharacter.accessorySlot == equipment;
    }
    void OpenPersonalItemActions(Baggable item)
    {
        selectedPersonalItem = item;
        PushNav(RebuildPersonalInventoryCards);
        RebuildPersonalItemActionCards(item);
    }
    void RebuildPersonalItemActionCards(Baggable item)
    {
        ClearContentCards();
        if(item is Equipment equipment && IsEquippedAnywhere(equipment))
        {
            SpawnContentCard("Currently Equipped", "unequip from Stats tab first", "", () => { }, false);
            return;
        }
        SpawnContentCard("Send To Party", "", "", SendSelectedItemToParty);
        if(item is Item usable && usable.itemType == Item.ItemType.Consumable)
        SpawnContentCard("Use", "", "", UsePersonalItem);
    }
    void SendSelectedItemToParty()
    {
        if(InventoryManager.instance != null) InventoryManager.instance.RemovePersonalInventory(selectedPersonalItem, selectedCharacter);
        PopNav();
    }
    void UsePersonalItem()
    {
        if(selectedPersonalItem is Item item)
        {
            if(item.effects != null)
            foreach(Effect effect in item.effects) if(effect != null) StartCoroutine(effect.Apply(selectedCharacter, selectedCharacter));
            if(item.itemType != Item.ItemType.KeyItem) selectedCharacter.personalInventory.RemoveItem(item);
        }
        PopNav();
    }
    void OpenAddFromPartyInventory()
    {
    PushNav(RebuildPersonalInventoryCards);
    RebuildPartyInventoryPickerCards();
    }
    void RebuildPartyInventoryPickerCards()
    {
        ClearContentCards();
        if(InventoryManager.instance == null) return;
        bool any = false;
        foreach(Baggable item in InventoryManager.instance.items)
        {
            any = true;
            Baggable captured = item;
            SpawnContentCard(captured.DisplayName, "", "", () => AddItemToSelectedCharacter(captured));
        }
        if(!any) SpawnContentCard("Party Inventory is empty.", "", "", () => { }, false);
    }
    void AddItemToSelectedCharacter(Baggable item)
    {
        if(selectedCharacter.personalInventory.CanAdd() && InventoryManager.instance != null)
        InventoryManager.instance.AddPersonalInventory(item, selectedCharacter);
        PopNav();
    }
    void OpenInfoTab()
    {
        inAbilitiesTab = false;
        navStack.Clear();
        ClearAllTabCards();
        if(partyController != null) partyController.SelectedCharacter(selectedCharacterObject);
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ShowInfoPanel(true);
        host.ClearPageableTab();
        host.SetBreadcrumbSuffix($"{selectedCharacter.currentName} > Bio");
        if(infoText != null)
        {
            PlayerStats stats = selectedCharacter.playerStats;
            string likes = stats.likes != null && stats.likes.Count > 0 ? string.Join(", ", stats.likes) : "-";
            string dislikes = stats.dislikes != null && stats.dislikes.Count > 0 ? string.Join(", ", stats.dislikes) : "-";
            infoText.text =
            $"{stats.bio}\n\n" +
            $"Sex: {selectedCharacter.currentSex}\nSexuality: {selectedCharacter.currentSexuality}\n" +
            $"Race: {stats.race}\nFrom: {stats.from}\n\n" +
            $"Likes: {likes}\nDislikes: {dislikes}";
        }
    }
}
