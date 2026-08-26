using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;
public class PartyMenu : MonoBehaviour
{
public PauseMenu host;
public RosterController rosterController;
public KeyCode rosterSwapKey = KeyCode.Tab;
public TextMeshProUGUI characterHeaderText;
public TextMeshProUGUI coreStatsText;
public TextMeshProUGUI secondaryStatsText;
public TextMeshProUGUI equippedSkillsText;
public TextMeshProUGUI infoText;
bool inCharacterDetail;
bool inAbilitiesTab;
int characterDetailAnchorDepth;
public bool InCharacterDetail => inCharacterDetail;
ActiveStats selectedCharacter;
Equipment.EquipmentType selectedSlotType;
int selectedTreeIndex; 
int selectedSkillSlotIndex;
Baggable selectedPersonalItem;
ActiveStats selectedBondPartner;
public void ResetState()
    {
        inCharacterDetail = false;
        inAbilitiesTab = false;
       if(host.miniTabGroup != null) host.miniTabGroup.Hide();
       if(host.microTabGroup != null) host.microTabGroup.Hide();
    }
    
public void OpenTab()
    {
        host.PrepareTabSwitch();
        inCharacterDetail = false;
        if(host.miniTabGroup != null) host.miniTabGroup.Hide();
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ShowRosterPanel();
        host.ClearMenuEntries();
        host.ClearScreenHistory();
        if(host.pageText != null) host.pageText.text = "";
        if(rosterController != null)
        {
            rosterController.Init(host);
            rosterController.Refresh(OpenCharacterDetail);
        }
        host.SetBreadcrumbSuffix("Party");
    }
    void OpenCharacterDetail(GameObject characterObject)
    {
        ActiveStats character = characterObject.GetComponent<ActiveStats>();
        if(character == null) return;
        selectedCharacter = character;
        inCharacterDetail = true;
        inAbilitiesTab = false;
        characterDetailAnchorDepth = host.ScreenDepth;
        SetupCharacterTabs();
    }
    public void ExitCharacterDetail()
    {
        inCharacterDetail = false;
        inAbilitiesTab = false;
        if(host.miniTabGroup != null) host.miniTabGroup.Hide();
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ClearScreenHistory();
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
        if(inCharacterDetail)
        {
            if(host.ScreenDepth > characterDetailAnchorDepth + 1) host.PreviousScreen();
            else ExitCharacterDetail();
            return;
        }
        if(rosterController != null && rosterController.HasPickedUp) {rosterController.CancelSwap(); return;}
        host.Close();
    }
    public void HandleTabInput()
    {
        if(!inCharacterDetail)
        {
            if(rosterController != null && Input.GetKeyDown(rosterSwapKey))
            rosterController.ToggleSwapOnFocused(EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null);
            return;
        }
    }
    void OpenStatsTab()
    {
        inAbilitiesTab = false;
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ShowListPanel();
        UpdateStatsExtraPanel();
        host.SwitchTab(StatsTabOptions(), "Stats", characterDetailAnchorDepth);
        host.SetStatsExtraPanelActive(true);
    }
void UpdateStatsExtraPanel()
    {
        ActiveStats character = selectedCharacter;
        if(character == null) return;
        if(characterHeaderText != null)
        characterHeaderText.text = $"{character.currentName} Lv.{character.currentLevel}\nHP{character.currentHP}/{character.finalHP} MP{character.currentMP}/{character.finalMP}\n" +
        $"Exp {character.currentExperience}/{character.currentExpToNextLevel} (Total Exp{character.totalExperience})";
        if(coreStatsText != null)
        coreStatsText.text = 
        $"STR: {character.finalStrength} MAG: {character.finalMagic} Def: {character.finalDefense} Wis: {character.finalWisdom} Tech: {character.finalTech}\n" +
        $"AFF: {character.finalAffinity} SPD: {character.finalSpeed} LUCK: {character.finalLuck}\n " +
        $"Magic Affinity: {character.currentMagicAffinity}";
        if(secondaryStatsText != null)
        secondaryStatsText.text =
        $"Accuracy: {character.Accuracy}\nPrecision: {character.Precision}\nEvasion: {character.Evasion}\n" +
        $"Foresight: {character.Foresight}\nCritical: {character.Critical}\nDodge: {character.Dodge}";
        if(equippedSkillsText != null)
        {
            List<string> names = character.equippedSkills.Where(skill => skill != null).Select(skill => skill.skillName).ToList();
            equippedSkillsText.text = names.Count > 0 ? "Equipped Skills:\n" + string.Join("\n", names) : "Equipped Skills:\nNone";
        }   
    }
    List<MenuOption> StatsTabOptions()
    {
        List<MenuOption> options = new List<MenuOption>();
        options.Add(EquipmentSlotOption(Equipment.EquipmentType.Weapon, "Weapon"));
        options.Add(EquipmentSlotOption(Equipment.EquipmentType.Head, "Head"));
        options.Add(EquipmentSlotOption(Equipment.EquipmentType.Body, "Body"));
        options.Add(EquipmentSlotOption(Equipment.EquipmentType.Shield, "Shield"));
        options.Add(EquipmentSlotOption(Equipment.EquipmentType.Accessory, "Accessory"));
       options.Add(new MenuOption("Bonds", () => { }) { enabled = false});
       foreach(BondProgress progress in selectedCharacter.bondProgress)
        {
            if(progress.partner == null) continue;
            BondRank rank = selectedCharacter.GetBondRank(progress.partner);
            BondProgress captured = progress;
            options.Add(new MenuOption($"{progress.partner.characterName} ({rank})", () => OpenBondBonusDetail(captured)));
        }
        return options;
    }
    MenuOption EquipmentSlotOption(Equipment.EquipmentType slotType, string label)
    {
        Equipment equipped = selectedCharacter.GetEquipped(slotType);
        string entryLabel = $"{label}: {(equipped != null ? equipped.equipmentName : "Empty")}";
        return new MenuOption(entryLabel, () => OpenEquipmentPicker(slotType));
    }
    void OpenEquipmentPicker(Equipment.EquipmentType slotType)
    {
        selectedSlotType = slotType;
        host.OpenScreen(ChooseEquipmentMenu(), slotType.ToString());
    }
    List<MenuOption> ChooseEquipmentMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        Equipment currentlyEquipped = selectedCharacter.GetEquipped(selectedSlotType);
        if(currentlyEquipped != null) options.Add(new MenuOption($"Unequip{currentlyEquipped.equipmentName}", UnequipEquipment));
        List<Equipment.WeaponType> allowed = selectedSlotType == Equipment.EquipmentType.Weapon
        ? selectedCharacter.playerStats.allowedWeaponTypes : null;
        List<Equipment> matching = selectedCharacter.personalInventory.GetEquippableOfType(selectedSlotType, allowed);
        foreach(Equipment item in matching)
        {
            Equipment captured = item;
            string label = selectedSlotType == Equipment.EquipmentType.Weapon ? $"{captured.equipmentName} ({captured.weaponType})" : captured.equipmentName;
            options.Add(new MenuOption (label, () => EquipEquipment(captured)));
        }
        return options;
    }
