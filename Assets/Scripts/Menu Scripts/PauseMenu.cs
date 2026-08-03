using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PauseMenu : MenuBase
{
public static PauseMenu instance;
public TextMeshProUGUI walletText;
public KeyCode pauseKey = KeyCode.Escape;
public bool pauseTimeScale = true;
public bool isOpen {get; private set;} = false;
public Image panelBackground;
protected Stack<MenuScreen> screenHistory = new Stack<MenuScreen>();
void Awake()
    {
        if(instance == null) instance = this; else Destroy(gameObject);
     }
     void Start()
    {
        SetDisplayActive(false);
     }
     void Update()
    {
        if(BattleManager.instance != null) return;
        if(Input.GetKeyDown(pauseKey))
        {
            if(isOpen) Close();
            else Open(); 
            return;
        }
        if(!isOpen) return;
        if(Input.GetKeyDown(KeyCode.Backspace))
        {
            if(screenHistory.Count > 1) PreviousScreen();
            else Close();
        }
    }
    public void Open()
    {
        isOpen = true;
       SetDisplayActive(true);
        if(pauseTimeScale) Time.timeScale = 0f;
        UpdateWallet();
        ApplyPanelColor();
        screenHistory.Clear();
        OpenScreen(TopLevelMenu());
    } 
    public void Close()
    {
        isOpen = false;
        SetDisplayActive(false);
        if(pauseTimeScale) Time.timeScale = 1f;
        ClearEntries();
        screenHistory.Clear();
    }
    void UpdateWallet()
    {
        if (walletText != null && Wallet.instance != null)
        walletText.text = $"{Wallet.instance.currentGold} Gold";
    }
    void ApplyPanelColor()
    {
        if (panelBackground != null && SettingsManager.instance != null)
        panelBackground.color = SettingsManager.instance.pauseMenuPanelColor;
    }
    protected void OpenScreen(List<MenuOption> options)
    {
        MenuScreen screen = new MenuScreen(options, fontSize, 1, cellSize, spacing);
        screenHistory.Push(screen);
        FillMenu(screen);
    }
    protected void PreviousScreen()
    {
        screenHistory.Pop();
        if(screenHistory.Count == 0) {Close(); return;}
        FillMenu(screenHistory.Peek());
    }
    protected void RefreshCurrentScreen()
    {
        FillMenu(screenHistory.Peek());
    }
    protected void FillMenu(MenuScreen screen)
{
    ClearEntries();
    ApplyGridSizing(screen.columns);
    if(screen.allOptions.Count == 0)
    {
        EmptyMenu(screen.fontSize);
        return;
    }
    foreach (MenuOption option in screen.allOptions)
        {
            GameObject entry = Instantiate(entryPrefab, optionsGrid);
            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if(label != null) {label.text = option.label; label.fontSize = screen.fontSize;}
            MenuOption captured = option;
            Button button = entry.GetComponent<Button>();
            button.interactable = option.enabled;
            button.onClick.AddListener(() => SelectCommand(captured));
            spawnedEntries.Add(entry);
        }
}
void SelectCommand(MenuOption option)
    {
        if(option.getChildren != null) OpenScreen(option.getChildren());
        else option.onSelect();
    }
    protected void EmptyMenu(float fontSize)
    {
        GameObject emptyEntry = Instantiate(entryPrefab, optionsGrid);
        var label = emptyEntry.GetComponentInChildren<TextMeshProUGUI>();
        if(label != null) {label.text = "Empty"; label.fontSize = fontSize;} 
        emptyEntry.GetComponent<Button>().interactable = false;
        spawnedEntries.Add(emptyEntry);
    }
     List<MenuOption> TopLevelMenu()
    {
        return new List<MenuOption>
        {
        new MenuOption("Party", OpenPartyMenu),
        new MenuOption("Items", ItemListMenu), 
        new MenuOption("Quests", QuestCategoryMenu),
        new MenuOption("Forge", OpenForgeMenu),
        new MenuOption("Bond", OpenBondMenu),
        new MenuOption("Bestiary", OpenBestiaryMenu),
        new MenuOption("Save", SaveGame),
        new MenuOption("Settings", OpenSettingsMenu),
        new MenuOption("Quit Game", QuitGame)
        };
    }
    void SaveGame()
    {
        if(SaveManager.instance != null) SaveManager.instance.SaveGame();
       else Debug.LogWarning("SaveManager missing Saving failed."); 
    }
     void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else 
        Application.Quit();
        #endif
    }
    public TextMeshProUGUI itemFeedbackText;
    Item selectedItem;
     List<MenuOption> ItemListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (var item in InventoryManager.Instance.items)
        {
            Item captured = item;
            options.Add(new MenuOption(captured.itemName, () => ChooseItemTarget(captured)));
        }
        return options;
    }
    void ChooseItemTarget(Item item)
    {
        selectedItem = item;
        OpenScreen(ItemTargets());
    }
    List<MenuOption> ItemTargets()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (GameObject characterObject in PlayerParty.instance.playableCharacters)
        {
            ActiveStats character = characterObject.GetComponent<ActiveStats>();
            if(character == null) continue;
            ActiveStats captured = character;
            options.Add(new MenuOption($"{captured.currentName}HP:{captured.currentHP}/{captured.finalHP}", () => UseItem(captured)));
        }
        return options;
    }
    void UseItem(ActiveStats target)
    {
        Item item = selectedItem;
        if(item.effects != null) foreach (Effect effect in item.effects) if(effect != null) StartCoroutine(effect.Apply(target,target));
        InventoryManager.Instance.LoseItem(item);
        if(itemFeedbackText != null) itemFeedbackText.text = $"Used {item.itemName} on {target.currentName}!";
        screenHistory.Pop();
        OpenScreen(ItemListMenu());
    }
    QuestProgress selectedQuest;
    List<MenuOption> QuestCategoryMenu()
    {
        return new List<MenuOption>
        {
            new MenuOption("Main Quests", MainQuestList),
            new MenuOption("Side Quests", SideQuestList)
        };
    }
    List<MenuOption> MainQuestList() => QuestListMenu(true);
    List<MenuOption> SideQuestList() => QuestListMenu(false);
    List<MenuOption> QuestListMenu(bool mainQuests)
    {
        List<MenuOption> options = new List<MenuOption>();
        if(QuestManager.instance == null) return options;
        foreach(QuestProgress progress in QuestManager.instance.activeQuests)
        {
            if(progress.quest.isMainQuest != mainQuests) continue;
            QuestProgress captured = progress;
            options.Add(new MenuOption(captured.quest.questName, () => OpenQuestDetail(captured)));
        }
        return options;
    }
    void OpenQuestDetail(QuestProgress progress)
    {
        selectedQuest = progress;
        OpenScreen(QuestDetailMenu());
    }
    List<MenuOption> QuestDetailMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        Quest quest = selectedQuest.quest;
        options.Add(new MenuOption(quest.description, () => {}) {enabled = false});
        for(int i = 0; i < quest.objectives.Count; i++)
        {
            string prefix = i < selectedQuest.currentObjective ? "[x]" : (i == selectedQuest.currentObjective ? "[>]" : "[]");
            options.Add(new MenuOption(prefix + quest.objectives[i].description, () => { }) {enabled = false});
        }
        return options;
    }
   public TextMeshProUGUI statsText;
