[System.Serializable]
public class QuestProgress
{
    public Quest quest;
    public int currentObjective;
    public bool isComplete;
    public QuestProgress(Quest quest)
    {
        this.quest = quest;
        currentObjective = 0;
        isComplete = false;
    }
    public QuestObjective CurrentObjective => 
    (!isComplete && currentObjective < quest.objectives.Count) ? quest.objectives[currentObjective] : null;
}
