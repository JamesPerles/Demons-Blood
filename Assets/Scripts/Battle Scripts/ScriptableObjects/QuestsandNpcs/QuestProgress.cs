using System.Collections.Generic;
public enum QuestState {Active, ReadyToTurnIn, Complete, Failed}
[System.Serializable]
public class QuestProgress
{
    public Quest quest;
    public int currentStage;
    public List<int> objectiveCounts = new List<int>();
    public List<int> optionalCounts = new List<int>();
    public List<bool> optionalClaimed = new List<bool>();
    public List <int> chosenObjectivePerStage = new List<int>();
    public QuestState state;
    public QuestProgress(Quest quest)
    {
        this.quest = quest;
        currentStage = 0;
        state = QuestState.Active;
        chosenObjectivePerStage = new List<int>();
        for(int i = 0; i < quest.stages.Count; i++) chosenObjectivePerStage.Add(-1);
        StageCount(); 
    }
    public QuestStage CurrentStage => (quest != null && 
    currentStage < quest.stages.Count) ? quest.stages[currentStage] : null;
    public int GetChosenObjective(int stageIndex)
    {
        if(chosenObjectivePerStage == null || stageIndex < 0 || stageIndex >= chosenObjectivePerStage.Count) return -1;
        return chosenObjectivePerStage[stageIndex];
    }
    public void StageCount()
    {
        objectiveCounts = new List<int>();
        optionalCounts = new List<int>();
        optionalClaimed = new List<bool>();
        QuestStage stage = CurrentStage;
        if(stage == null) return;
        for(int i = 0; i < stage.objectives.Count; i++) 
        {
            objectiveCounts.Add(0);
            foreach(OptionalObjective optional in stage.objectives[i].optionalObjectives)
            {
                optionalCounts.Add(0);
                optionalClaimed.Add(false);
            }
    }
}
public int OptionalTracker(int mainObjectiveIndex, int subObjectiveIndex)
    {
        QuestStage stage = CurrentStage;
        if(stage == null || mainObjectiveIndex < 0 || mainObjectiveIndex >= stage.objectives.Count) return -1;
        int flat = 0;
        for(int i = 0; i < mainObjectiveIndex; i++) flat += stage.objectives[i].optionalObjectives.Count;
        return flat + subObjectiveIndex;
    }
}