ActiveStats selectedCharacter;
Equipment.EquipmentType selectedSlotType;
int selectedTreeIndex; 
List<MenuOption> OpenPartyMenu()
    {
        if(statsText != null) statsText.text = "";
        return PlayerPartyMenu();
    }
      List<MenuOption> PlayerPartyMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
        {
            ActiveStats character = characterObject.GetComponent<ActiveStats>();
            if(character == null) continue;
            ActiveStats captured = character;
            options.Add(new MenuOption($"{captured.currentName} Lv.{captured.currentLevel}", () => OpenCharacter(captured)));
        }
        return options;
    }
    void OpenCharacter(ActiveStats character)
    {
        selectedCharacter = character;
        OpenScreen(CharacterMenu());
    }
    List<MenuOption> CharacterMenu()
    {
        UpdateStats();
        List<MenuOption> options = new List<MenuOption>();
        options.Add(SlotOption(Equipment.EquipmentType.Weapon, "Weapon"));
        options.Add(SlotOption(Equipment.EquipmentType.Head, "Head"));
        options.Add(SlotOption(Equipment.EquipmentType.Body, "Body"));
        options.Add(SlotOption(Equipment.EquipmentType.Shield, "Shield"));
        options.Add(SlotOption(Equipment.EquipmentType.Accessory, "Accessory"));
        options.Add(new MenuOption("Swap", SwapMenu));
        options.Add(new MenuOption($"Skills ({selectedCharacter.skillPoints} pts)", SkillTreeMenu));
    return options;
    }
    void UpdateStats()
    {
        if (statsText == null || selectedCharacter == null) return;
        ActiveStats character = selectedCharacter;
        string weaponTypes = string.Join(", ", character.playerStats.allowedWeaponTypes);
        statsText.text = 
        $"{character.currentName} Lv.{character.currentLevel} HP: {character.currentHP}/{character.finalHP} MP: {character.currentMP}/{character.finalMP}\n" +
        $"STR: {character.finalStrength} MAG: {character.finalMagic} Def: {character.finalDefense} Wis: {character.finalWisdom} Tech: {character.finalTech}\n" +
        $"AFF: {character.finalAffinity} SPD: {character.finalSpeed} LUCK: {character.finalLuck} Can Equip: {weaponTypes}";
    }
    MenuOption SlotOption(Equipment.EquipmentType slotType, string label)
    {
        Equipment equipped = selectedCharacter.GetEquipped(slotType);
        string entryLabel = $"{label}: {(equipped != null ? equipped.equipmentName : "Empty")}";
        return new MenuOption(entryLabel, () => OpenEquipmentPicker(slotType));
    }
    void OpenEquipmentPicker(Equipment.EquipmentType slotType)
    {
        selectedSlotType = slotType;
        OpenScreen(EquipmentMenu());
    }
    List<MenuOption> EquipmentMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        Equipment currentlyEquipped = selectedCharacter.GetEquipped(selectedSlotType);
        if(currentlyEquipped != null) options.Add(new MenuOption($"Unequip{currentlyEquipped.equipmentName}", UnequipEquipment));
        List<Equipment> matching = EquipmentManager.instance.equipment.FindAll(equipment => equipment.equipmentType == selectedSlotType);
        if(selectedSlotType == Equipment.EquipmentType.Weapon) matching = matching.FindAll
        (equipment => selectedCharacter.playerStats.allowedWeaponTypes.Contains(equipment.weaponType));
        foreach(Equipment item in matching)
        {
            Equipment captured = item;
            string label = selectedSlotType == Equipment.EquipmentType.Weapon ? $"{captured.equipmentName} ({captured.weaponType})" : captured.equipmentName;
            options.Add(new MenuOption (label, () => EquipEquipment(captured)));
        }
        return options;
    }
