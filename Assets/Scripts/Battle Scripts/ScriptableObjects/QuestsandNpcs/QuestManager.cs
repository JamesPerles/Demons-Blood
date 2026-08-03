using System.Collections.Generic;
using UnityEngine;
public class QuestManager : MonoBehaviour
{
public static QuestManager instance;
public List<QuestProgress> activeQuests = new List<QuestProgress>();
public List<Quest> completedQuests = new List<Quest>();
void Awake()
    {
        if(instance == null) {instance = this; DontDestroyOnLoad(gameObject);}
        else Destroy(gameObject); return;
    }
    void OnEnable()
    {
        if(FlagManager.instance != null) FlagManager.instance.onFlagChanged += HandleFlagChanged;
    }
    void OnDisable()
    {
        if(FlagManager.instance != null) FlagManager.instance.onFlagChanged -= HandleFlagChanged;
    }
    public void StartQuest(Quest quest)
    {
        if(quest == null) return;
        if(activeQuests.Exists(progress => progress.quest == quest) || completedQuests.Contains(quest)) return;
  activeQuests.Add(new QuestProgress(quest));
    }
    void HandleFlagChanged(string key)
    {
        List<QuestProgress> snapshot = new List<QuestProgress>(activeQuests);
        foreach(QuestProgress progress in snapshot)
        {
            QuestObjective current = progress.CurrentObjective;
            if(current == null) continue;
            if(current.flagKey == key && FlagManager.instance.GetFlags(key)) AdvanceQuest(progress);
        }
    }
    void AdvanceQuest(QuestProgress progress)
    {
        progress.currentObjective++;
        if(progress.currentObjective >= progress.quest.objectives.Count)
        {
            progress.isComplete = true;
            CompleteQuest(progress);
        }
        void CompleteQuest(QuestProgress completedProgress)
        {
            Quest quest = completedProgress.quest;
            activeQuests.Remove(completedProgress);
            completedQuests.Add(quest);
            if(quest.goldReward > 0 && Wallet.instance != null) Wallet.instance.AddGold(quest.goldReward);
            if (PlayerParty.instance != null)
            {
                foreach (GameObject characterObject in PlayerParty.instance.playableCharacters)
                {
                ActiveStats character = characterObject.GetComponent<ActiveStats>();
                if(character != null && quest.expReward > 0) character.GainExperience(quest.expReward);
                }

            }
        foreach(Item item in quest.itemRewards)
        {
            if(item != null && InventoryManager.Instance != null) InventoryManager.Instance.PickupItem(item);
        }
        foreach (Equipment equipment in quest.equipmentRewards)
            {
                if(equipment != null && EquipmentManager.instance != null) EquipmentManager.instance.PickupEquipment(equipment);
            }
    }
}
}
