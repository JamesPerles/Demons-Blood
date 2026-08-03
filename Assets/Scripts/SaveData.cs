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
    public EquipmentSaveData weapon, head, body, shield, accessory;
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
}
[System.Serializable]
public class QuestProgressSaveData
{
    public string questName;
    public int currentObjective;
    public bool isComplete;
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
    public Color pauseMenuPanelColor;
}
[System.Serializable]
public class SaveData
{
 public int gold;
 public List<string> inventoryItemNames = new List<string>();
 public List<EquipmentSaveData> invetoryEquipment = new List<EquipmentSaveData>();
 public List<string> playablePartyOrder = new List<string>();
 public List<CharacterSaveData> characters = new List<CharacterSaveData>();
 public List<FlagEntry> flags = new List<FlagEntry>();
 public List<QuestProgressSaveData> activeQuests = new List<QuestProgressSaveData>();
 public List<string> completedQuests = new List<string>();
 public List<int> discoveredEnemies = new List<int>();
 public SettingsSaveData settings;
 public string sceneName;
 public float playerPosX, playerPosY, playerPosZ;
}