void EquipEquipment(Equipment newEquipment)
    {
        Equipment previous = selectedCharacter.Equip(newEquipment);
        EquipmentManager.instance.LoseEquipment(newEquipment);
        if(previous != null)
        EquipmentManager.instance.PickupEquipment(previous);
        screenHistory.Pop();
        OpenScreen(CharacterMenu());
    }
    void UnequipEquipment()
    {
        Equipment removed = selectedCharacter.GetEquipped(selectedSlotType);
        selectedCharacter.Unequip(selectedSlotType);
        if(removed != null) EquipmentManager.instance.PickupEquipment(removed);
        screenHistory.Pop();
        OpenScreen(CharacterMenu());
    }
    bool IsActive(GameObject characterObject)
    {
        return System.Array.IndexOf(PlayerParty.instance.ActiveParty, characterObject) >= 0;
    }
    List<MenuOption> SwapMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        GameObject selectedObject = selectedCharacter.gameObject;
        bool selectedIsActive = IsActive(selectedObject);
        foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
        {
           if(characterObject == selectedObject) continue;
           if(IsActive(characterObject) == selectedIsActive) continue;
           ActiveStats other = characterObject.GetComponent<ActiveStats>();
           if(other == null) continue;
           GameObject capturedObject = characterObject;
           options.Add(new MenuOption($"{other.currentName} Lv.{other.currentLevel}", () => DoSwap(capturedObject))); 
        }
        return options;
    }
    void DoSwap(GameObject other)
    {
        PlayerParty.instance.Swap(selectedCharacter.gameObject, other);
        screenHistory.Pop();
        OpenScreen(CharacterMenu());
    }
    List<MenuOption> SkillTreeMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        if(selectedCharacter.skillTrees == null) return options;
        for(int i = 0; i < selectedCharacter.skillTrees.trees.Count; i++)
        {
            SkillTree tree = selectedCharacter.skillTrees.trees[i];
            int capturedIndex = i;
            int points = selectedCharacter.GetTreePoints(i);
            options.Add(new MenuOption($"{tree.treeName} ({points} pts)", () => OpenTreeNodes(capturedIndex)));
        }
        return options;
    }
    void OpenTreeNodes(int treeIndex)
    {
        selectedTreeIndex = treeIndex;
        OpenScreen(TreeNodeMenu());
    }
    List<MenuOption> TreeNodeMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        SkillTree tree = selectedCharacter.skillTrees.trees[selectedTreeIndex];
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
        screenHistory.Pop();
        OpenScreen(TreeNodeMenu());
    }
    PlayerStats selectedBondPartner;
    List<MenuOption> OpenBondMenu()
    {
        return BondRosterMenu();
    }
     List<MenuOption> BondRosterMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
        {
            ActiveStats character = characterObject.GetComponent<ActiveStats>();
            if(character == null) continue;
            ActiveStats captured = character;
            options.Add(new MenuOption(captured.currentName, () => OpenPartnerList(captured)));
        }
        return options;
    } 
      void OpenPartnerList(ActiveStats character)
    {
        selectedCharacter = character;
        OpenScreen(PartnerMenu());
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
        selectedBondPartner = data.partner;
        OpenScreen(ConversationMenu(data));
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
        screenHistory.Pop();
        OpenScreen(ConversationMenu(data));
    }
    public TextMeshProUGUI bestiaryDetailText;
    List<MenuOption> OpenBestiaryMenu()
    {
        if(bestiaryDetailText != null) bestiaryDetailText.text = "";
        return EnemyListMenu();
    }
      List<MenuOption> EnemyListMenu()
        {
            List<MenuOption> options = new List<MenuOption>();
            foreach (EnemyStats enemy in BestiaryManager.instance.allEnemies)
            {
                EnemyStats captured = enemy;
                bool discovered = BestiaryManager.instance.IsDiscovered(enemy);
                string label = discovered ? enemy.enemyName : "???";
                options.Add(new MenuOption(label, () => ShowDetail(captured)));
            }
            return options;
        }
        void ShowDetail(EnemyStats enemy)
        {
            if(bestiaryDetailText == null) return;
            bool discovered = BestiaryManager.instance.IsDiscovered(enemy);
            bestiaryDetailText.text = discovered
            ? $"{enemy.enemyName}\nLv.{enemy.level}\nHP: {enemy.hp} MP: {enemy.mp}\nSTR: {enemy.strength} MAG: {enemy.magic} DEF: {enemy.defense} WIS: {enemy.wisdom}\n\n{enemy.dexEntry}"
            : "Not yet encountered.";
        }