void EquipEquipment(Equipment equipment)
    {
        selectedCharacter.Equip(equipment);
        UpdateStatsExtraPanel();
        host.PopAndRefresh(StatsTabOptions());
    }
    void UnequipEquipment()
    {
        selectedCharacter.Unequip(selectedSlotType);
        UpdateStatsExtraPanel();
        host.PopAndRefresh(StatsTabOptions());
    }
    void OpenBondBonusDetail(BondProgress progress)
    {
        List<MenuOption> options = new List<MenuOption>();
        BondRank rank = selectedCharacter.GetBondRank(progress.partner);
        BondData data = selectedCharacter.playerStats.bonds.Find(bond => bond.partner == progress.partner);
        if(data != null)
        {
            BondRankBonus rankBonus = data.rankBonuses.Find(rankbonus => rankbonus.rank == rank);
            if(rankBonus != null && rankBonus.bonuses != null)
            foreach(StatBonus bonus in rankBonus.bonuses)
            options.Add(new MenuOption($"{bonus.stat}: +{bonus.amount}", () => { }) {enabled = false});
        }
        if(options.Count == 0) options.Add(new MenuOption("No active bonuses at this rank.", () => { }) { enabled = false});
        host.OpenScreen(options, $"{progress.partner.characterName} Bonuses");
    }
    void AbilitiesTab()
    {
        inAbilitiesTab = true;
        host.ShowListPanel();
        host.SetStatsExtraPanelActive(false);
        List<TabDefinition> microTabs = new List<TabDefinition>
        {
            new TabDefinition("Arts", OpenArtsMicroTab),
            new TabDefinition("Spells", OpenSpellsMicroTab),
            new TabDefinition("Fusion", OpenFusionMicroTab),
            new TabDefinition("Skills", OpenSkillsMicroTab),
            new TabDefinition("Skill Trees", OpenSkillTreeMicroTab),
        };
        if(host.microTabGroup != null)
        {
            host.microTabGroup.Show();
            host.microTabGroup.SetTabs(microTabs, 0);
        }
    }
    void OpenArtsMicroTab()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Art art in selectedCharacter.learnedArts)
        {
            Art captured = art;
            MenuOption option = new MenuOption($"{captured.artName} (HP {captured.Cost})", () => { }) {enabled = false};
            option.description = $"DMG {captured.Damage}{(captured.isAOE ? " AOE" : "")}";
            options.Add(option);
        }
        if(options.Count == 0) options.Add(new MenuOption("No arts learned.", () => { }) { enabled = false});
        host.SwitchTab(options, "Arts", characterDetailAnchorDepth);
    }
    void OpenSpellsMicroTab()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Spell spell in selectedCharacter.learnedSpells)
        {
            Spell captured = spell;
            MenuOption option = new MenuOption($"{captured.spellName} (MP {captured.Cost})", () => { }) {enabled = false};
            option.description = $"DMG {captured.Damage} Element: {captured.element}{(captured.isAOE ? " AOE" : "")}";
            options.Add(option);
        }
        if(options.Count == 0) options.Add(new MenuOption("No spells learned.", () => { }) { enabled = false});
        host.SwitchTab(options, "Spells", characterDetailAnchorDepth);
    }
    void OpenFusionMicroTab()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Fusion fusion in selectedCharacter.learnedFusions)
        {
            Fusion captured = fusion;
            MenuOption option = new MenuOption($"{captured.fusionName} (HP {captured.HPCost}/MP {captured.MPCost})", () => { }) {enabled = false};
            option.description = $"DMG {captured.Damage} Element: {captured.element}{(captured.isAOE ? " AOE" : "")}";
            options.Add(option);
        }
        if(options.Count == 0) options.Add(new MenuOption("No fusions learned.", () => { }) { enabled = false});
        host.SwitchTab(options, "Fusions", characterDetailAnchorDepth);
    }
    void OpenSkillsMicroTab()
    {
        host.SwitchTab(SkillsMicroTabOptions(), "Skills", characterDetailAnchorDepth);
    }
    List<MenuOption> SkillsMicroTabOptions()
    {
        List<MenuOption> options = new List<MenuOption>();
        for(int i = 0; i < selectedCharacter.skillSlots.Length; i++)
        {
            int capturedIndex = i;
            Skill equipped = selectedCharacter.GetSkillSlot(i);
            string label = i == 0 
            ? $"Personal: {(equipped != null ? equipped.skillName : "None")}"
            : $"Slot {i}: {(equipped != null ? equipped.skillName : "Empty")}";
            options.Add(new MenuOption(label, () => SkillSlotSelection(capturedIndex)));
        }
        return options;
    }
    void SkillSlotSelection(int slotIndex)
    {
        selectedSkillSlotIndex = slotIndex;
        host.OpenScreen(SkillSlotOptions(), $"Slot {slotIndex}");
    }
    List<MenuOption> SkillSlotOptions()
    {
        List<MenuOption> options = new List<MenuOption>();
        Skill current = selectedCharacter.GetSkillSlot(selectedSkillSlotIndex);
        if(current != null) options.Add(new MenuOption($"Clear ({current.skillName})", ClearSelectedSkillSlot));
        foreach(Skill skill in selectedCharacter.learnedSkills)
        {
            Skill captured = skill;
            options.Add(new MenuOption(captured.skillName, () => AssignSkill(captured)));
        }
        return options;
    }
    void AssignSkill(Skill skill)
    {
        selectedCharacter.SetSkillSlot(selectedSkillSlotIndex, skill);
        UpdateStatsExtraPanel();
        host.PopAndRefresh(SkillsMicroTabOptions());
    }
    void ClearSelectedSkillSlot()
    {
        selectedCharacter.ClearSkillSlot(selectedSkillSlotIndex);
        UpdateStatsExtraPanel();
        host.PopAndRefresh(SkillsMicroTabOptions());
    }
    void OpenSkillTreeMicroTab()
    {
        host.SwitchTab(SkillTreeMenu(), "Skill Tree", characterDetailAnchorDepth);
    }
    List<MenuOption> SkillTreeMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        if(selectedCharacter.playerStats.skillTrees == null) return options;
        for(int i = 0; i < selectedCharacter.playerStats.skillTrees.trees.Count; i++)
        {
            SkillTree tree = selectedCharacter.playerStats.skillTrees.trees[i];
            int capturedIndex = i;
            int points = selectedCharacter.GetTreePoints(i);
            options.Add(new MenuOption($"{tree.treeName} ({points} pts)", () => OpenTreeNodes(capturedIndex)));
        }
        return options;
    }
    void OpenTreeNodes(int treeIndex)
    {
        selectedTreeIndex = treeIndex;
        string treeName = selectedCharacter.playerStats.skillTrees.trees[treeIndex].treeName;
        host.OpenScreen(TreeNodeMenu(), treeName);
    }
    List<MenuOption> TreeNodeMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        SkillTree tree = selectedCharacter.playerStats.skillTrees.trees[selectedTreeIndex];
        int points = selectedCharacter.GetTreePoints(selectedTreeIndex);
        MenuOption spendOption = new MenuOption($"Spend Point ({selectedCharacter.skillPoints} available)", SpendPoint);
        spendOption.enabled = selectedCharacter.skillPoints > 0;
        options.Add(spendOption);
        foreach(SkillTreePath path in tree.paths)
        {
            bool unlocked = selectedCharacter.IsPathUnlocked(path);
            string status = unlocked ? "Unlocked" : $"{points}/{path.pointsRequired}";
            MenuOption nodeOption = new MenuOption($"{path.pathName} ({status})", () => { });
            nodeOption.enabled = false;
            options.Add(nodeOption);
        }
        return options;
    }
