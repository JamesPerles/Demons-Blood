using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    const string ItemsFolder = "Items";
    const string EquipmentFolder = "Equipment";
    const string SkillsFolder = "Skills";
    const string SpellsFolder = "Spells";
    const string ArtsFolder = "Arts";
    const string FusionsFolder = "Fusions";
    const string QuestsFolder = "Quests";
    const string PlayerStatsFolder = "PartyMembers";
    const string LocationsFolder = "Locations";
    const string MaterialsFolder = "Materials";
    string SavePath(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");
    [System.Serializable]
    public class SaveSlotSummary
    {
        public bool exists;
        public string sceneName;
        public int gold;
        public string savedAt;
        public string leadCharacterName = "";
        public int leadCharacterLevel;
    }
    void Awake()
    {
        if(instance == null) {instance = this; DontDestroyOnLoad(gameObject); StartCoroutine(CaptureDefaults());}
        else Destroy(gameObject);
    }
    public bool SaveExists(int slot = 0) => File.Exists(SavePath(slot));
    string defaultSnapshotJson;
    IEnumerator CaptureDefaults()
    {
        yield return null;
        yield return null;
        if(WalletManager.instance == null || InventoryManager.instance == null || PlayerParty.instance == null || 
        FlagManager.instance == null || QuestManager.instance == null || BestiaryManager.instance == null)
        {
            Debug.LogWarning("SaveManager: could not capture the default game state - a manager was missing at startup. New Game will not reset.");
            yield break;
        }
        defaultSnapshotJson = JsonUtility.ToJson(BuildSaveData());
    }
    public void ResetToDefaults()
    {
        if(string.IsNullOrEmpty(defaultSnapshotJson)) { Debug.LogWarning("SaveManager: no default snapshot - New Game did not reset anything."); return; }
        SaveData data = JsonUtility.FromJson<SaveData>(defaultSnapshotJson);
        data.settings = null;
        ApplyLoad(data);
    }

    SaveData BuildSaveData()
    {
        SaveData data = new SaveData();
        data.gold = WalletManager.instance.currentGold;
        data.inventoryItemNames = new List<string>();
        foreach(Item item in InventoryManager.instance.items.OfType<Item>())
        if(item != null) data.inventoryItemNames.Add(item.name);
        data.inventoryMaterialNames = new List<string>();
        foreach(Materials material in InventoryManager.instance.items.OfType<Materials>())
        if(material != null) data.inventoryMaterialNames.Add(material.name);
        data.inventoryEquipment = new List<EquipmentSaveData>();
        foreach(Equipment equipment in InventoryManager.instance.items.OfType<Equipment>())
        data.inventoryEquipment.Add(SaveEquipment(equipment));
        data.playablePartyOrder = new List<string>();
        data.characters = new List<CharacterSaveData>();
        foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
        {
            ActiveStats character =  characterObject.GetComponent<ActiveStats>();
            if(character == null) continue;
            data.playablePartyOrder.Add(character.playerStats.name);
            data.characters.Add(SaveCharacter(character));
        }
        data.flags = new List<FlagEntry>();
        foreach (var flag in FlagManager.instance.SaveFlags())
        data.flags.Add(new FlagEntry {key = flag.Key, value = flag.Value});
        data.activeQuests = new List<QuestProgressSaveData>();
        foreach (var progress in QuestManager.instance.activeQuests)
        data.activeQuests.Add(SaveQuestProgress(progress));
        data.completedQuests = new List<string>();
        foreach (var quest in QuestManager.instance.completedQuests)
        data.completedQuests.Add(quest.name);
        data.failedQuests = new List<string>();
        foreach(var quest in QuestManager.instance.failedQuests)
        data.failedQuests.Add(quest.name);
        data.discoveredEnemies = BestiaryManager.instance.GetDiscoveredIDs();
        data.discoveredLocations = new List<string>();
        foreach(LocationData location in Resources.LoadAll<LocationData>(LocationsFolder))
        if(location != null && location.IsDiscovered) data.discoveredLocations.Add(location.name);
        if(SettingsManager.instance != null)
        {
            data.settings = new SettingsSaveData
            {
                musicVolume = SettingsManager.instance.musicVolume,
                sfxVolume = SettingsManager.instance.sfxVolume,
                dialogueTextSpeed =SettingsManager.instance.dialogueTextSpeed,
                battleTextSpeed = SettingsManager.instance.battleTextSpeed,
                battleSpeedMultiplier = SettingsManager.instance.battleSpeedMultiplier,
                textColor = SettingsManager.instance.uiTextColor,
                menuPanelColor = SettingsManager.instance.menuPanelColor,
                menuBorderColor = SettingsManager.instance.menuBorderColor
            };
        }
        data.sceneName = SceneManager.GetActiveScene().name;
        data.lastTown = BattleManager.lastTown;
        data.savedAt = System.DateTime.Now.ToString("MMM d, h:mm tt");
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj != null)
        {
            Vector3 playerPos = playerObj.transform.position;
            data.playerPosX  = playerPos.x;
            data.playerPosY = playerPos.y;
            data.playerPosZ = playerPos.z;
        }
        else
        {
            Debug.LogWarning("Could not find object tagged Player/ postion not correctly saved.");
        }
        return data;
    }
    public void SaveGame(int slot = 0)
    {
        SaveData data = BuildSaveData();
        string json = JsonUtility.ToJson(data,true);
        File.WriteAllText(SavePath(slot),json);
        Debug.Log($"Game saved to{SavePath(slot)}");
    }
    string BaseAssetName(Equipment equipment)
    {
        string name = string.IsNullOrEmpty(equipment.baseAssetName) ? equipment.name : equipment.baseAssetName;
        return name.Replace("(Clone)", "").Trim();
    }
    QuestProgressSaveData SaveQuestProgress(QuestProgress progress)
    {
        QuestProgressSaveData saved = new QuestProgressSaveData
        {
            questName = progress.quest.name, currentStage = progress.currentStage,
            objectiveCounts = new List<int>(progress.objectiveCounts),
            chosenObjectivePerStage = new List<int>(progress.chosenObjectivePerStage),
            state = progress.state,
            optionalCounts = new List<int>(progress.optionalCounts),
            optionalClaimed = new List<bool>(progress.optionalClaimed),
            pendingOptionalGold = progress.pendingOptionalGold,
            pendingOptionalExp = progress.pendingOptionalExp
        };
        foreach(Item item in progress.pendingOptionalItems) if(item != null) saved.pendingOptionalItemNames.Add(item.name);
        foreach(Equipment equipment in progress.pendingOptionalEquipment) if(equipment != null) saved.pendingOptionalEquipmentNames.Add(equipment.name);
        return saved;
    }
    void LoadOptionalProgress(QuestProgress progress, QuestProgressSaveData saved)
    {
        if(saved.optionalCounts != null && saved.optionalCounts.Count == progress.optionalCounts.Count) progress.optionalCounts = new List<int>(saved.optionalCounts);
        if(saved.optionalClaimed != null && saved.optionalClaimed.Count == progress.optionalClaimed.Count) progress.optionalClaimed = new List<bool>(saved.optionalClaimed);
        progress.pendingOptionalGold = saved.pendingOptionalGold;
        progress.pendingOptionalExp = saved.pendingOptionalExp;
        progress.pendingOptionalItems = new List<Item>();
        if(saved.pendingOptionalItemNames != null)
        foreach(string itemName in saved.pendingOptionalItemNames)
        {
            Item item = Resources.Load<Item>($"{ItemsFolder}/{itemName}");
            if(item != null) progress.pendingOptionalItems.Add(item);
        }
        progress.pendingOptionalEquipment = new List<Equipment>();
        if(saved.pendingOptionalEquipmentNames != null)
        foreach(string equipmentName in saved.pendingOptionalEquipmentNames)
        {
            Equipment equipment = Resources.Load<Equipment>($"{EquipmentFolder}/{equipmentName}");
            if(equipment != null) progress.pendingOptionalEquipment.Add(equipment);
        }
    }
    EquipmentSaveData SaveEquipment(Equipment equipment)
    {
        if (equipment == null) return null;
        return new EquipmentSaveData
        {
            baseAssetName = BaseAssetName(equipment),
            equipmentName = equipment.equipmentName,
            strength = equipment.strength, enhancementLevel = equipment.enhancementLevel, element = equipment.element
        };
    }
    CharacterSaveData SaveCharacter(ActiveStats character)
    {
        CharacterSaveData charac = new CharacterSaveData();
        charac.character = character.playerStats.name;
        charac.currentHP = character.currentHP;
        charac.currentMP = character.currentMP;
        charac.currentExperience = character.currentExperience;
        charac.currentExpToNextLevel = character.currentExpToNextLevel;
        charac.currentLevel = character.currentLevel;
        charac.totalExperience = character.totalExperience;
        charac.baseStats = character.SaveBaseStats();
        charac.personalItemNames = new List<string>();
        charac.personalEquipment = new List<EquipmentSaveData>();
        foreach(Baggable carried in character.personalInventory.items)
        {
            if(carried is Equipment equipment)
            {
                int equippedSlot = -1;
                if(character.weaponSlot == equipment) equippedSlot = (int) Equipment.EquipmentType.Weapon;
                else if(character.headSlot == equipment) equippedSlot = (int) Equipment.EquipmentType.Head;
                else if(character.bodySlot == equipment) equippedSlot = (int) Equipment.EquipmentType.Body;
                else if(character.shieldSlot == equipment) equippedSlot = (int) Equipment.EquipmentType.Shield;
                else if(character.accessorySlot == equipment) equippedSlot = (int) Equipment.EquipmentType.Accessory;
                charac.personalEquipment.Add(new EquipmentSaveData
                {
                    baseAssetName = BaseAssetName(equipment),
                    equipmentName = equipment.equipmentName,
                    strength = equipment.strength,
                    enhancementLevel = equipment.enhancementLevel,
                    element = equipment.element,
                    equippedSlot = equippedSlot
                });
            }
            else if(carried is Item item)
            {
                charac.personalItemNames.Add(item.name);
            }
            else if(carried is Materials material) charac.personalMaterialNames.Add(material.name);
        }
        charac.learnedArts = character.learnedArts.Select(art => art.name).ToList();
        charac.learnedSpells = character.learnedSpells.Select(spell => spell.name).ToList();
        charac.learnedFusions = character.learnedFusions.Select(fusion => fusion.name).ToList();
        charac.learnedSkills = character.learnedSkills.Select(skill => skill.name).ToList();
        charac.skillSlots = character.skillSlots.Select(skill => skill != null ? skill.name : "").ToList();
        charac.skillPoints = character.skillPoints;
        charac.treePoints = character.SaveTreePoints();
        charac.unlockedPaths = character.SavePaths();
        charac.magicAffinity = character.currentMagicAffinity;
        charac.bondProgress = new List<BondProgressSaveData>();
        foreach (var bondpartner in character.bondProgress)
        charac.bondProgress.Add(new BondProgressSaveData
        {
            partners = bondpartner.partner != null ? bondpartner.partner.name : "", points = bondpartner.points,
            conversationsViewed = new List<bool>(bondpartner.conversationsViewed)});
            return charac;
    }
    public void LoadGame(int slot = 0)
    {
        if(!SaveExists(slot)) {Debug.LogWarning("No save found."); return;}
        string json = File.ReadAllText(SavePath(slot));
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        StartCoroutine(LoadingGame(data));
    }
        void ApplyLoad(SaveData data)
        {
            WalletManager.instance.SetGold(data.gold);
        InventoryManager.instance.items.Clear();
        foreach(string itemName in data.inventoryItemNames)
        {
            Item item = Resources.Load<Item>($"{ItemsFolder}/{itemName}");
            if(item != null) InventoryManager.instance.PickupItem(item, false);
            else Debug.LogWarning($"Could not find Item '{itemName}' in Resources/{ItemsFolder}");
        }
        if(data.inventoryMaterialNames != null)
        foreach(string materialName in data.inventoryMaterialNames)
            {
                Materials material = Resources.Load<Materials>($"{MaterialsFolder}/{materialName}");
                if(material != null) InventoryManager.instance.PickupItem(material, false);
                else Debug.LogWarning($"Could not find Materials '{materialName}' in Resources/{MaterialsFolder}");
            }
        foreach(EquipmentSaveData equip in data.inventoryEquipment)
        {
            Equipment loaded = LoadEquipment(equip);
            if(loaded != null) InventoryManager.instance.PickupItem(loaded, false);
        }
        List<GameObject> reordered = new List<GameObject>();
        foreach(string id in data.playablePartyOrder)
        {
            GameObject found = FindCharacter(id);
            if(found != null) reordered.Add(found);
        }
        if(PlayerParty.instance != null)
        {
            foreach(GameObject currentCharacter in PlayerParty.instance.playableCharacters)
            if(currentCharacter != null && !reordered.Contains(currentCharacter)) reordered.Add(currentCharacter);
        }
        if (reordered.Count > 0) PlayerParty.instance.SetOrder(reordered.ToArray());
        foreach(CharacterSaveData charac in data.characters)
        {
            GameObject charObj = FindCharacter(charac.character);
            if(charObj == null) {Debug.LogWarning($"Could not find character '{charac.character}' in playablecharacters"); continue;}
            ActiveStats character = charObj.GetComponent<ActiveStats>();
            LoadCharacter(character, charac);
        }
        FlagManager.instance.LoadFlags(data.flags.ToDictionary(flag => flag.key, flag => flag.value));
        QuestManager.instance.activeQuests.Clear();
        foreach (QuestProgressSaveData questprog in data.activeQuests)
        {
            Quest quest = Resources.Load<Quest>($"{QuestsFolder}/{questprog.questName}");
            if(quest == null) {Debug.LogWarning($"Could not find Quest '{questprog.questName}'"); continue;}
            QuestProgress progress = new QuestProgress(quest);
            progress.currentStage = questprog.currentStage;
            progress.StageCount();
            progress.objectiveCounts = new List<int>(questprog.objectiveCounts);
            progress.chosenObjectivePerStage = new List<int>(questprog.chosenObjectivePerStage);
            progress.state = questprog.state;
            LoadOptionalProgress(progress, questprog);
            QuestManager.instance.activeQuests.Add(progress);
        }
        QuestManager.instance.completedQuests.Clear();
        foreach(string questName in data.completedQuests)
        {
            Quest quest = Resources.Load<Quest>($"{QuestsFolder}/{questName}");
            if(quest != null) QuestManager.instance.completedQuests.Add(quest);
        }
        QuestManager.instance.failedQuests.Clear();
        foreach(string questName in data.failedQuests)
        {
            Quest quest = Resources.Load<Quest>($"{QuestsFolder}/{questName}");
            if(quest != null) QuestManager.instance.failedQuests.Add(quest);
        }
        BestiaryManager.instance.LoadDiscovered(data.discoveredEnemies);
        LocationData.discoveredNames.Clear();
        foreach(LocationData location in Resources.LoadAll<LocationData>(LocationsFolder))
        if(location != null && data.discoveredLocations != null && 
        data.discoveredLocations.Contains(location.name)) LocationData.discoveredNames.Add(location.name);
        if(SettingsManager.instance != null) SettingsManager.instance.LoadSettings(data.settings);
        BattleManager.lastTown = string.IsNullOrEmpty(data.lastTown) ? null : data.lastTown;
        }
        IEnumerator LoadingGame(SaveData data)
        {
        ApplyLoad(data);
        Vector3 savedPlayerPos = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
        if(SceneFader.instance != null) SceneFader.instance.LoadScene(data.sceneName, () => LoadedPlayerLocation(savedPlayerPos));
        else yield return StartCoroutine(LoadSceneFallback(data.sceneName, savedPlayerPos));
        Debug.Log("Game Loaded");
    }
    void LoadedPlayerLocation(Vector3 savedPlayerPos)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj != null) playerObj.transform.position = savedPlayerPos;
        else Debug.LogWarning("Could not find Player");
    }
    IEnumerator LoadSceneFallback(string sceneName, Vector3 savedPlayerPos)
    {
         AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        while (!operation.isDone) yield return null;
        LoadedPlayerLocation(savedPlayerPos);
    }
    GameObject FindCharacter(string playerStatsName)
    {
        foreach(GameObject obj in PlayerParty.instance.playableCharacters)
        {
            ActiveStats stats = obj.GetComponent<ActiveStats>();
            if(stats != null && stats.playerStats.name == playerStatsName) return obj;
        }
        return null;
    }
    Equipment LoadEquipment(EquipmentSaveData data)
    {
        if(data == null || string.IsNullOrEmpty(data.baseAssetName)) return null;
        Equipment baseAsset = Resources.Load<Equipment>($"{EquipmentFolder}/{data.baseAssetName}");
        if(baseAsset == null) {Debug.LogWarning($"Could not find Equipment '{data.baseAssetName}' in Resources/{EquipmentFolder}"); return null;}
        Equipment instance = Instantiate(baseAsset);
        instance.baseAssetName = data.baseAssetName;
        instance.equipmentName = data.equipmentName;
        instance.strength = data.strength;
        instance.enhancementLevel = data.enhancementLevel;
        instance.element = data.element;
        return instance;
    }
    void LoadCharacter(ActiveStats character, CharacterSaveData charsave)
    {
        character.LoadBaseStats(charsave.baseStats);
        if(charsave.baseStats != null && charsave.baseStats.Count >= 10) character.totalExperience = charsave.totalExperience;
        character.currentHP = charsave.currentHP;
        character.currentMP = charsave.currentMP;
        character.currentExperience = charsave.currentExperience;
        character.currentExpToNextLevel = charsave.currentExpToNextLevel;
        character.currentLevel = charsave.currentLevel;
        character.currentMagicAffinity = charsave.magicAffinity;
        character.personalInventory.items.Clear();
        character.weaponSlot = null;
        character.headSlot = null;
        character.bodySlot = null;
        character.shieldSlot = null;
        character.accessorySlot = null;
        foreach(string itemName in charsave.personalItemNames)
        {
            Item item = Resources.Load<Item>($"{ItemsFolder}/{itemName}");
            if(item != null) character.personalInventory.AddItem(item);
            else Debug.LogWarning($"Could not find Item '{itemName}' in Resources/{ItemsFolder} for {charsave.character}'s personal inventory");
        }
        if(charsave.personalMaterialNames != null)
        foreach(string materialName in charsave.personalMaterialNames)
            {
                Materials material = Resources.Load<Materials>($"{MaterialsFolder}/{materialName}");
                if(material != null) character.personalInventory.AddItem(material);
                else Debug.LogWarning($"Could not find Materials '{materialName}' in Resources/{MaterialsFolder} for {charsave.character}'s personal inventory");
            }
        foreach(EquipmentSaveData equipData in charsave.personalEquipment)
        {
            Equipment loaded = LoadEquipment(equipData);
            if(loaded == null) continue;
            character.personalInventory.AddItem(loaded);
            if(equipData.equippedSlot == (int)Equipment.EquipmentType.Weapon) character.weaponSlot = loaded;
            else if(equipData.equippedSlot == (int)Equipment.EquipmentType.Head) character.headSlot = loaded;
            else if(equipData.equippedSlot == (int)Equipment.EquipmentType.Body) character.bodySlot = loaded;
            else if(equipData.equippedSlot == (int)Equipment.EquipmentType.Shield) character.shieldSlot = loaded;
            else if(equipData.equippedSlot == (int)Equipment.EquipmentType.Accessory) character.accessorySlot = loaded;
        }
        character.learnedSpells.Clear();
        foreach(string numbr in charsave.learnedSpells){var spell = 
        Resources.Load<Spell>($"{SpellsFolder}/{numbr}"); if(spell != null) character.learnedSpells.Add(spell);}
        character.learnedArts.Clear();
        foreach(string numbr in charsave.learnedArts){var art = 
        Resources.Load<Art>($"{ArtsFolder}/{numbr}"); if(art != null) character.learnedArts.Add(art);}
        character.learnedFusions.Clear();
        foreach(string numbr in charsave.learnedFusions){var fusion = 
        Resources.Load<Fusion>($"{FusionsFolder}/{numbr}"); if(fusion != null) character.learnedFusions.Add(fusion);}
        character.learnedSkills.Clear();
        foreach(string numbr in charsave.learnedSkills){var skill = 
        Resources.Load<Skill>($"{SkillsFolder}/{numbr}"); if(skill != null) character.learnedSkills.Add(skill);}
        for(int i = 0; i < character.skillSlots.Length && i < charsave.skillSlots.Count; i++)
        character.skillSlots[i] = string.IsNullOrEmpty
        (charsave.skillSlots[i]) ? null : Resources.Load<Skill>($"{SkillsFolder}/{charsave.skillSlots[i]}");
        character.skillPoints = charsave.skillPoints;
        character.LoadTreePoints(charsave.treePoints);
        foreach(Vector2Int pair in charsave.unlockedPaths)
        character.LoadPaths(pair.x, pair.y);
        foreach(BondProgressSaveData bondprogress in charsave.bondProgress)
        {
            PlayerStats partner = Resources.Load<PlayerStats>($"{PlayerStatsFolder}/{bondprogress.partners}");
            if(partner == null) continue;
            BondProgress progress = character.GetBondProgress(partner);
            if(progress == null) continue;
            progress.points = bondprogress.points;
            progress.conversationsViewed = new List<bool>(bondprogress.conversationsViewed);
        }
        character.RefreshStats();
    }
    public SaveSlotSummary GetSlotSummary(int slot)
    {
        SaveSlotSummary summary = new SaveSlotSummary();
        if(!SaveExists(slot)) return summary;
        try
        {
            string json = File.ReadAllText(SavePath(slot));
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            summary.exists = true;
            summary.sceneName = data.sceneName;
            summary.gold = data.gold;
            summary.savedAt = data.savedAt;
            if(data.characters != null && data.characters.Count > 0)
            {
                summary.leadCharacterName = data.characters[0].character;
                summary.leadCharacterLevel = data.characters[0].currentLevel;
            }
        }
        catch(System.Exception ex)
        {
            Debug.LogWarning($"Could not read save slot {slot} : {ex.Message}");
            summary.exists = false;
        }
        return summary;
    }
}