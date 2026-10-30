using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class FlagEntry
{
    public string key;
    public bool value;
}
[System.Serializable]
public class EquipmentSaveData
{
    public string baseAssetName;
    public string equipmentName;
    public int strength;
    public int enhancementLevel;
    public Element element;
    public int equippedSlot = -1;
}
[System.Serializable]
public class BondProgressSaveData
{
    public string partners;
    public int points;
    public List<bool> conversationsViewed;
}
[System.Serializable]
public class CharacterSaveData
{
    public string character;
    public int currentHP;
    public int currentMP;
    public int currentExperience;
    public int currentExpToNextLevel;
    public int currentLevel;
    public List<string> personalItemNames = new List<string>();
    public List<string> personalMaterialNames = new List<string>();
    public List<EquipmentSaveData> personalEquipment = new List<EquipmentSaveData>();
    public List<string> learnedSpells;
    public List<string> learnedArts;
    public List<string> learnedFusions;
    public List<string> learnedSkills;
    public List<string> skillSlots;
    public int skillPoints;
    public List<int> treePoints;
    public List<Vector2Int> unlockedPaths;
    public List<BondProgressSaveData> bondProgress;
    public Element magicAffinity;
    public List<int> baseStats = new List<int>();
    public int totalExperience;
}
[System.Serializable]
public class QuestProgressSaveData
{
    public string questName;
    public int currentStage;
    public List<int> objectiveCounts = new List<int>();
    public List<int> chosenObjectivePerStage = new List<int>();
    public QuestState state;
    public List<int> optionalCounts = new List<int>();
    public List<bool> optionalClaimed = new List<bool>();
    public int pendingOptionalGold;
    public int pendingOptionalExp;
    public List<string> pendingOptionalItemNames = new List<string>();
    public List<string> pendingOptionalEquipmentNames = new List<string>();
}
[System.Serializable]
public class SettingsSaveData
{
    public float musicVolume;
    public float sfxVolume;
    public float dialogueTextSpeed;
    public float battleTextSpeed;
    public float battleSpeedMultiplier;
    public Color textColor;
    public Color menuPanelColor = new Color(0.04f, 0.04f, 0.04f, 1f);
    public Color menuBorderColor = new Color(0.42f, 0.08f, 0.08f, 1f);
}
[System.Serializable]
public class SaveData
{
 public int gold;
 public List<string> inventoryItemNames = new List<string>();
 public List<string> inventoryMaterialNames = new List<string>();
 public List<EquipmentSaveData> inventoryEquipment = new List<EquipmentSaveData>();
 public List<string> playablePartyOrder = new List<string>();
 public List<CharacterSaveData> characters = new List<CharacterSaveData>();
 public List<FlagEntry> flags = new List<FlagEntry>();
 public List<QuestProgressSaveData> activeQuests = new List<QuestProgressSaveData>();
 public List<string> completedQuests = new List<string>();
 public List<string> failedQuests = new List<string>();
 public List<int> discoveredEnemies = new List<int>();
 public List<string> discoveredLocations = new List<string>();
 public SettingsSaveData settings;
 public string sceneName;
 public float playerPosX, playerPosY, playerPosZ;
 public string savedAt;
 public string lastTown;
}