public TextMeshProUGUI forgeGoldText;
public TextMeshProUGUI forgeFeedbackText;
public int enhanceBaseCost = 100;
public int enhanceCostPerLevel = 50;
public int enhanceStrengthGain = 2;
public int maxEnhancementLevel = 10;
public int addElementCost = 200;
Equipment selectedWeaponForElement;
public List<CraftRecipe> craftRecipes = new List<CraftRecipe>();
public List<AlchemyRecipe> alchemyRecipes = new List<AlchemyRecipe>();
Item selectedFirstItem;
List<MenuOption> OpenForgeMenu()
    {
        if(forgeFeedbackText != null) forgeFeedbackText.text = "";
        UpdateForgeGoldText();
        return ForgeMainMenu();
    }
     void UpdateForgeGoldText()
    {
        if(forgeGoldText != null && Wallet.instance != null) forgeGoldText.text = $"{Wallet.instance.currentGold} Gold"; 
    }
    bool TrySpendForge(int cost)
    {
        if(Wallet.instance == null || !Wallet.instance.SpendGold(cost))
        {
            if(forgeFeedbackText != null) forgeFeedbackText.text = "Not enough gold.";
            return false;
        }
        UpdateForgeGoldText();
        return true;
    }
     List<MenuOption> ForgeMainMenu()
    {
        return new List<MenuOption>
        {
            new MenuOption("Enhance Weapon", EnhanceListMenu), new MenuOption("Add Element", ElementListMenu),
            new MenuOption("Smelt and Craft", SmeltCraftMenu), new MenuOption("Alchemy", AlchemyFirstItemMenu)
        };
    }
    List<MenuOption> EnhanceListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Equipment equipment in EquipmentManager.instance.equipment)
        {
            if(equipment.equipmentType != Equipment.EquipmentType.Weapon) continue;
            Equipment captured = equipment;
            int cost = EnhanceCost(captured.enhancementLevel);
            bool atMax = captured.enhancementLevel >= maxEnhancementLevel;
            string label = atMax ? $"{captured.equipmentName} (MAX)" : $"{captured.equipmentName} - STR {captured.strength} -> {captured.strength + enhanceStrengthGain} ({cost}g)";
        MenuOption option = new MenuOption(label, () => EnhanceWeapon(captured));
        option.enabled = !atMax;
        options.Add(option);
        }
         foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
    {
        ActiveStats character = characterObject.GetComponent<ActiveStats>();
        if(character == null || character.weaponSlot == null) continue;
        Equipment equipped = character.weaponSlot;
        ActiveStats capturedOwner = character;
        int cost = EnhanceCost(equipped.enhancementLevel);
        bool atMax = equipped.enhancementLevel >= maxEnhancementLevel;
        string label = atMax 
        ? $"{equipped.equipmentName} (Equipped: {character.currentName}) (MAX)" : 
        $"{equipped.equipmentName} (Equipped: {character.currentName}) - STR {equipped.strength} -> {equipped.strength + enhanceStrengthGain} ({cost}g)";
        MenuOption option = new MenuOption(label, () => EnhanceWeapon(equipped, capturedOwner));
        option.enabled = !atMax;
        options.Add(option);
    }
    return options;
    }
    int EnhanceCost(int currentLevel) => enhanceBaseCost + (currentLevel * enhanceCostPerLevel);
    void EnhanceWeapon(Equipment original, ActiveStats owner = null)
    {
        int cost = EnhanceCost(original.enhancementLevel);
        if (!TrySpendForge(cost)) return;
        Equipment enhanced = Instantiate(original);
        enhanced.baseAssetName = string.IsNullOrEmpty(original.baseAssetName) ? original.name : original.baseAssetName;
        enhanced.strength += enhanceStrengthGain;
        enhanced.enhancementLevel = original.enhancementLevel + 1;
        enhanced.equipmentName = StripSuffix(original.equipmentName) + $" +{enhanced.enhancementLevel}";
        if(owner != null)
        {
            owner.Equip(enhanced);
        }
        else
         {
        EquipmentManager.instance.LoseEquipment(original);
        EquipmentManager.instance.PickupEquipment(enhanced); 
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Enhanced to {enhanced.equipmentName}.";
   screenHistory.Pop();
   OpenScreen(EnhanceListMenu());
    }
    string StripSuffix(string name)
    {
        int plusIndex = name.LastIndexOf(" +");
        return plusIndex >= 0 ? name.Substring(0, plusIndex) : name;
    }
    List<MenuOption> ElementListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Equipment equipment in EquipmentManager.instance.equipment)
        {
            if(equipment.equipmentType != Equipment.EquipmentType.Weapon) continue;
            if(equipment.element != Element.None) continue;
            Equipment captured = equipment;
            options.Add(new MenuOption
            ($"{captured.equipmentName} ({addElementCost}g)", () => OpenElementPicker(captured)));
        }
        return options;
    }
      void OpenElementPicker(Equipment weapon)
    {
        selectedWeaponForElement = weapon;
        OpenScreen(ElementPickerMenu());
    }
    List<MenuOption> ElementPickerMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Element element in System.Enum.GetValues(typeof(Element)))
        {
            if(element == Element.None) continue;
            Element captured = element;
            options.Add(new MenuOption(captured.ToString(), () => AddElement(captured)));
        }
        return options;
    }
    void AddElement(Element element)
    {
        Equipment original = selectedWeaponForElement;
        if(!TrySpendForge(addElementCost)) return;
        Equipment enhanced = Instantiate(original);
        enhanced.baseAssetName = string.IsNullOrEmpty(original.baseAssetName) ? original.name : original.baseAssetName;
        enhanced.element = element;
        enhanced.equipmentName = $"{original.equipmentName} ({element})";
        EquipmentManager.instance.LoseEquipment(original);
        EquipmentManager.instance.PickupEquipment(enhanced);
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"{enhanced.equipmentName} imbued with {element}";
        screenHistory.Pop();
        screenHistory.Pop();
        OpenScreen(ElementListMenu());
    }
     List<MenuOption> SmeltCraftMenu()
    {
        return new List<MenuOption>
        {
            new MenuOption("Smelt", SmeltListMenu),
            new MenuOption("Craft", CraftListMenu)
        };
    }
    List<MenuOption> SmeltListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach(Equipment equipment in EquipmentManager.instance.equipment)
        {
            if(equipment.smeltYield == null || equipment.smeltYield.Count == 0) continue;
            Equipment captured = equipment;
            string yieldText = string.Join(", ", captured.smeltYield.Select(material => $"{material.amount}x {material.material.itemName}"));
        options.Add(new MenuOption($"{captured.equipmentName} -> {yieldText}", () => SmeltWeapons(captured)));
        }
        return options;
    }
    void SmeltWeapons(Equipment equipment)
    {
        EquipmentManager.instance.LoseEquipment(equipment);
        foreach (MaterialAmount material in equipment.smeltYield)
        {
            for(int i = 0; i < material.amount; i++) InventoryManager.Instance.PickupItem(material.material);
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Smelted{equipment.equipmentName}.";
   screenHistory.Pop();
   OpenScreen(SmeltListMenu());
    }
    List<MenuOption> CraftListMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (CraftRecipe recipe in craftRecipes)
        {
            if(recipe == null) continue;
            if(recipe.result == null && recipe.itemResult == null) continue;
            CraftRecipe captured = recipe;
            string resultName = recipe.result != null ? recipe.result.equipmentName : recipe.itemResult.itemName;
            string costText = string.Join(", ", captured.requiredMaterials.Select(materials => $"{materials.amount}x {materials.material.itemName}"));
     MenuOption option = new MenuOption($"{resultName} ({costText})", () => CraftEquipment(captured));
     option.enabled = HasMaterials(captured);
     options.Add(option);
        }
        return options;
    }
    bool HasMaterials(CraftRecipe recipe)
    {
        foreach(MaterialAmount required in recipe.requiredMaterials)
        {
            int owned = InventoryManager.Instance.items.FindAll(item => item == required.material).Count;
            if (owned < required.amount) return false;
        }
        return true;
    }
    void CraftEquipment(CraftRecipe recipe)
    {
        if(!HasMaterials(recipe)){if(forgeFeedbackText != null) forgeFeedbackText.text = "Missing materials."; return;}
  foreach(MaterialAmount required in recipe.requiredMaterials)
        {
            for(int i = 0; i < required.amount; i++) InventoryManager.Instance.LoseItem(required.material);
        }
        string craftedName;
        if(recipe.result != null)
        {
        Equipment crafted = Instantiate(recipe.result);
        EquipmentManager.instance.PickupEquipment(crafted);
        craftedName = crafted.equipmentName;
        }
        else
        {
            InventoryManager.Instance.PickupItem(recipe.itemResult);
            craftedName = recipe.itemResult.itemName;
        }
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"Crafted{craftedName}.";
        screenHistory.Pop();
        OpenScreen(CraftListMenu());
    }
    List<MenuOption> AlchemyFirstItemMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        foreach (Item item in InventoryManager.Instance.items)
        {
            Item captured = item;
            options.Add(new MenuOption(captured.itemName, () => OpenAlchemySecondItem(captured)));
        }
        return options;
    }
    void OpenAlchemySecondItem(Item first)
    {
        selectedFirstItem = first;
        OpenScreen(AlchemySecondItemMenu());
    }
    List<MenuOption> AlchemySecondItemMenu()
    {
        List<MenuOption> options = new List<MenuOption>();
        List<Item> remaining = new List<Item>(InventoryManager.Instance.items);
        remaining.Remove(selectedFirstItem);
        foreach (Item item in remaining)
        {
            Item captured = item;
            options.Add(new MenuOption(captured.itemName, () => CombineItems(captured)));
        }
        return options;
    }
     void CombineItems(Item second)
    {
        Item first = selectedFirstItem;
        AlchemyRecipe matched = alchemyRecipes.Find(recipe => recipe != null &&
        ((recipe.ingredientA == first && recipe.ingredientB == second) ||
        (recipe.ingredientA == second && recipe.ingredientB == first)));
        if(matched == null || matched.result == null)
        {
             if(forgeFeedbackText != null) forgeFeedbackText.text = $"{first.itemName} + {second.itemName} does nothing.";
            screenHistory.Clear();
            OpenScreen(ForgeMainMenu());
            return;
        }
        InventoryManager.Instance.LoseItem(first);
        InventoryManager.Instance.LoseItem(second);
        InventoryManager.Instance.PickupItem(matched.result);
        if(forgeFeedbackText != null) forgeFeedbackText.text = $"{first.itemName} + {second.itemName} became {matched.result.itemName}.";
    screenHistory.Clear();
    OpenScreen(ForgeMainMenu());
    }
    List<MenuOption> OpenSettingsMenu()
    {
        return SettingsMenuList();
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
        options.Add(new MenuOption($"Panel Color R: {Mathf.RoundToInt(setting.pauseMenuPanelColor.r * 100)}%", () => CyclePanelColorChannel(0)));
        options.Add(new MenuOption($"Panel Color G: {Mathf.RoundToInt(setting.pauseMenuPanelColor.g * 100)}%", () => CyclePanelColorChannel(1)));
        options.Add(new MenuOption($"Panel Color B: {Mathf.RoundToInt(setting.pauseMenuPanelColor.b * 100)}%", () => CyclePanelColorChannel(2)));
        
        return options;
    }
    void RefreshSettingsScreen()
    {
        screenHistory.Pop();
        OpenScreen(SettingsMenuList());
    }
    void CycleMusicVolume()
    {
        SettingsManager.instance.musicVolume = NextVolumeStep(SettingsManager.instance.musicVolume);
        RefreshSettingsScreen();
    }
    void CycleSfxVolume()
    {
        SettingsManager.instance.sfxVolume = NextVolumeStep(SettingsManager.instance.sfxVolume);
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
        Color c = SetChannel(SettingsManager.instance.pauseMenuPanelColor, channel, NextVolumeStep(GetChannel(SettingsManager.instance.pauseMenuPanelColor, channel)));
        SettingsManager.instance.pauseMenuPanelColor = c;
        ApplyPanelColor();
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