void SpendPoint()
    {
        selectedCharacter.SpendSkillPoint(selectedTreeIndex);
        UpdateStatsExtraPanel();
        host.RefreshScreen(TreeNodeMenu());
    }
    void OpenBondMenu()
    {
        inAbilitiesTab = false;
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ShowListPanel();
        host.SetStatsExtraPanelActive(false);
        host.SwitchTab(PartnerMenu(), "Bond", characterDetailAnchorDepth);
    }
    List<MenuOption> PartnerMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (BondData data in selectedCharacter.playerStats.bonds)
        {
            BondRank rank = selectedCharacter.GetBondRank(data.partner);
            string label = $"{data.partner.characterName} ({rank})";
            BondData capturedData = data;
            MenuOption option = new MenuOption(label, () => OpenConversationList(capturedData));
            options.Add(option);
        }
        return options;
    }
    void OpenConversationList(BondData data)
    {
        host.OpenScreen(ConversationMenu(data), data.partner.characterName);
    }
    List<MenuOption> ConversationMenu(BondData data)
    {
        List<MenuOption> options = new List<MenuOption>();
        BondProgress progress = selectedCharacter.GetBondProgress(data.partner);
        BondRank currentRank = selectedCharacter.GetBondRank(data.partner);
        for(int i = 0; i < data.conversations.Count; i++)
        {
            BondConversation conversation = data.conversations[i];
            bool viewed = progress.conversationsViewed[i];
            bool available = !viewed && currentRank >= conversation.requiredRank;
            int capturedIndex = i;
            BondConversation capturedConversation = conversation;
            MenuOption option = new MenuOption(
                viewed ? $"{conversation.requiredRank} Rank (Viewed)" : $"{conversation.requiredRank} Rank",
                () => PlayConversation(data, capturedConversation, capturedIndex));
                option.enabled = available;
                options.Add(option);
        }
            return options;
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
        host.RefreshScreen(ConversationMenu(data));
    }
    void OpenInventoryTab()
    {
        inAbilitiesTab = false;
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.ShowListPanel();
        host.SetStatsExtraPanelActive(false);
        host.SwitchTab(PersonalInventoryOptions(), "Inventory", characterDetailAnchorDepth);
    }
    List<MenuOption> PersonalInventoryOptions()
    {
        List<MenuOption> options = new List<MenuOption>();
        options.Add(new MenuOption($" Add from Party Inventory ({selectedCharacter.personalInventory.FreeSlots} free)", OpenAddFromPartyInventory));
        foreach(Baggable item in selectedCharacter.personalInventory.items)
        {
            Baggable captured = item;
            bool isEquipped = captured is Equipment equipment && IsEquippedAnywhere(equipment);
            string label = isEquipped ? $"{captured.DisplayName} (Equipped)" : captured.DisplayName;
            options.Add(new MenuOption(label, () => OpenPersonalItemActions(captured)));
        }
        return options;
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
        List<MenuOption> options = new List<MenuOption>();
        if(item is Equipment equipment && IsEquippedAnywhere(equipment))
        options.Add(new MenuOption("Currently equipped - unequip from stats tab first", () => { }) {enabled = false});
        else options.Add(new MenuOption("Send To Party", SendSelectedItemToParty));
        if(item is Item usable && usable.itemType == Item.ItemType.Consumable)
        options.Add(new MenuOption("Use", UsePersonalItem));
        host.OpenScreen(options, item.DisplayName);
    }
    void SendSelectedItemToParty()
    {
        if(InventoryManager.Instance != null) InventoryManager.Instance.RemovePersonalInventory(selectedPersonalItem, selectedCharacter);
        UpdateStatsExtraPanel();
        host.PopAndRefresh(PersonalInventoryOptions());
    }
    void UsePersonalItem()
    {
        if(selectedPersonalItem is Item item)
        {
            if(item.effects != null)
            foreach(Effect effect in item.effects) if(effect != null) StartCoroutine(effect.Apply(selectedCharacter, selectedCharacter));
            if(item.itemType != Item.ItemType.KeyItem) selectedCharacter.personalInventory.RemoveItem(item);
        }
        UpdateStatsExtraPanel();
        host.PopAndRefresh(PersonalInventoryOptions());
    }
    void OpenAddFromPartyInventory()
    {
        host.OpenScreen(PartyInventoryPickerOptions(), "Party Inventory");
    }
    List<MenuOption> PartyInventoryPickerOptions()
    {
        List<MenuOption> options = new List<MenuOption>();
        if(InventoryManager.Instance == null) return options;
        foreach(Baggable item in InventoryManager.Instance.items)
        {
            Baggable captured = item;
            options.Add(new MenuOption(captured.DisplayName, () => AddItemToSelectedCharacter(captured)));
        }
        if(options.Count == 0) options.Add(new MenuOption("Party Inventory is empty. =", () => { }) {enabled = false});
        return options;
    }
    void AddItemToSelectedCharacter(Baggable item)
    {
        if(selectedCharacter.personalInventory.CanAdd() && InventoryManager.Instance != null)
        InventoryManager.Instance.AddPersonalInventory(item, selectedCharacter);
        host.PopAndRefresh(PersonalInventoryOptions());
    }
    void OpenInfoTab()
    {
        inAbilitiesTab = false;
        if(host.microTabGroup != null) host.microTabGroup.Hide();
        host.PopScreenHistoryTo(characterDetailAnchorDepth);
        host.ShowInfoPanel();
        host.ClearMenuEntries();
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
