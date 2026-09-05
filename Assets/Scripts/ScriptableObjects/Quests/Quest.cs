using System.Collections.Generic;
using UnityEngine;
public enum ObjectiveType{Flag, KillCount, CollectItem}
[System.Serializable]
public class OptionalObjective
{
[TextArea(1, 3)] public string description;
public ObjectiveType type;
public string flagKey;
public EnemyStats targetEnemy;
public Item targetItem;
public int requiredCount = 1;
public int bonusGoldReward;
public int bonusExpReward;
public List<Item> bonusItemRewards = new List<Item>();
public List<Equipment> bonusEquipmentRewards = new List<Equipment>();
}
[System.Serializable]
public class QuestObjective
{
[TextArea(1, 3)] public string description;
public ObjectiveType type;
public string flagKey;
public EnemyStats targetEnemy;
public Item targetItem;
public int requiredCount = 1;
public int choiceGoldReward;
public int choiceExpReward;
public List<Item> choiceItemRewards = new List<Item>();
public List<Equipment> choiceEquipmentRewards = new List<Equipment>();
public bool hasOptionalObjectives;
public List<OptionalObjective> optionalObjectives = new List<OptionalObjective>();
}
[System.Serializable]
public class QuestStage
    {
        public List<QuestObjective> objectives = new List<QuestObjective>();
        public bool isChoiceStage;
    }
[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string questName;
    [TextArea(2,5)] public string description;
    public bool isMainQuest;
    public string questGiver;
    public List<QuestStage> stages = new List<QuestStage>();
    public List<Quest> prerequisite = new List<Quest>();
    public string unlockFlagKey;
    public string failFlagKey;
    public bool requiresTurnIn;
    public string turnInFlagKey;
    public int goldReward;
    public int expReward;
    public List<Item> itemRewards = new List<Item>();
    public List<Equipment> equipmentRewards = new List<Equipment>();
}
