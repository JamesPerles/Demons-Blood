using System.Collections.Generic;
using UnityEngine;
public class QuestManager : MonoBehaviour
{
public static QuestManager instance;
public List<QuestProgress> activeQuests = new List<QuestProgress>();
public List<Quest> completedQuests = new List<Quest>();
public List<Quest> failedQuests = new List<Quest>();
void Awake()
    {
        if(instance == null) {instance = this; DontDestroyOnLoad(gameObject);}
        else Destroy(gameObject); return;
    }
    void Start()
    {
        if(FlagManager.instance != null) FlagManager.instance.onFlagChanged += FlagChanged;
    }
    void OnDisable()
    {
        if(FlagManager.instance != null) FlagManager.instance.onFlagChanged -= FlagChanged;
    }
    public bool IsQuestAvailable(Quest quest)
    {
        if(quest == null) return false;
        if(activeQuests.Exists(progress => progress.quest == quest)) return false;
        if(completedQuests.Contains(quest)) return false;
        if(failedQuests.Contains(quest)) return false;
        foreach(Quest prereq in quest.prerequesite)
        if(!completedQuests.Contains(prereq)) return false;
        if(!string.IsNullOrEmpty(quest.unlockFlagKey) && !FlagManager.instance.GetFlag(quest.unlockFlagKey)) return false;
        return true;
    }
    public void StartQuest(Quest quest)
    {
        if(!IsQuestAvailable(quest)) return;
  activeQuests.Add(new QuestProgress(quest));
    }
    void FlagChanged(string key)
    {
        List<QuestProgress> snapshot = new List<QuestProgress>(activeQuests);
        foreach(QuestProgress progress in snapshot)
        {
            if(!string.IsNullOrEmpty(progress.quest.failFlagKey) && progress.quest.failFlagKey == key
            && FlagManager.instance.GetFlag(key))
            {
                FailQuest(progress);
                continue;
            }
            if(progress.state == QuestState.ReadyToTurnIn && progress.quest.requiresTurnIn
            && progress.quest.turnInFlagKey == key && FlagManager.instance.GetFlag(key))
            {
                CompleteQuest(progress);
                continue;
            }
            if(progress.state != QuestState.Active) continue;
            QuestStage stage = progress.CurrentStage;
            if(stage == null) continue;
            int stageAtStart = progress.currentStage;
              for(int i = 0; i < stage.objectives.Count; i++)
            {
                QuestObjective objective = stage.objectives[i];
                if(objective.type == ObjectiveType.Flag && objective.flagKey == key && FlagManager.instance.GetFlag(key))
                {
                 ObjectiveSatisfied(progress, i);
                 if(progress.currentStage != stageAtStart) break;
                }
                if(objective.hasOptionalObjectives)
                for(int j = 0; j < objective.optionalObjectives.Count; j++)
                {
                    OptionalObjective optional = objective.optionalObjectives[j];
                    if(optional.type != ObjectiveType.Flag || optional.flagKey != key || !FlagManager.instance.GetFlag(key)) continue;
                    int tracker = progress.OptionalTracker(i, j);
                    if(tracker < 0 || tracker >= progress.optionalClaimed.Count || progress.optionalClaimed[tracker]) continue;
                    progress.optionalClaimed[tracker] = true;
                    progress.pendingOptionalGold += optional.bonusGoldReward;
                    progress.pendingOptionalExp += optional.bonusExpReward;
                    if(optional.bonusItemRewards != null) progress.pendingOptionalItems.AddRange(optional.bonusItemRewards);
                    if(optional.bonusEquipmentRewards != null) progress.pendingOptionalEquipment.AddRange(optional.bonusEquipmentRewards);
                }
                if(progress.currentStage != stageAtStart) break;
            }
        }
    }
    public void ReportKill(EnemyStats enemy)
    {
        if(enemy == null) return;
        List<QuestProgress> snapshot = new List<QuestProgress>(activeQuests);
        foreach(QuestProgress progress in snapshot)
        {
            if(progress.state != QuestState.Active) continue;
            QuestStage stage = progress.CurrentStage;
            if(stage == null) continue;
            int stageAtStart = progress.currentStage;
            for(int i = 0; i < stage.objectives.Count; i++)
            {
                QuestObjective objective = stage.objectives[i];
                if(objective.type == ObjectiveType.KillCount && objective.targetEnemy == enemy)
                {
                    bool wasSatisfied = progress.objectiveCounts[i] >= objective.requiredCount;
                    progress.objectiveCounts[i] = Mathf.Min(objective.requiredCount, progress.objectiveCounts[i] + 1);
                    if(!wasSatisfied && progress.objectiveCounts[i] >= objective.requiredCount) ObjectiveSatisfied(progress, i);
                    if(progress.currentStage != stageAtStart) break;
                }
                if(objective.hasOptionalObjectives)
                for(int j = 0; j < objective.optionalObjectives.Count; j++)
                {
                    OptionalObjective optional = objective.optionalObjectives[j];
                    if(optional.type == ObjectiveType.KillCount && optional.targetEnemy == enemy)
                    CheckOptional(progress, i, j, optional);
                }
                if(progress.currentStage != stageAtStart) break;
            }
        }
    }
    public void ReportItemObtained(Baggable item)
    {
        if(item == null) return;
        List<QuestProgress> snapshot = new List<QuestProgress>(activeQuests);
        foreach(QuestProgress progress in snapshot)
        {
            if(progress.state != QuestState.Active) continue;
            QuestStage stage = progress.CurrentStage;
            if(stage == null) continue;
            int stageAtStart = progress.currentStage;
            for(int i = 0; i < stage.objectives.Count; i++)
            {
                QuestObjective objective = stage.objectives[i];
                if(objective.type == ObjectiveType.CollectItem && objective.targetItem == item)
                {
                    bool wasSatisfied = progress.objectiveCounts[i] >= objective.requiredCount;
                    progress.objectiveCounts[i] = Mathf.Min(objective.requiredCount, progress.objectiveCounts[i] + 1);
                    if(!wasSatisfied && progress.objectiveCounts[i] >= objective.requiredCount) ObjectiveSatisfied(progress, i);
                    if(progress.currentStage != stageAtStart) break;
                }
                if(objective.hasOptionalObjectives)
                for(int j = 0; j < objective.optionalObjectives.Count; j++)
                {
                    OptionalObjective optional = objective.optionalObjectives[j];
                    if(optional.type == ObjectiveType.CollectItem && optional.targetItem == item)
                    CheckOptional(progress, i, j, optional);
                }
                if(progress.currentStage != stageAtStart) break;
            }
        }
    }
    void CheckOptional(QuestProgress progress, int mainIndex, int subIndex, OptionalObjective optional)
    {
        int tracker = progress.OptionalTracker(mainIndex, subIndex);
        if(tracker < 0 || tracker >= progress.optionalCounts.Count || progress.optionalClaimed[tracker]) return;
        progress.optionalCounts[tracker] = Mathf.Min(optional.requiredCount, progress.optionalCounts[tracker] + 1);
        if(progress.optionalCounts[tracker] >= optional.requiredCount)
        {
            progress.optionalClaimed[tracker] = true;
             progress.pendingOptionalGold += optional.bonusGoldReward;
                    progress.pendingOptionalExp += optional.bonusExpReward;
                    if(optional.bonusItemRewards != null) progress.pendingOptionalItems.AddRange(optional.bonusItemRewards);
                    if(optional.bonusEquipmentRewards != null) progress.pendingOptionalEquipment.AddRange(optional.bonusEquipmentRewards);
        }
    }
    public void TurnInQuest(Quest quest)
    {
        QuestProgress progress =activeQuests.Find(prog => prog.quest == quest);
        if(progress != null && progress.state == QuestState.ReadyToTurnIn) CompleteQuest(progress);
    }
    public bool IsObjectiveSatisfied(QuestProgress progress, int index)
    {
        QuestObjective objective = progress.CurrentStage.objectives[index];
        switch(objective.type)
        {
            case ObjectiveType.Flag: return FlagManager.instance.GetFlag(objective.flagKey);
            case ObjectiveType.KillCount:
            case ObjectiveType.CollectItem: return progress.objectiveCounts[index] >= objective.requiredCount;
            default: return false;
        }
    }
    void ObjectiveSatisfied(QuestProgress progress, int objectiveIndex)
    {
        QuestStage stage = progress.CurrentStage;
        QuestObjective objective = stage.objectives[objectiveIndex];
        if(stage.isChoiceStage)
        {
            if(progress.currentStage >= 0 && progress.currentStage < progress.chosenObjectivePerStage.Count)
            progress.chosenObjectivePerStage[progress.currentStage] = objectiveIndex;
            AdvanceStage(progress);
            GrantRewards(objective.choiceGoldReward, objective.choiceExpReward, objective.choiceItemRewards, objective.choiceEquipmentRewards);
        }
        else
        {
            for(int i = 0; i < stage.objectives.Count; i++)
            if(!IsObjectiveSatisfied(progress, i)) return;
            AdvanceStage(progress);
        }
    }
    void AdvanceStage(QuestProgress progress)
    {
        progress.currentStage++;
        if(progress.currentStage >= progress.quest.stages.Count)
        {
            if(progress.quest.requiresTurnIn) progress.state = QuestState.ReadyToTurnIn;
            else CompleteQuest(progress);
        }
        else
        {
            progress.StageCount();
        }
        }
        void FailQuest(QuestProgress progress)
        {
            progress.state = QuestState.Failed;
            activeQuests.Remove(progress);
            failedQuests.Add(progress.quest);
        }
        void CompleteQuest(QuestProgress completedProgress)
        {
         completedProgress.state = QuestState.Complete;
         Quest quest = completedProgress.quest;
         activeQuests.Remove(completedProgress);
         completedQuests.Add(quest);
         if(FlagManager.instance != null) FlagManager.instance.SetFlag($"{quest.name}_Completed", true);
         GrantRewards(quest.goldReward, quest.expReward, quest.itemRewards, quest.equipmentRewards);
         GrantRewards(completedProgress.pendingOptionalGold, completedProgress.pendingOptionalExp, completedProgress.pendingOptionalItems, completedProgress.pendingOptionalEquipment);
}
        void GrantRewards(int gold, int exp, List<Item> items, List<Equipment> equipment, string source = "")
    {
        if(gold > 0 && WalletManager.instance != null) WalletManager.instance.AddGold(gold);
        if(exp > 0 && PlayerParty.instance != null)
        {
            foreach(GameObject characterObject in PlayerParty.instance.playableCharacters)
            {
                ActiveStats character = characterObject.GetComponent<ActiveStats>();
                if(character != null) character.GainExperience(exp);
            }
        }
        if(items != null)
        foreach(Item item in items)
        if(item != null && InventoryManager.instance != null) InventoryManager.instance.PickupItem(item);
        if(equipment != null)
        foreach(Equipment equip in equipment)
        if(equip != null && InventoryManager.instance != null) InventoryManager.instance.PickupItem(equip);
    }
}
