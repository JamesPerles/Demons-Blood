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
    const string PlayerStatsFolder = "PlayerStats";
    string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    void Awake()
    {
        if(instance == null) {instance = this; DontDestroyOnLoad(gameObject);}
        else Destroy(gameObject);
    }
    public bool SaveExists() => File.Exists(SavePath);
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.gold = Wallet.instance.currentGold;
        data.inventoryItemNames = new List<string>();
        foreach(Item item in InventoryManager.Instance.items)
        if(item != null) data.inventoryItemNames.Add(item.name);
        data.invetoryEquipment = new List<EquipmentSaveData>();
        foreach(Equipment equipment in EquipmentManager.instance.equipment)
        data.invetoryEquipment.Add(SaveEquipment(equipment));
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
        data.activeQuests.Add(new QuestProgressSaveData
        {questName = progress.quest.name, currentObjective = progress.currentObjective, isComplete = progress.isComplete});
data.completedQuests = new List<string>();
foreach (var quest in QuestManager.instance.completedQuests)
data.completedQuests.Add(quest.name);
data.discoveredEnemies = BestiaryManager.instance.GetDiscoveredIDs();
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
                pauseMenuPanelColor = SettingsManager.instance.pauseMenuPanelColor
            };
        }
        data.sceneName = SceneManager.GetActiveScene().name;
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
        string json = JsonUtility.ToJson(data,true);
        File.WriteAllText(SavePath,json);
        Debug.Log($"Game saved to{SavePath}");
    }
    EquipmentSaveData SaveEquipment(Equipment equipment)
    {
        if (equipment == null) return null;
        return new EquipmentSaveData
        {
            baseAssetName = string.IsNullOrEmpty(equipment.baseAssetName) ? equipment.name : equipment.baseAssetName,
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
        charac.weapon = SaveEquipment(character.weaponSlot);
        charac.head = SaveEquipment(character.headSlot);
        charac.body = SaveEquipment(character.bodySlot);
        charac.shield = SaveEquipment(character.shieldSlot);
        charac.accessory = SaveEquipment(character.accessorySlot);
        charac.learnedArts = character.learnedArts.Select(art => art.name).ToList();
        charac.learnedSpells = character.learnedSpells.Select(spell => spell.name).ToList();
        charac.learnedFusions = character.learnedFusions.Select(fusion => fusion.name).ToList();
        charac.learnedSkills = character.learnedSkills.Select(skill => skill.name).ToList();
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
    public void LoadGame()
    {
        if(!SaveExists()) {Debug.LogWarning("No save found."); return;}
        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        StartCoroutine(LoadingGame(data));
        IEnumerator LoadingGame(SaveData data)
        {
             Wallet.instance.SetGold(data.gold);
        InventoryManager.Instance.items.Clear();
        foreach(string itemName in data.inventoryItemNames)
        {
            Item item = Resources.Load<Item>($"{ItemsFolder}/{itemName}");
            if(item != null) InventoryManager.Instance.PickupItem(item);
            else Debug.LogWarning($"Could not find Item '{itemName}' in Resources/{ItemsFolder}");
        }
        EquipmentManager.instance.equipment.Clear();
        foreach(EquipmentSaveData equip in data.invetoryEquipment)
        {
            Equipment loaded = LoadEquipment(equip);
            if(loaded != null) EquipmentManager.instance.PickupEquipment(loaded);
        }
        List<GameObject> reordered = new List<GameObject>();
        foreach(string id in data.playablePartyOrder)
        {
            GameObject found = FindCharacter(id);
            if(found != null) reordered.Add(found);
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
            progress.currentObjective = questprog.currentObjective;
            progress.isComplete = questprog.isComplete;
            QuestManager.instance.activeQuests.Add(progress);
        }
        QuestManager.instance.completedQuests.Clear();
        foreach(string questName in data.completedQuests)
        {
            Quest quest = Resources.Load<Quest>($"{QuestsFolder}/{questName}");
            if(quest != null) QuestManager.instance.completedQuests.Add(quest);
        }
        BestiaryManager.instance.LoadDiscovered(data.discoveredEnemies);
        if(SettingsManager.instance != null) SettingsManager.instance.SettingsSave(data.settings);
        AsyncOperation operation = SceneManager.LoadSceneAsync(data.sceneName);//what is async??/
        while (!operation.isDone) yield return null;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if(playerObj != null)
            {
                playerObj.transform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
            }
            else
            {
                Debug.LogWarning("Could not find Player");
            }
        Debug.Log("Game Loaded");
    }
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
        character.currentHP = charsave.currentHP;
        character.currentMP = charsave.currentMP;
        character.currentExperience = charsave.currentExperience;
        character.currentExpToNextLevel = charsave.currentExpToNextLevel;
        character.currentLevel = charsave.currentLevel;
        character.currentMagicAffinity = charsave.magicAffinity;
        character.weaponSlot = LoadEquipment(charsave.weapon);
        character.headSlot = LoadEquipment(charsave.head);
        character.bodySlot = LoadEquipment(charsave.body);
        character.shieldSlot = LoadEquipment(charsave.shield);
        character.accessorySlot = LoadEquipment(charsave.accessory);
        character.learnedSpells.Clear();
        foreach(string numbr in charsave.learnedSpells){var spell = Resources.Load<Spell>($"{SpellsFolder}/{numbr}"); if(spell != null) character.learnedSpells.Add(spell);}
       character.learnedArts.Clear();
       foreach(string numbr in charsave.learnedArts){var art = Resources.Load<Art>($"{ArtsFolder}/{numbr}"); if(art != null) character.learnedArts.Add(art);}
       character.learnedFusions.Clear();
       foreach(string numbr in charsave.learnedFusions){var fusion = Resources.Load<Fusion>($"{FusionsFolder}/{numbr}"); if(fusion != null) character.learnedFusions.Add(fusion);}
      character.learnedSkills.Clear();
      foreach(string numbr in charsave.learnedSkills){var skill = Resources.Load<Skill>($"{SkillsFolder}/{numbr}"); if(skill != null) character.learnedSkills.Add(skill);}
       for(int i = 0; i < character.skillSlots.Length && i < charsave.skillSlots.Count; i++)
       character.skillSlots[i] = string.IsNullOrEmpty(charsave.skillSlots[i]) ? null : Resources.Load<Skill>($"{SkillsFolder}/{charsave.skillSlots[i]}");
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
}