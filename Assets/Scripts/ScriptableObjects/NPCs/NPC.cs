using UnityEngine;
public class NPC : MonoBehaviour, IInteractable
{
public string npcName;
public string[] dialogueLines;
public Quest offeredQuest;
public string[] questOfferDialogueLines;
public virtual void Interact()
    {
        if(DialogueBox.instance == null) {Debug.LogError("No Dialogue in Scene"); return;}
        if(offeredQuest != null && QuestManager.instance != null && QuestManager.instance.IsQuestAvailable(offeredQuest))
        {
            string[] lines = (questOfferDialogueLines != null && questOfferDialogueLines.Length > 0) ? questOfferDialogueLines : dialogueLines;
            if(lines == null || lines.Length == 0) {GiveQuest(); return;}
            DialogueBox.instance.StartDialogue(npcName, lines, GiveQuest);
            return;
        }
        if(dialogueLines == null || dialogueLines.Length == 0) {Debug.LogWarning($"{npcName} has no lines set"); return;}
        DialogueBox.instance.StartDialogue(npcName, dialogueLines);
    }
    void GiveQuest()
    {
        if(QuestManager.instance != null && offeredQuest != null) QuestManager.instance.StartQuest(offeredQuest);
    }
}
