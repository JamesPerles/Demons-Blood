using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class QuestObjective
{
[TextArea(1, 3)] public string description;
public string flagKey;
}
[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string questName;
    [TextArea(2,5)] public string description;
    public bool isMainQuest;
    public List<QuestObjective> objectives = new List<QuestObjective>();
    public int goldReward;
    public int expReward;
    public List<Item> itemRewards = new List<Item>();
    public List<Equipment> equipmentRewards = new List<Equipment>();
}
